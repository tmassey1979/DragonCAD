using DragonCAD.App.BoardEditor;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardLayerPaletteStateTests
{
    [Fact]
    public void ExportImportLayerPaletteStatePersistsVisibilityActiveLayerAndColors()
    {
        BoardEditorViewModel source = new();
        source.SetActiveLayer("Bottom");
        source.SetLayerVisibility("Silkscreen", false);
        source.SetLayerColor("Top", "#FFAA00");
        source.SetLayerColor("Bottom", "#0055AA");

        BoardLayerPaletteState state = source.ExportLayerPaletteState();
        BoardEditorViewModel imported = new();
        BoardLayerPaletteImportResult result = imported.ImportLayerPaletteState(state);

        Assert.True(result.Applied);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Bottom", imported.ActiveLayerName);
        Assert.Contains(imported.Layers, layer => layer.Name == "Silkscreen" && !layer.IsVisible);
        Assert.Contains(imported.Layers, layer => layer.Name == "Top" && layer.ColorHex == "#FFAA00");
        Assert.Contains(imported.Layers, layer => layer.Name == "Bottom" && layer.ColorHex == "#0055AA");
    }

    [Fact]
    public void TwoLayerPresetContainsTopAndBottomCopperLayers()
    {
        BoardLayerPaletteState preset = BoardLayerPalettePresets.TwoLayer;

        Assert.Equal("Top", preset.ActiveLayerName);
        Assert.Equal(["Top", "Bottom"], preset.Layers.Take(2).Select(layer => layer.Name));
        Assert.DoesNotContain(preset.Layers, layer => layer.Name.StartsWith("Inner", StringComparison.Ordinal));
    }

    [Fact]
    public void FourLayerPresetContainsTopInnerAndBottomCopperLayers()
    {
        BoardLayerPaletteState preset = BoardLayerPalettePresets.FourLayer;

        Assert.Equal("Top", preset.ActiveLayerName);
        Assert.Equal(["Top", "Inner 1", "Inner 2", "Bottom"], preset.Layers.Take(4).Select(layer => layer.Name));
    }

    [Fact]
    public void ApplyLayerPalettePresetReplacesLayerPaletteDeterministically()
    {
        BoardEditorViewModel board = new();

        BoardLayerPaletteImportResult result = board.ApplyLayerPalettePreset(BoardLayerPalettePresets.FourLayer);

        Assert.True(result.Applied);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Top", board.ActiveLayerName);
        Assert.Equal(["Top", "Inner 1", "Inner 2", "Bottom"], board.Layers.Take(4).Select(layer => layer.Name));
    }

    [Fact]
    public void ImportLayerPaletteStateReportsInvalidActiveLayerDiagnostic()
    {
        BoardEditorViewModel board = new();
        BoardLayerPaletteState state = new(
            "Inner 9",
            [
                new BoardLayerState("Top", "#E63D32", true),
                new BoardLayerState("Bottom", "#2D8CFF", true)
            ]);

        BoardLayerPaletteImportResult result = board.ImportLayerPaletteState(state);

        Assert.False(result.Applied);
        Assert.Contains("Active layer 'Inner 9' does not exist in the layer palette.", result.Diagnostics);
        Assert.Equal("Top", board.ActiveLayerName);
        Assert.DoesNotContain(board.Layers, layer => layer.Name == "Inner 9");
    }

    [Fact]
    public void ImportLayerPaletteStateReportsDuplicateLayerNameDiagnostic()
    {
        BoardEditorViewModel board = new();
        BoardLayerPaletteState state = new(
            "Top",
            [
                new BoardLayerState("Top", "#E63D32", true),
                new BoardLayerState("Top", "#2D8CFF", false)
            ]);

        BoardLayerPaletteImportResult result = board.ImportLayerPaletteState(state);

        Assert.False(result.Applied);
        Assert.Contains("Layer name 'Top' appears more than once.", result.Diagnostics);
        Assert.Equal(["Top", "Bottom", "Silkscreen"], board.Layers.Take(3).Select(layer => layer.Name));
    }
}
