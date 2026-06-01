namespace DragonCAD.Fabrication.Bom;

public sealed record BomPartIdentity(string Part, string Value, string Package)
{
    public static BomPartIdentity Unspecified { get; } = new(string.Empty, string.Empty, string.Empty);
}
