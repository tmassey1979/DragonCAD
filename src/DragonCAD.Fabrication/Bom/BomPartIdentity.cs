namespace DragonCAD.Fabrication.Bom;

public sealed record BomPartIdentity(string Part, string Value, string Package, string ManufacturerPartNumber = "")
{
    public static BomPartIdentity Unspecified { get; } = new(string.Empty, string.Empty, string.Empty);
}
