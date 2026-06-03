using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartProductionHandoffRequest
{
    private PcbCartProductionHandoffRequest(
        ManufacturingOutputManifest manifest,
        int quantity,
        PcbCartBoardFinish finish,
        PcbCartBoardStackupSummary? stackup,
        PcbCartAssemblySide assemblySide,
        string notes,
        PcbCartBomItem[] bomItems,
        PcbCartPlacement[] placements)
    {
        Manifest = manifest;
        Quantity = quantity;
        Finish = finish;
        Stackup = stackup;
        AssemblySide = assemblySide;
        Notes = notes;
        BomItems = bomItems;
        Placements = placements;
    }

    public ManufacturingOutputManifest Manifest { get; }

    public int Quantity { get; }

    public PcbCartBoardFinish Finish { get; }

    public PcbCartBoardStackupSummary? Stackup { get; }

    public PcbCartAssemblySide AssemblySide { get; }

    public string Notes { get; }

    public IReadOnlyList<PcbCartBomItem> BomItems { get; }

    public IReadOnlyList<PcbCartPlacement> Placements { get; }

    public bool IncludesAssembly => AssemblySide != PcbCartAssemblySide.None;

    public static PcbCartProductionHandoffRequest Create(
        ManufacturingOutputManifest manifest,
        int quantity,
        PcbCartBoardFinish finish,
        PcbCartBoardStackupSummary? stackup,
        PcbCartAssemblySide assemblySide,
        string? notes = null,
        IEnumerable<PcbCartBomItem>? bomItems = null,
        IEnumerable<PcbCartPlacement>? placements = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be at least 1.");
        }

        return new PcbCartProductionHandoffRequest(
            manifest,
            quantity,
            finish,
            stackup,
            assemblySide,
            notes?.Trim() ?? string.Empty,
            (bomItems ?? []).OrderBy(item => item.Designator, StringComparer.Ordinal).ToArray(),
            (placements ?? []).OrderBy(placement => placement.Designator, StringComparer.Ordinal).ToArray());
    }
}
