namespace DragonCAD.App.Help;

public static class ActiveToolHelpTopicMapper
{
    private static readonly IReadOnlyDictionary<string, string> TopicIdsByToolId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ActivateSelectToolCommand"] = "schematic-editing.placing-symbols",
        ["schematic.select"] = "schematic-editing.placing-symbols",
        ["Select"] = "schematic-editing.placing-symbols",
        ["PlaceSelectedComponentCommand"] = "schematic-editing.placing-symbols",
        ["PlaceArmedComponentOnSchematicCommand"] = "schematic-editing.placing-symbols",
        ["schematic.place-component"] = "schematic-editing.placing-symbols",
        ["ActivateWireToolCommand"] = "schematic-editing.placing-wires",
        ["schematic.wire"] = "schematic-editing.placing-wires",
        ["Wire"] = "schematic-editing.placing-wires",

        ["ActivateBoardSelectToolCommand"] = "pcb-layout.board-basics",
        ["board.select"] = "pcb-layout.board-basics",
        ["ActivateBoardRouteToolCommand"] = "pcb-layout.routing",
        ["board.route"] = "pcb-layout.routing",
        ["Route"] = "pcb-layout.routing",
        ["FinishBoardRouteCommand"] = "pcb-layout.routing",
        ["PlaceBoardViaCommand"] = "pcb-layout.vias",
        ["InsertBoardViaIntoSelectedTraceSegmentCommand"] = "pcb-layout.vias",
        ["board.via"] = "pcb-layout.vias",

        ["SetGridSizeCommand"] = "pcb-layout.grids",
        ["ToggleGridSnapCommand"] = "pcb-layout.grids",
        ["ToggleGridVisibilityCommand"] = "pcb-layout.grids",
        ["grid.size"] = "pcb-layout.grids",
        ["grid.snap"] = "pcb-layout.grids",
        ["grid.visibility"] = "pcb-layout.grids",

        ["SetActiveBoardLayerCommand"] = "pcb-layout.layers",
        ["ToggleSelectedBoardLayerVisibilityCommand"] = "pcb-layout.layers",
        ["MoveSelectedBoardTraceToLayerCommand"] = "pcb-layout.layers",
        ["OpenLayerSettingsCommand"] = "pcb-layout.layers",
        ["layer.active"] = "pcb-layout.layers",
        ["layer.visibility"] = "pcb-layout.layers",

        ["NewComponentEditorCommand"] = "component-libraries.component-editing",
        ["OpenSelectedComponentEditorCommand"] = "component-libraries.component-editing",
        ["AddComponentEditorStarterGeometryCommand"] = "component-libraries.component-editing",
        ["AddComponentEditorSymbolPinCommand"] = "component-libraries.component-editing",
        ["component-editor.symbol-pin"] = "component-libraries.component-editing",
        ["component.symbol-pin"] = "component-libraries.component-editing",
        ["ValidateComponentDraftCommand"] = "component-libraries.component-editing",

        ["SearchLibraryCommand"] = "component-libraries.library-basics",
        ["library.search"] = "component-libraries.library-basics",
        ["library.browse"] = "component-libraries.library-basics"
    };

    public static ActiveToolHelpTopicResult Resolve(string? toolId) =>
        Resolve(toolId, HelpTopicRegistry.CreateDefault());

    public static ActiveToolHelpTopicResult Resolve(string? toolId, HelpTopicRegistry registry)
    {
        string normalizedToolId = NormalizeToolId(toolId);
        if (normalizedToolId.Length > 0 && TopicIdsByToolId.TryGetValue(normalizedToolId, out string? topicId))
        {
            return new ActiveToolHelpTopicResult(
                ToolId: normalizedToolId,
                Topic: registry.GetTopicOrFallback(topicId),
                IsKnownTool: true,
                Diagnostics: []);
        }

        HelpTopic fallback = registry.GetTopicOrFallback(normalizedToolId);
        string diagnosticToolId = normalizedToolId.Length > 0 ? normalizedToolId : "(blank)";
        return new ActiveToolHelpTopicResult(
            ToolId: normalizedToolId,
            Topic: fallback,
            IsKnownTool: false,
            Diagnostics: [$"No local help topic mapping is registered for active tool id '{diagnosticToolId}'."]);
    }

    private static string NormalizeToolId(string? toolId) => (toolId ?? string.Empty).Trim();
}

public sealed record ActiveToolHelpTopicResult(
    string ToolId,
    HelpTopic Topic,
    bool IsKnownTool,
    IReadOnlyList<string> Diagnostics);
