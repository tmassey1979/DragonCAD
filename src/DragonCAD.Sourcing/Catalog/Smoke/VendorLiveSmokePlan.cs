namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed record VendorLiveSmokePlan(
    VendorLiveSmokeMode Mode,
    IReadOnlyList<VendorLiveSmokeProviderCheck> ProviderChecks);
