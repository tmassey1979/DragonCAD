using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentEditor;
using DragonCAD.App.SchematicEditor;
using DragonCAD.App.Tooling;

namespace DragonCAD.App.Tests.Tooling;

public sealed class EditorToolRailCatalogTests
{
    [Fact]
    public void SchematicBoardAndComponentToolsExposeRailMetadata()
    {
        SchematicEditorViewModel schematic = new();
        BoardEditorViewModel board = new();
        ComponentEditorViewModel component = ComponentEditorWorkspace.StartNew("dragon:test-component").ViewModel;

        IReadOnlyList<EditorToolGroup> schematicGroups = EditorToolRailCatalog.ForSchematic(schematic);
        IReadOnlyList<EditorToolGroup> boardGroups = EditorToolRailCatalog.ForBoard(board);
        IReadOnlyList<EditorToolGroup> componentGroups = EditorToolRailCatalog.ForComponent(component);

        AssertGroupToolsExposeMetadata(schematicGroups);
        AssertGroupToolsExposeMetadata(boardGroups);
        AssertGroupToolsExposeMetadata(componentGroups);
        Assert.Contains(schematicGroups, group => group.Id == "schematic-drawing");
        Assert.Contains(boardGroups, group => group.Id == "board-routing");
        Assert.Contains(componentGroups, group => group.Id == "component-footprint");
    }

    [Fact]
    public void ActiveStateTracksEditorToolSelection()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();

        EditorToolItem route = FindTool(EditorToolRailCatalog.ForBoard(board), "route");
        EditorToolItem select = FindTool(EditorToolRailCatalog.ForBoard(board), "select");

        Assert.True(route.IsActive);
        Assert.False(select.IsActive);

        ComponentEditorViewModel component = ComponentEditorWorkspace.StartNew("dragon:active-component").ViewModel;
        component.ActivateFootprintTool(ComponentEditorFootprintTool.ThroughHolePad);

        EditorToolItem pad = FindTool(EditorToolRailCatalog.ForComponent(component), "pad");

        Assert.True(pad.IsActive);
    }

    [Fact]
    public void CursorHintsExistForAllRequiredModes()
    {
        IReadOnlyDictionary<string, EditorToolCursorHint> hints = EditorToolCursorHints.AllByMode;

        string[] expectedModes = ["select", "move", "wire", "route", "place", "text", "pad", "via", "delete"];

        foreach (string mode in expectedModes)
        {
            EditorToolCursorHint hint = Assert.Contains(mode, hints);
            Assert.False(string.IsNullOrWhiteSpace(hint.CursorKey));
            Assert.False(string.IsNullOrWhiteSpace(hint.StatusText));
        }
    }

    [Fact]
    public void DisabledToolsExposeDeterministicReasons()
    {
        SchematicEditorViewModel schematic = new();
        BoardEditorViewModel board = new();

        EditorToolItem schematicPlace = FindTool(EditorToolRailCatalog.ForSchematic(schematic), "place");
        EditorToolItem boardVia = FindTool(EditorToolRailCatalog.ForBoard(board), "via");

        Assert.False(schematicPlace.IsEnabled);
        Assert.Equal("Choose a component before using the schematic place tool.", schematicPlace.DisabledReason);
        Assert.False(boardVia.IsEnabled);
        Assert.Equal("Start a route before dropping a via.", boardVia.DisabledReason);
    }

    [Fact]
    public void EditorSpecificGroupsExposeExpectedToolIds()
    {
        Assert.Equal(
            ["select", "move", "wire", "place", "text", "delete"],
            EditorToolRailCatalog.ForSchematic(new()).SelectMany(group => group.Tools).Select(tool => tool.Id));
        Assert.Equal(
            ["select", "move", "route", "via", "delete"],
            EditorToolRailCatalog.ForBoard(new BoardEditorViewModel()).SelectMany(group => group.Tools).Select(tool => tool.Id));
        Assert.Equal(
            ["select", "place", "wire", "arc", "text", "delete", "select-footprint", "pad", "smd-pad", "place-footprint", "hole", "text-footprint", "keepout", "delete-footprint"],
            EditorToolRailCatalog.ForComponent(ComponentEditorWorkspace.StartNew("dragon:group-component").ViewModel)
                .SelectMany(group => group.Tools)
                .Select(tool => tool.Id));
    }

    private static void AssertGroupToolsExposeMetadata(IReadOnlyList<EditorToolGroup> groups)
    {
        Assert.NotEmpty(groups);
        foreach (EditorToolItem tool in groups.SelectMany(group => group.Tools))
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Id));
            Assert.False(string.IsNullOrWhiteSpace(tool.IconId));
            Assert.False(string.IsNullOrWhiteSpace(tool.TooltipText));
            Assert.False(string.IsNullOrWhiteSpace(tool.ShortcutText));
            Assert.False(string.IsNullOrWhiteSpace(tool.CursorHint.Mode));
        }
    }

    private static EditorToolItem FindTool(IReadOnlyList<EditorToolGroup> groups, string id) =>
        Assert.Single(groups.SelectMany(group => group.Tools), tool => tool.Id == id);
}
