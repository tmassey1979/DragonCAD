using System.Text.Json;
using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Projects;

public sealed class ProjectShellStateStore
{
    public const string ShellStatePath = "dragoncad.shell-state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public void Save(string projectRoot, ProjectShellState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(state);

        Directory.CreateDirectory(projectRoot);
        File.WriteAllText(
            Path.Combine(projectRoot, ShellStatePath),
            JsonSerializer.Serialize(state, SerializerOptions));
    }

    public ProjectShellState? Load(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        string path = Path.Combine(projectRoot, ShellStatePath);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProjectShellState>(
            File.ReadAllText(path),
            SerializerOptions);
    }
}

public sealed record ProjectShellState(
    IReadOnlyList<ProjectShellSchematicComponent> SchematicComponents,
    IReadOnlyList<SchematicWire> SchematicWires,
    IReadOnlyList<SchematicNet> SchematicNets,
    IReadOnlyList<SchematicNetLabel> SchematicNetLabels,
    IReadOnlyList<ProjectShellBoardComponent> BoardComponents,
    IReadOnlyList<BoardTrace> BoardTraces,
    IReadOnlyList<BoardVia> BoardVias);

public sealed record ProjectShellSchematicComponent(
    string InstanceId,
    string ReferenceDesignator,
    string ComponentId,
    string DisplayName,
    CadPoint Position,
    ComponentSymbolPreview SymbolPreview,
    ComponentFootprintPreview FootprintPreview,
    string Value,
    int RotationDegrees,
    bool IsMirrored);

public sealed record ProjectShellBoardComponent(
    string SyncId,
    string ReferenceDesignator,
    string ComponentId,
    string DisplayName,
    CadPoint Position,
    ComponentFootprintPreview FootprintPreview,
    string Value,
    int RotationDegrees,
    bool IsMirrored);
