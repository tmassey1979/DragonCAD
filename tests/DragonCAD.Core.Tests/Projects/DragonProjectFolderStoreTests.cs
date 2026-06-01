using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Projects;

namespace DragonCAD.Core.Tests.Projects;

public sealed class DragonProjectFolderStoreTests
{
    [Fact]
    public void CreateSaveAndLoadProjectFolderRoundTripsDeterministically()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        DragonProject project = new(
            new DragonProjectManifest("Blink", new Version(1, 0), "dragoncad"),
            ProjectComponents: [Component("dragon:led", "LED")]);

        store.Save(temp.Path, project);
        string firstManifest = File.ReadAllText(Path.Combine(temp.Path, DragonProjectFolderStore.ManifestFileName));
        DragonProject loaded = store.Load(temp.Path);
        store.Save(temp.Path, loaded);
        string secondManifest = File.ReadAllText(Path.Combine(temp.Path, DragonProjectFolderStore.ManifestFileName));

        Assert.Equal(project, loaded);
        Assert.Equal(firstManifest, secondManifest);
        Assert.True(File.Exists(Path.Combine(temp.Path, "components", "dragon%3Aled.dcad-component.json")));
    }

    [Fact]
    public void LoadReturnsEmptyComponentListWhenComponentFolderIsMissing()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        store.Save(temp.Path, new DragonProject(new DragonProjectManifest("Empty", new Version(1, 0), "dragoncad"), []));
        Directory.Delete(Path.Combine(temp.Path, "components"), recursive: true);

        DragonProject loaded = store.Load(temp.Path);

        Assert.Empty(loaded.ProjectComponents);
    }

    private static ComponentDefinition Component(string id, string displayName) =>
        new(
            new ComponentId(id),
            displayName,
            ComponentKind.Custom,
            Manufacturer: "",
            ManufacturerPartNumber: "",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints: [],
            Variants: [],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

    private sealed class TempProjectDirectory : IDisposable
    {
        private TempProjectDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempProjectDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DragonCAD.ProjectFolder.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempProjectDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
