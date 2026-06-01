namespace DragonCAD.Fabrication.Bom;

public sealed record BomLine(BomPartIdentity Identity, IReadOnlyList<string> References)
{
    public int Quantity => References.Count;
}
