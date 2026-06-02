using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Definitions;

namespace DragonCAD.Core.Projects;

public sealed record DragonProject(
    DragonProjectManifest Manifest,
    DragonSchematicDocument Schematic,
    DragonBoardDocument Board,
    DragonLibraryReferences LibraryReferences,
    DragonDatasheetIntake DatasheetIntake,
    DragonFabricationMetadata FabricationMetadata,
    IReadOnlyList<ComponentDefinition> ProjectComponents)
{
    public bool Equals(DragonProject? other) =>
        other is not null &&
        ProjectJson.Serialize(Manifest) == ProjectJson.Serialize(other.Manifest) &&
        ProjectJson.Serialize(Schematic) == ProjectJson.Serialize(other.Schematic) &&
        ProjectJson.Serialize(Board) == ProjectJson.Serialize(other.Board) &&
        ProjectJson.Serialize(LibraryReferences) == ProjectJson.Serialize(other.LibraryReferences) &&
        ProjectJson.Serialize(DatasheetIntake) == ProjectJson.Serialize(other.DatasheetIntake) &&
        ProjectJson.Serialize(FabricationMetadata) == ProjectJson.Serialize(other.FabricationMetadata) &&
        ProjectComponents
            .OrderBy(component => component.Id.Value, StringComparer.Ordinal)
            .Select(ComponentDefinitionSerializer.Serialize)
            .SequenceEqual(
                other.ProjectComponents
                    .OrderBy(component => component.Id.Value, StringComparer.Ordinal)
                    .Select(ComponentDefinitionSerializer.Serialize),
                StringComparer.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Manifest, ProjectComponents.Count);
}

public sealed record DragonProjectManifest(
    string Name,
    Version SchemaVersion,
    string Generator);

public sealed record DragonSchematicDocument
{
    public DragonSchematicDocument(
        string documentId,
        IReadOnlyList<DragonSchematicComponent> components,
        IReadOnlyList<DragonSchematicNet> nets)
    {
        DocumentId = documentId;
        Components = SortComponents(components);
        Nets = SortNets(nets);
    }

    public string DocumentId { get; }

    public IReadOnlyList<DragonSchematicComponent> Components { get; }

    public IReadOnlyList<DragonSchematicNet> Nets { get; }

    private static IReadOnlyList<DragonSchematicComponent> SortComponents(IReadOnlyList<DragonSchematicComponent> components) =>
        components
            .OrderBy(component => component.Reference, StringComparer.Ordinal)
            .ThenBy(component => component.IdentityId, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<DragonSchematicNet> SortNets(IReadOnlyList<DragonSchematicNet> nets) =>
        nets
            .OrderBy(net => net.Name, StringComparer.Ordinal)
            .ToArray();
}

public sealed record DragonSchematicComponent(
    string IdentityId,
    string Reference,
    string ComponentId);

public sealed record DragonSchematicNet
{
    public DragonSchematicNet(string name, IReadOnlyList<string> pins)
    {
        Name = name;
        Pins = pins.Order(StringComparer.Ordinal).ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<string> Pins { get; }
}

public sealed record DragonBoardDocument
{
    public DragonBoardDocument(
        string documentId,
        IReadOnlyList<DragonBoardPlacement> placements,
        IReadOnlyList<DragonBoardTrace> traces)
    {
        DocumentId = documentId;
        Placements = placements
            .OrderBy(placement => placement.Reference, StringComparer.Ordinal)
            .ThenBy(placement => placement.SchematicComponentId, StringComparer.Ordinal)
            .ToArray();
        Traces = traces
            .OrderBy(trace => trace.NetName, StringComparer.Ordinal)
            .ThenBy(trace => trace.Layer, StringComparer.Ordinal)
            .ToArray();
    }

    public string DocumentId { get; }

    public IReadOnlyList<DragonBoardPlacement> Placements { get; }

