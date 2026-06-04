namespace DragonCAD.Fabrication.Bom;

public sealed record BomLine(BomPartIdentity Identity, IReadOnlyList<string> References, string Notes = "")
{
    public int Quantity => References.Count;
}
