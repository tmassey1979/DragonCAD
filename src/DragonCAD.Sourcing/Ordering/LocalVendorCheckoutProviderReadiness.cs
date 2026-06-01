namespace DragonCAD.Sourcing.Ordering;

public sealed record LocalVendorCheckoutProviderReadiness(
    string ProviderId,
    string DisplayName,
    IReadOnlyList<LocalVendorCheckoutBlocker> Blockers,
    LocalVendorCheckoutAction Action)
{
    public bool IsReadyForLiveOrder => Blockers.Count == 0;
}