    public IReadOnlyList<DragonBoardTrace> Traces { get; }
}

public sealed record DragonBoardPlacement(
    string SchematicComponentId,
    string Reference,
    string FootprintId,
    decimal X,
    decimal Y,
    decimal Rotation);

public sealed record DragonBoardTrace(
    string NetName,
    string Layer,
    decimal Width);

public sealed record DragonLibraryReferences
{
    public DragonLibraryReferences(IReadOnlyList<DragonLibraryReference> references)
    {
        References = references
            .OrderBy(reference => reference.LibraryId, StringComparer.Ordinal)
            .ThenBy(reference => reference.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<DragonLibraryReference> References { get; }
}

public sealed record DragonLibraryReference(
    string LibraryId,
    string Path,
    string? Checksum);

public sealed record DragonDatasheetIntake
{
    public DragonDatasheetIntake(IReadOnlyList<DragonDatasheetRecord> records)
    {
        Records = records
            .OrderBy(record => record.PartNumber, StringComparer.Ordinal)
            .ThenBy(record => record.TargetPath, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<DragonDatasheetRecord> Records { get; }
}

public sealed record DragonDatasheetRecord(
    string PartNumber,
    string SourceUri,
    string TargetPath,
    string? Checksum);

public sealed record DragonFabricationMetadata
{
    public DragonFabricationMetadata(
        IReadOnlyList<DragonFabricationOutput> outputs,
        IReadOnlyList<DragonFabricationAttribute> attributes)
    {
        Outputs = outputs
            .OrderBy(output => output.Kind, StringComparer.Ordinal)
            .ThenBy(output => output.Path, StringComparer.Ordinal)
            .ToArray();
        Attributes = attributes
            .OrderBy(attribute => attribute.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<DragonFabricationOutput> Outputs { get; }

    public IReadOnlyList<DragonFabricationAttribute> Attributes { get; }
}

public sealed record DragonFabricationOutput(
    string Kind,
    string Path,
    string? Checksum);

public sealed record DragonFabricationAttribute(
    string Name,
    string Value);

public sealed record DragonProjectLoadResult(
    DragonProject? Project,
    IReadOnlyList<DragonProjectDiagnostic> Diagnostics);

public sealed record DragonProjectDiagnostic(
    DragonProjectDiagnosticSeverity Severity,
    string Code,
    string Message);

public enum DragonProjectDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed class DragonProjectFolderStore
{
    public const string ManifestFileName = "dragoncad.project.json";
    public const string ComponentsDirectoryName = "components";

    private const string ComponentFileSuffix = ".dcad-component.json";
    private const string SchematicPath = "schematic/schematic.json";
    private const string BoardPath = "board/board.json";
    private const string LibraryReferencesPath = "libraries/library-references.json";
    private const string DatasheetIntakePath = "datasheets/datasheet-intake.json";
    private const string FabricationMetadataPath = "fabrication/fabrication-metadata.json";

    private static readonly string[] RequiredFiles =
    [
        ManifestFileName,
        SchematicPath,
        BoardPath,
        LibraryReferencesPath,
        DatasheetIntakePath,
        FabricationMetadataPath
    ];

    public void Save(string projectRoot, DragonProject project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(project);

        Directory.CreateDirectory(projectRoot);
        WriteJson(projectRoot, ManifestFileName, project.Manifest);
        WriteJson(projectRoot, SchematicPath, project.Schematic);
        WriteJson(projectRoot, BoardPath, project.Board);
        WriteJson(projectRoot, LibraryReferencesPath, project.LibraryReferences);
        WriteJson(projectRoot, DatasheetIntakePath, project.DatasheetIntake);
        WriteJson(projectRoot, FabricationMetadataPath, project.FabricationMetadata);
        SaveComponents(projectRoot, project.ProjectComponents);
    }

    public DragonProjectLoadResult Load(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        List<DragonProjectDiagnostic> diagnostics = [];
        foreach (string relativePath in RequiredFiles)
        {
            string absolutePath = Path.Combine(projectRoot, relativePath);
            if (!File.Exists(absolutePath))
            {
                diagnostics.Add(MissingFileDiagnostic(relativePath));
            }
        }

        if (diagnostics.Count > 0)
        {
            return new DragonProjectLoadResult(null, diagnostics);
        }

        DragonProjectManifest? manifest = ReadJson<DragonProjectManifest>(projectRoot, ManifestFileName, diagnostics);
        DragonSchematicDocument? schematic = ReadJson<DragonSchematicDocument>(projectRoot, SchematicPath, diagnostics);
        DragonBoardDocument? board = ReadJson<DragonBoardDocument>(projectRoot, BoardPath, diagnostics);
        DragonLibraryReferences? libraryReferences = ReadJson<DragonLibraryReferences>(projectRoot, LibraryReferencesPath, diagnostics);
        DragonDatasheetIntake? datasheetIntake = ReadJson<DragonDatasheetIntake>(projectRoot, DatasheetIntakePath, diagnostics);
        DragonFabricationMetadata? fabricationMetadata = ReadJson<DragonFabricationMetadata>(projectRoot, FabricationMetadataPath, diagnostics);
        ComponentDefinition[] components = ReadComponents(projectRoot, diagnostics);

        if (diagnostics.Count > 0 ||
            manifest is null ||
            schematic is null ||
            board is null ||
            libraryReferences is null ||
            datasheetIntake is null ||
            fabricationMetadata is null)
        {
            return new DragonProjectLoadResult(null, diagnostics);
        }

        return new DragonProjectLoadResult(
            new DragonProject(
                manifest,
                schematic,
                board,
                libraryReferences,
                datasheetIntake,
                fabricationMetadata,
                components),
            []);
    }

    private static void SaveComponents(string projectRoot, IReadOnlyList<ComponentDefinition> components)
    {
        string componentsDirectory = Path.Combine(projectRoot, ComponentsDirectoryName);
        Directory.CreateDirectory(componentsDirectory);

        foreach (string staleComponentPath in Directory.EnumerateFiles(componentsDirectory, $"*{ComponentFileSuffix}"))
        {
            File.Delete(staleComponentPath);
        }

        foreach (ComponentDefinition component in components.OrderBy(component => component.Id.Value, StringComparer.Ordinal))
        {
            File.WriteAllText(
                ComponentPath(projectRoot, component.Id.Value),
                ComponentDefinitionSerializer.Serialize(component));
        }
    }

    private static ComponentDefinition[] ReadComponents(
        string projectRoot,
        List<DragonProjectDiagnostic> diagnostics)
    {
        string componentsDirectory = Path.Combine(projectRoot, ComponentsDirectoryName);
        if (!Directory.Exists(componentsDirectory))
        {
            return [];
        }

        List<ComponentDefinition> components = [];
        foreach (string path in Directory
            .EnumerateFiles(componentsDirectory, $"*{ComponentFileSuffix}")
            .Order(StringComparer.Ordinal))
        {
            try
            {
                components.Add(ComponentDefinitionSerializer.Deserialize(File.ReadAllText(path)));
            }
            catch (JsonException exception)
            {
                diagnostics.Add(CorruptFileDiagnostic(RelativePath(projectRoot, path), exception));
            }
        }

        return components.ToArray();
    }

    private static void WriteJson<T>(string projectRoot, string relativePath, T value)
    {
        string path = Path.Combine(projectRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? projectRoot);
        File.WriteAllText(path, ProjectJson.Serialize(value));
    }

    private static T? ReadJson<T>(
        string projectRoot,
        string relativePath,
        List<DragonProjectDiagnostic> diagnostics)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                File.ReadAllText(Path.Combine(projectRoot, relativePath)),
                ProjectJson.Options);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(CorruptFileDiagnostic(relativePath, exception));
            return default;
        }
        catch (NotSupportedException exception)
        {
            diagnostics.Add(CorruptFileDiagnostic(relativePath, exception));
            return default;
        }
    }

    private static DragonProjectDiagnostic MissingFileDiagnostic(string relativePath) =>
        new(
            DragonProjectDiagnosticSeverity.Error,
            "ProjectFileMissing",
            $"Required project file '{NormalizePath(relativePath)}' is missing.");

    private static DragonProjectDiagnostic CorruptFileDiagnostic(string relativePath, Exception exception) =>
        new(
            DragonProjectDiagnosticSeverity.Error,
            "ProjectFileCorrupt",
            $"Project file '{NormalizePath(relativePath)}' could not be read: {exception.Message}");

    private static string ComponentPath(string projectRoot, string componentId) =>
        Path.Combine(
            projectRoot,
            ComponentsDirectoryName,
            $"{Uri.EscapeDataString(componentId)}{ComponentFileSuffix}");

    private static string RelativePath(string projectRoot, string path) =>
        NormalizePath(Path.GetRelativePath(projectRoot, path));

    private static string NormalizePath(string path) => path.Replace('\\', '/');

}

internal static class ProjectJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new VersionJsonConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}

internal sealed class VersionJsonConverter : JsonConverter<Version>
{
    public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value) || !Version.TryParse(value, out Version? version))
        {
            throw new JsonException("Version value must be a valid version string.");
        }

        return version;
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        string version = value.Build < 0
            ? string.Create(CultureInfo.InvariantCulture, $"{value.Major}.{value.Minor}")
            : value.ToString();
        writer.WriteStringValue(version);
    }
}
