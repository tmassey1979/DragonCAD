using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentEditor;
using DragonCAD.App.SchematicEditor;

namespace DragonCAD.App.Tooling;

public static class EditorToolRailCatalog
{
    public static IReadOnlyList<EditorToolGroup> ForSchematic(SchematicEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        bool placeActive = editor.HasActivePlacementCandidate;
        bool wireActive = editor.PendingWireStart is not null;
        bool hasSelection = editor.SelectedComponent is not null || editor.SelectedWire is not null || editor.SelectedNetLabel is not null;

        return
        [
            new(
                "schematic-drawing",
                "Schematic",
                [
                    Tool("select", "tool-select", !placeActive && !wireActive, "Select schematic objects", "V", "select"),
                    Tool("move", "tool-move", false, "Move selected schematic objects", "M", "move", hasSelection, "Select a schematic object before using the move tool."),
                    Tool("wire", "tool-wire", wireActive, "Draw schematic wires", "W", "wire"),
                    Tool("place", "tool-place", placeActive, "Place the chosen component", "P", "place", editor.HasActivePlacementCandidate, "Choose a component before using the schematic place tool."),
                    Tool("text", "tool-text", false, "Add schematic text or net labels", "T", "text"),
                    Tool("delete", "tool-delete", false, "Delete selected schematic objects", "Del", "delete", hasSelection, "Select a schematic object before using the delete tool.")
                ])
        ];
    }

    public static IReadOnlyList<EditorToolGroup> ForBoard(BoardEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        bool routeActive = string.Equals(editor.ActiveTool, "Route", StringComparison.Ordinal);
        bool hasSelection = editor.SelectedObjectCount > 0 || editor.SelectedComponent is not null || editor.SelectedTrace is not null || editor.SelectedVia is not null;
        bool canDropVia = routeActive && editor.PendingTraceStart is not null;

        return
        [
            new(
                "board-routing",
                "Board",
                [
                    Tool("select", "tool-select", !routeActive, "Select board objects", "V", "select"),
                    Tool("move", "tool-move", false, "Move selected board objects", "M", "move", hasSelection, "Select a board object before using the move tool."),
                    Tool("route", "tool-route", routeActive, "Route board traces", "R", "route"),
                    Tool("via", "tool-via", false, "Drop a via while routing", "Shift+V", "via", canDropVia, "Start a route before dropping a via."),
                    Tool("delete", "tool-delete", false, "Delete selected board objects", "Del", "delete", hasSelection, "Select a board object before using the delete tool.")
                ])
        ];
    }

    public static IReadOnlyList<EditorToolGroup> ForComponent(ComponentEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return
        [
            new(
                "component-symbol",
                "Symbol",
                [
                    Tool("select", "tool-select", editor.ActiveSymbolTool == ComponentEditorSymbolTool.Select, "Select symbol primitives", "V", "select"),
                    Tool("place", "tool-pin", editor.ActiveSymbolTool == ComponentEditorSymbolTool.Pin, "Place a symbol pin", "P", "place"),
                    Tool("wire", "tool-line", editor.ActiveSymbolTool == ComponentEditorSymbolTool.Line, "Draw symbol lines", "L", "wire"),
                    Tool("arc", "tool-arc", editor.ActiveSymbolTool == ComponentEditorSymbolTool.Arc, "Draw symbol arcs", "A", "wire"),
                    Tool("text", "tool-text", editor.ActiveSymbolTool == ComponentEditorSymbolTool.Text, "Place symbol text", "T", "text"),
                    Tool("delete", "tool-delete", false, "Delete selected symbol content", "Del", "delete")
                ]),
            new(
                "component-footprint",
                "Footprint",
                [
                    Tool("select-footprint", "tool-select", editor.ActiveFootprintTool == ComponentEditorFootprintTool.Select, "Select footprint primitives", "V", "select"),
                    Tool("pad", "tool-pad", editor.ActiveFootprintTool == ComponentEditorFootprintTool.ThroughHolePad, "Place a through-hole pad", "P", "pad"),
                    Tool("smd-pad", "tool-smd-pad", editor.ActiveFootprintTool == ComponentEditorFootprintTool.SmdPad, "Place an SMD pad", "S", "pad"),
                    Tool("place-footprint", "tool-outline", editor.ActiveFootprintTool == ComponentEditorFootprintTool.Outline, "Draw footprint outline", "O", "place"),
                    Tool("hole", "tool-hole", editor.ActiveFootprintTool == ComponentEditorFootprintTool.Hole, "Place a mounting hole", "H", "pad"),
                    Tool("text-footprint", "tool-text", editor.ActiveFootprintTool == ComponentEditorFootprintTool.SilkscreenText, "Place footprint text", "T", "text"),
                    Tool("keepout", "tool-keepout", editor.ActiveFootprintTool == ComponentEditorFootprintTool.Keepout, "Draw a footprint keepout", "K", "place"),
                    Tool("delete-footprint", "tool-delete", false, "Delete selected footprint content", "Del", "delete")
                ])
        ];
    }

    private static EditorToolItem Tool(
        string id,
        string iconId,
        bool isActive,
        string tooltipText,
        string shortcutText,
        string cursorMode,
        bool isEnabled = true,
        string? disabledReason = null) =>
        new(
            id,
            iconId,
            isActive,
            tooltipText,
            shortcutText,
            EditorToolCursorHints.ForMode(cursorMode),
            isEnabled,
            isEnabled ? null : disabledReason);
}

public sealed record EditorToolGroup(
    string Id,
    string Title,
    IReadOnlyList<EditorToolItem> Tools);

public sealed record EditorToolItem(
    string Id,
    string IconId,
    bool IsActive,
    string TooltipText,
    string ShortcutText,
    EditorToolCursorHint CursorHint,
    bool IsEnabled,
    string? DisabledReason);
