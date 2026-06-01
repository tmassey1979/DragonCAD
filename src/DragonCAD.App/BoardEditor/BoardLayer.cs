namespace DragonCAD.App.BoardEditor;

public sealed record BoardLayer(
    string Name,
    string ColorHex,
    bool IsVisible = true);
