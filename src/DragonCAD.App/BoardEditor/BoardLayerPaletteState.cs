namespace DragonCAD.App.BoardEditor;

public sealed record BoardLayerPaletteState(
    string ActiveLayerName,
    IReadOnlyList<BoardLayerState> Layers);

public sealed record BoardLayerState(
    string Name,
    string ColorHex,
    bool IsVisible);

public sealed record BoardLayerPaletteImportResult(
    bool Applied,
    IReadOnlyList<string> Diagnostics);

public static class BoardLayerPalettePresets
{
    public static BoardLayerPaletteState TwoLayer { get; } = new(
        "Top",
        [
            new("Top", "#E63D32", true),
            new("Bottom", "#2D8CFF", true),
            new("Silkscreen", "#E2E8F0", true),
            new("Documentation", "#22C55E", true),
            new("Dimension", "#A3E635", true),
            new("Keepout", "#F43F5E", true),
            new("Names", "#F8FAFC", true),
            new("Values", "#CBD5E1", true),
            new("Drills", "#94A3B8", true)
        ]);

    public static BoardLayerPaletteState FourLayer { get; } = new(
        "Top",
        [
            new("Top", "#E63D32", true),
            new("Inner 1", "#F59E0B", true),
            new("Inner 2", "#8B5CF6", true),
            new("Bottom", "#2D8CFF", true),
            new("Silkscreen", "#E2E8F0", true),
            new("Documentation", "#22C55E", true),
            new("Dimension", "#A3E635", true),
            new("Keepout", "#F43F5E", true),
            new("Names", "#F8FAFC", true),
            new("Values", "#CBD5E1", true),
            new("Drills", "#94A3B8", true)
        ]);
}
