using DragonCAD.App.Help;

namespace DragonCAD.App.Tests.Help;

public sealed class ActiveToolHelpTopicMapperTests
{
    [Theory]
    [InlineData("ActivateWireToolCommand", "schematic-editing.placing-wires")]
    [InlineData("schematic.wire", "schematic-editing.placing-wires")]
    [InlineData("Wire", "schematic-editing.placing-wires")]
    public void MapsSchematicWireToolToWireHelp(string toolId, string expectedTopicId)
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve(toolId);

        Assert.True(result.IsKnownTool);
        Assert.Equal(toolId, result.ToolId);
        Assert.Equal(expectedTopicId, result.Topic.Id);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("ActivateBoardRouteToolCommand", "pcb-layout.routing")]
    [InlineData("board.route", "pcb-layout.routing")]
    [InlineData("Route", "pcb-layout.routing")]
    [InlineData("PlaceBoardViaCommand", "pcb-layout.vias")]
    [InlineData("board.via", "pcb-layout.vias")]
    public void MapsBoardRouteAndViaToolsToBoardHelp(string toolId, string expectedTopicId)
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve(toolId);

        Assert.True(result.IsKnownTool);
        Assert.Equal(expectedTopicId, result.Topic.Id);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("AddComponentEditorSymbolPinCommand")]
    [InlineData("component-editor.symbol-pin")]
    [InlineData("component.symbol-pin")]
    public void MapsComponentSymbolPinToolsToComponentEditingHelp(string toolId)
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve(toolId);

        Assert.True(result.IsKnownTool);
        Assert.Equal("component-libraries.component-editing", result.Topic.Id);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("SearchLibraryCommand")]
    [InlineData("library.search")]
    [InlineData("library.browse")]
    public void MapsLibraryToolsToLibraryHelp(string toolId)
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve(toolId);

        Assert.True(result.IsKnownTool);
        Assert.Equal("component-libraries.library-basics", result.Topic.Id);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("SetGridSizeCommand", "pcb-layout.grids")]
    [InlineData("ToggleGridVisibilityCommand", "pcb-layout.grids")]
    [InlineData("grid.visibility", "pcb-layout.grids")]
    [InlineData("SetActiveBoardLayerCommand", "pcb-layout.layers")]
    [InlineData("ToggleSelectedBoardLayerVisibilityCommand", "pcb-layout.layers")]
    [InlineData("layer.visibility", "pcb-layout.layers")]
    public void MapsGridAndLayerToolsToLocalWorkflowHelp(string toolId, string expectedTopicId)
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve(toolId);

        Assert.True(result.IsKnownTool);
        Assert.Equal(expectedTopicId, result.Topic.Id);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void UnknownToolReturnsFriendlyFallbackWithDiagnostics()
    {
        ActiveToolHelpTopicResult result = ActiveToolHelpTopicMapper.Resolve("toolbar-button-42");

        Assert.False(result.IsKnownTool);
        Assert.Equal(HelpTopicRegistry.MissingTopicId, result.Topic.Id);
        Assert.Equal("Help topic not found", result.Topic.Title);
        Assert.Contains("toolbar-button-42", result.Topic.Summary);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("toolbar-button-42", StringComparison.Ordinal));
    }
}
