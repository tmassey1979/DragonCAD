namespace DragonCAD.App.Tooling;

public static class EditorToolCursorHints
{
    public static IReadOnlyDictionary<string, EditorToolCursorHint> AllByMode { get; } =
        new Dictionary<string, EditorToolCursorHint>(StringComparer.Ordinal)
        {
            ["select"] = new("select", "Arrow", "Click to select objects."),
            ["move"] = new("move", "SizeAll", "Drag to move the selected object."),
            ["wire"] = new("wire", "Cross", "Click pins or vertices to draw a wire."),
            ["route"] = new("route", "Cross", "Click pads or trace endpoints to route copper."),
            ["place"] = new("place", "Cross", "Click to place the active item."),
            ["text"] = new("text", "IBeam", "Click to place text."),
            ["pad"] = new("pad", "Cross", "Click to place a pad."),
            ["via"] = new("via", "Cross", "Click while routing to drop a via."),
            ["delete"] = new("delete", "No", "Click or confirm to delete the selected object.")
        };

    public static EditorToolCursorHint ForMode(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        if (AllByMode.TryGetValue(mode, out EditorToolCursorHint? hint))
        {
            return hint;
        }

        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown editor tool cursor mode.");
    }
}

public sealed record EditorToolCursorHint(
    string Mode,
    string CursorKey,
    string StatusText);
