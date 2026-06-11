using System.Collections.ObjectModel;
using System.Text.Json;

namespace DragonCAD.App.Shell;

public sealed class DockLayoutPresetCatalog
{
    private readonly IReadOnlyDictionary<string, DockLayoutPreset> presetsByName;

    private DockLayoutPresetCatalog(IReadOnlyList<DockLayoutPreset> presets)
    {
        Presets = new ReadOnlyCollection<DockLayoutPreset>(presets.ToArray());
        presetsByName = Presets.ToDictionary(preset => preset.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<DockLayoutPreset> Presets { get; }

    public static DockLayoutPresetCatalog CreateDefault() =>
        new(
            [
                new DockLayoutPreset(
                    "Schematic Focus",
                    "Schematic",
                    [
                        new ShellDocumentTabPlacement("schematic", "Schematic", true),
                        new ShellDocumentTabPlacement("board", "PcbLayout", false)
                    ],
                    [
                        new ShellPanelPlacement("library", "Library", "Left", 0),
                        new ShellPanelPlacement("properties", "Properties", "Right", 0),
                        new ShellPanelPlacement("help", "Help", "Right", 1)
                    ],
                    [
                        new ShellPanelPlacement("output", "Output", "Bottom", 0)
                    ]),
                new DockLayoutPreset(
                    "PCB Focus",
                    "PcbLayout",
                    [
                        new ShellDocumentTabPlacement("board", "PcbLayout", true),
                        new ShellDocumentTabPlacement("schematic", "Schematic", false)
                    ],
                    [
                        new ShellPanelPlacement("layers", "Layers", "Right", 0),
                        new ShellPanelPlacement("properties", "Properties", "Right", 1),
                        new ShellPanelPlacement("library", "Library", "Left", 0)
                    ],
                    [
                        new ShellPanelPlacement("output", "Output", "Bottom", 0)
                    ]),
                new DockLayoutPreset(
                    "Component Authoring",
                    "ComponentEditor",
                    [
                        new ShellDocumentTabPlacement("component-editor", "ComponentEditor", true),
                        new ShellDocumentTabPlacement("schematic", "Schematic", false),
                        new ShellDocumentTabPlacement("board", "PcbLayout", false)
                    ],
                    [
                        new ShellPanelPlacement("library", "Library", "Left", 0),
                        new ShellPanelPlacement("properties", "Properties", "Right", 0),
                        new ShellPanelPlacement("help", "Help", "Right", 1)
                    ],
                    [
                        new ShellPanelPlacement("output", "Output", "Bottom", 0)
                    ]),
                new DockLayoutPreset(
                    "Marketplace",
                    "Marketplace",
                    [
                        new ShellDocumentTabPlacement("marketplace", "Marketplace", true),
                        new ShellDocumentTabPlacement("schematic", "Schematic", false),
                        new ShellDocumentTabPlacement("board", "PcbLayout", false)
                    ],
                    [
                        new ShellPanelPlacement("library", "Library", "Left", 0),
                        new ShellPanelPlacement("properties", "Properties", "Right", 0),
                        new ShellPanelPlacement("help", "Help", "Right", 1)
                    ],
                    [
                        new ShellPanelPlacement("output", "Output", "Bottom", 0)
                    ]),
                new DockLayoutPreset(
                    "Manufacturing Review",
                    "Fabrication",
                    [
                        new ShellDocumentTabPlacement("fabrication", "Fabrication", true),
                        new ShellDocumentTabPlacement("board", "PcbLayout", false),
                        new ShellDocumentTabPlacement("schematic", "Schematic", false)
                    ],
                    [
                        new ShellPanelPlacement("layers", "Layers", "Right", 0),
                        new ShellPanelPlacement("properties", "Properties", "Right", 1),
                        new ShellPanelPlacement("help", "Help", "Right", 2)
                    ],
                    [
                        new ShellPanelPlacement("output", "Output", "Bottom", 0)
                    ])
            ]);

    public DockLayoutPreset GetRequired(string name) =>
        presetsByName.TryGetValue(name, out DockLayoutPreset? preset)
            ? preset
            : throw new InvalidOperationException($"Unknown dock layout preset: {name}.");

    public ShellDockLayoutApplyResult TryApply(string? presetName, ShellDockLayoutState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string normalizedName = presetName?.Trim() ?? "";
        if (!presetsByName.TryGetValue(normalizedName, out DockLayoutPreset? preset))
        {
            return new ShellDockLayoutApplyResult(
                false,
                state,
                [new ShellDockLayoutDiagnostic("UnknownPreset", $"Unknown dock layout preset: {normalizedName}.")]);
        }

        return new ShellDockLayoutApplyResult(
            true,
            state with
            {
                SelectedPresetName = preset.Name,
                ActiveWorkspaceTab = preset.ActiveWorkspaceTab,
                DocumentTabs = preset.DocumentTabs,
                SidePanels = preset.SidePanels,
                BottomPanels = preset.BottomPanels
            },
            []);
    }
}

public sealed class ShellDockLayoutStateStore
{
    public const string LayoutStatePath = "dragoncad.shell-layout.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public void Save(string projectRoot, ShellDockLayoutState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(state);

        Directory.CreateDirectory(projectRoot);
        File.WriteAllText(
            Path.Combine(projectRoot, LayoutStatePath),
            JsonSerializer.Serialize(state, SerializerOptions));
    }

    public ShellDockLayoutState? Load(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        string path = Path.Combine(projectRoot, LayoutStatePath);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ShellDockLayoutState>(
            File.ReadAllText(path),
            SerializerOptions);
    }
}

public sealed record DockLayoutPreset(
    string Name,
    string ActiveWorkspaceTab,
    IReadOnlyList<ShellDocumentTabPlacement> DocumentTabs,
    IReadOnlyList<ShellPanelPlacement> SidePanels,
    IReadOnlyList<ShellPanelPlacement> BottomPanels);

public sealed record ShellDockLayoutState(
    string ActiveWorkspaceTab,
    string SelectedPresetName,
    IReadOnlyList<ShellOpenDocument> OpenDocuments,
    IReadOnlyList<ShellDocumentTabPlacement> DocumentTabs,
    IReadOnlyList<ShellPanelPlacement> SidePanels,
    IReadOnlyList<ShellPanelPlacement> BottomPanels) : IEquatable<ShellDockLayoutState>
{
    public static ShellDockLayoutState Create(
        string activeWorkspaceTab,
        IReadOnlyList<ShellOpenDocument> openDocuments) =>
        new(activeWorkspaceTab, "", openDocuments, [], [], []);

    public bool Equals(ShellDockLayoutState? other) =>
        other is not null &&
        string.Equals(ActiveWorkspaceTab, other.ActiveWorkspaceTab, StringComparison.Ordinal) &&
        string.Equals(SelectedPresetName, other.SelectedPresetName, StringComparison.Ordinal) &&
        OpenDocuments.SequenceEqual(other.OpenDocuments) &&
        DocumentTabs.SequenceEqual(other.DocumentTabs) &&
        SidePanels.SequenceEqual(other.SidePanels) &&
        BottomPanels.SequenceEqual(other.BottomPanels);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(ActiveWorkspaceTab, StringComparer.Ordinal);
        hash.Add(SelectedPresetName, StringComparer.Ordinal);
        AddSequenceHash(hash, OpenDocuments);
        AddSequenceHash(hash, DocumentTabs);
        AddSequenceHash(hash, SidePanels);
        AddSequenceHash(hash, BottomPanels);
        return hash.ToHashCode();
    }

    private static void AddSequenceHash<T>(HashCode hash, IEnumerable<T> values)
    {
        foreach (T value in values)
        {
            hash.Add(value);
        }
    }
}

public sealed record ShellDockLayoutApplyResult(
    bool Succeeded,
    ShellDockLayoutState State,
    IReadOnlyList<ShellDockLayoutDiagnostic> Diagnostics);

public sealed record ShellDockLayoutDiagnostic(string Code, string Message);

public sealed record ShellOpenDocument(string Id, string Title, string WorkspaceTab);

public sealed record ShellDocumentTabPlacement(string Id, string WorkspaceTab, bool IsActive);

public sealed record ShellPanelPlacement(string Id, string Title, string Placement, int Order);
