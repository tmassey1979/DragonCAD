namespace DragonCAD.Fabrication.PickAndPlace;

public sealed record ComponentPlacementRow
{
    public ComponentPlacementRow(
        string reference,
        string value,
        string package,
        long x,
        long y,
        int rotation,
        PlacementSide side)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Reference is required.", nameof(reference));
        }

        if (rotation is not (0 or 90 or 180 or 270))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Rotation must be 0, 90, 180, or 270 degrees.");
        }

        Reference = reference.Trim();
        Value = value ?? string.Empty;
        Package = package ?? string.Empty;
        X = x;
        Y = y;
        Rotation = rotation;
        Side = side;
    }

    public string Reference { get; }

    public string Value { get; }

    public string Package { get; }

    public long X { get; }

    public long Y { get; }

    public int Rotation { get; }

    public PlacementSide Side { get; }
}
