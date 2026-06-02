namespace DragonCAD.Fabrication.Outputs.Assembly;

public sealed record AssemblyComponent
{
    private AssemblyComponent(
        string reference,
        string value,
        string manufacturerPartNumber,
        string package,
        string footprint,
        string sourcingStatus,
        long? x,
        long? y,
        int? rotation,
        AssemblyPlacementSide? side,
        string placementStatus)
    {
        Reference = Normalize(reference);
        Value = Normalize(value);
        ManufacturerPartNumber = Normalize(manufacturerPartNumber);
        Package = Normalize(package);
        Footprint = Normalize(footprint);
        SourcingStatus = Normalize(sourcingStatus);
        X = x;
        Y = y;
        Rotation = rotation;
        Side = side;
        PlacementStatus = Normalize(placementStatus);
    }

    public string Reference { get; }

    public string Value { get; }

    public string ManufacturerPartNumber { get; }

    public string Package { get; }

    public string Footprint { get; }

    public string SourcingStatus { get; }

    public long? X { get; }

    public long? Y { get; }

    public int? Rotation { get; }

    public AssemblyPlacementSide? Side { get; }

    public string PlacementStatus { get; }

    public static AssemblyComponent Placed(
        string reference,
        string value,
        string manufacturerPartNumber,
        string package,
        string footprint,
        string sourcingStatus,
        long x,
        long y,
        int rotation,
        AssemblyPlacementSide side,
        string placementStatus)
    {
        return new(
            reference,
            value,
            manufacturerPartNumber,
            package,
            footprint,
            sourcingStatus,
            x,
            y,
            rotation,
            side,
            placementStatus);
    }

    public static AssemblyComponent Unplaced(
        string reference,
        string value,
        string manufacturerPartNumber,
        string package,
        string footprint,
        string sourcingStatus,
        string placementStatus)
    {
        return new(
            reference,
            value,
            manufacturerPartNumber,
            package,
            footprint,
            sourcingStatus,
            x: null,
            y: null,
            rotation: null,
            side: null,
            placementStatus);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
