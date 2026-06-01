namespace DragonCAD.Sourcing.Bom;

public sealed record BomCostRollupLine(
    BomComponentQuantity Component,
    IReadOnlyList<BomProviderOffer> ProviderOffers,
    BomProviderOffer? SelectedOffer);
