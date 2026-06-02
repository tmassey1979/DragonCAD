using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Projects;

namespace DragonCAD.Core.Tests.Projects;

public sealed class DragonProjectFolderStoreTests
{
    [Fact]
    public void SaveAndLoadProjectFolderRoundTripsIdentityAndSyncIds()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        DragonProject project = ProjectWithUnsortedLinks();

        store.Save(temp.Path, project);
        DragonProjectLoadResult result = store.Load(temp.Path);

        Assert.Empty(result.Diagnostics);
        DragonProject loaded = Assert.IsType<DragonProject>(result.Project);
        Assert.Equal(project, loaded);
        Assert.Equal("schema:main", loaded.Schematic.Components[0].IdentityId);
        Assert.Equal("schema:main", loaded.Board.Placements[0].SchematicComponentId);
    }

    [Fact]
    public void SaveWritesDeterministicProjectFolderJsonFiles()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        DragonProject project = ProjectWithUnsortedLinks();

        store.Save(temp.Path, project);
        Dictionary<string, string> firstWrite = ReadProjectJsonFiles(temp.Path);
        store.Save(temp.Path, project);
        Dictionary<string, string> secondWrite = ReadProjectJsonFiles(temp.Path);

        Assert.Equal(
            [
                "board/board.json",
                "components/dragon%3Aled.dcad-component.json",
                "datasheets/datasheet-intake.json",
                "dragoncad.project.json",
                "fabrication/fabrication-metadata.json",
                "libraries/library-references.json",
                "schematic/schematic.json"
            ],
            firstWrite.Keys);
        Assert.Equal(firstWrite, secondWrite);
        Assert.Contains("\"schemaVersion\": \"1.0\"", firstWrite["dragoncad.project.json"], StringComparison.Ordinal);
        Assert.True(firstWrite["libraries/library-references.json"].IndexOf("adafruit", StringComparison.Ordinal)
            < firstWrite["libraries/library-references.json"].IndexOf("sparkfun", StringComparison.Ordinal));
        Assert.True(firstWrite["datasheets/datasheet-intake.json"].IndexOf("NE555", StringComparison.Ordinal)
            < firstWrite["datasheets/datasheet-intake.json"].IndexOf("TMP36", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadReturnsMissingFileDiagnosticWithoutProject()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        store.Save(temp.Path, ProjectWithUnsortedLinks());
        File.Delete(Path.Combine(temp.Path, "schematic", "schematic.json"));

        DragonProjectLoadResult result = store.Load(temp.Path);

        Assert.Null(result.Project);
        DragonProjectDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DragonProjectDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("ProjectFileMissing", diagnostic.Code);
        Assert.Contains("schematic/schematic.json", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadReturnsCorruptFileDiagnosticWithoutProject()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        store.Save(temp.Path, ProjectWithUnsortedLinks());
        File.WriteAllText(Path.Combine(temp.Path, "board", "board.json"), "{ invalid json");

        DragonProjectLoadResult result = store.Load(temp.Path);

        Assert.Null(result.Project);
        DragonProjectDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DragonProjectDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("ProjectFileCorrupt", diagnostic.Code);
        Assert.Contains("board/board.json", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadReturnsEmptyComponentListWhenComponentFolderIsMissing()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        DragonProject project = new(
            new DragonProjectManifest("Empty", new Version(1, 0), "dragoncad"),
            new DragonSchematicDocument("schematic", [], []),
            new DragonBoardDocument("board", [], []),
            new DragonLibraryReferences([]),
            new DragonDatasheetIntake([]),
            new DragonFabricationMetadata([], []),
            ProjectComponents: []);

        store.Save(temp.Path, project);
        Directory.Delete(Path.Combine(temp.Path, "components"), recursive: true);
        DragonProjectLoadResult result = store.Load(temp.Path);

        DragonProject loaded = Assert.IsType<DragonProject>(result.Project);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(loaded.ProjectComponents);
    }

    private static DragonProject ProjectWithUnsortedLinks() =>
        new(
            new DragonProjectManifest("Blink", new Version(1, 0), "dragoncad"),
            new DragonSchematicDocument(
                "blink-schematic",
                components:
                [
                    new DragonSchematicComponent("schema:main", "D1", "dragon:led")
                ],
                nets:
                [
                    new DragonSchematicNet("N$2", ["D1.K", "R1.2"]),
                    new DragonSchematicNet("N$1", ["D1.A", "R1.1"])
                ]),
            new DragonBoardDocument(
                "blink-board",
                placements:
                [
                    new DragonBoardPlacement("schema:main", "D1", "LED-0603", 12.5m, 3.75m, 90m)
                ],
                traces:
                [
                    new DragonBoardTrace("N$2", "bottom", 0.15m),
                    new DragonBoardTrace("N$1", "top", 0.2m)
                ]),
            new DragonLibraryReferences(
            [
                new DragonLibraryReference("sparkfun", "libraries/sparkfun.dcadlib.json", "sha256:sparkfun"),
                new DragonLibraryReference("adafruit", "libraries/adafruit.dcadlib.json", "sha256:adafruit")
            ]),
            new DragonDatasheetIntake(
            [
                new DragonDatasheetRecord("TMP36", "https://example.test/tmp36.pdf", "datasheets/tmp36.pdf", "sha256:tmp36"),
                new DragonDatasheetRecord("NE555", "https://example.test/ne555.pdf", "datasheets/ne555.pdf", "sha256:ne555")
            ]),
            new DragonFabricationMetadata(
                outputs:
                [
                    new DragonFabricationOutput("pick-place", "fabrication/blink-pos.csv", "sha256:pos"),
                    new DragonFabricationOutput("gerber", "fabrication/blink-gerbers.zip", "sha256:gerbers")
                ],
                attributes:
                [
                    new DragonFabricationAttribute("finish", "ENIG"),
                    new DragonFabricationAttribute("layers", "2")
                ]),
            ProjectComponents: [Component("dragon:led", "LED")]);

    private static Dictionary<string, string> ReadProjectJsonFiles(string projectRoot) =>
        Directory
            .EnumerateFiles(projectRoot, "*.json", SearchOption.AllDirectories)
            .Select(path => (
                RelativePath: Path.GetRelativePath(projectRoot, path).Replace('\\', '/'),
                Contents: File.ReadAllText(path)))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToDictionary(file => file.RelativePath, file => file.Contents, StringComparer.Ordinal);

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
