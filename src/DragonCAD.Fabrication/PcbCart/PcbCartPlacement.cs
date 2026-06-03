namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartPlacement
{
    private PcbCartPlacement(string designator, PcbCartAssemblySide side)
    {
        Designator = designator;
        Side = side;
    }

    public string Designator { get; }

    public PcbCartAssemblySide Side { get; }

    public static PcbCartPlacement Create(string designator, PcbCartAssemblySide side)
    {
        if (string.IsNullOrWhiteSpace(designator))
        {
            throw new ArgumentException("Designator must not be empty.", nameof(designator));
        }

        if (side == PcbCartAssemblySide.None)
        {
            throw new ArgumentException("Placement side must identify a board side.", nameof(side));
        }

        return new PcbCartPlacement(designator.Trim(), side);
    }
}
