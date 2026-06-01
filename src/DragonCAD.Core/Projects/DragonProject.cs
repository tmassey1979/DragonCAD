using System.Text.Encodings.Web;
using System.Text.Json;
using DragonCAD.Core.Components.Definitions;

namespace DragonCAD.Core.Projects;

public sealed record DragonProject(
    DragonProjectManifest Manifest,
    IReadOnlyList<ComponentDefinition> ProjectComponents)
{
    public bool Equals(DragonProject? other) =>
        other is not null &&
        Manifest == other.Manifest &&
        ProjectComponents
            .Select(ComponentDefinitionSerializer.Serialize)
            .SequenceEqual(other.ProjectComponents.Select(ComponentDefinitionSerializer.Serialize), StringComparer.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Manifest, ProjectComponents.Count);
}

public sealed record DragonProjectManifest(
    string Name,
    Version SchemaVersion,
    string Generator);

public sealed class DragonProjectFolderStore
{
    public const string ManifestFileName = "dragoncad.project.json";
    public const string ComponentsDirectoryName = "components";
    private const string ComponentFileSuffix = ".dcad-component.json";

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public void Save(string projectRoot, DragonProject project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(project);

        Directory.CreateDirectory(projectRoot);
        Directory.CreateDirectory(Path.Combine(projectRoot, ComponentsDirectoryName));

        string manifestJson = JsonSerializer.Serialize(project.Manifest, ManifestOptions);
        File.WriteAllText(Path.Combine(projectRoot, ManifestFileName), manifestJson);

        foreach (ComponentDefinition component in project.ProjectComponents.OrderBy(component => component.Id.Value, StringComparer.Ordinal))
        {
            File.WriteAllText(
                ComponentPath(projectRoot, component.Id.Value),
                ComponentDefinitionSerializer.Serialize(component));
        }
    }

    public DragonProject Load(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        string manifestPath = Path.Combine(projectRoot, ManifestFileName);
        DragonProjectManifest manifest = JsonSerializer.Deserialize<DragonProjectManifest>(
            File.ReadAllText(manifestPath),
            ManifestOptions) ?? throw new InvalidOperationException("Project manifest was empty.");

        string componentsDirectory = Path.Combine(projectRoot, ComponentsDirectoryName);
        ComponentDefinition[] components = Directory.Exists(componentsDirectory)
            ? Directory
                .EnumerateFiles(componentsDirectory, $"*{ComponentFileSuffix}")
                .Order(StringComparer.Ordinal)
                .Select(path => ComponentDefinitionSerializer.Deserialize(File.ReadAllText(path)))
                .ToArray()
            : [];

        return new DragonProject(manifest, components);
    }

    private static string ComponentPath(string projectRoot, string componentId) =>
        Path.Combine(
            projectRoot,
            ComponentsDirectoryName,
            $"{Uri.EscapeDataString(componentId)}{ComponentFileSuffix}");
}
