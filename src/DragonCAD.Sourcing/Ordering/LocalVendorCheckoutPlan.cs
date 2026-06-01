namespace DragonCAD.Sourcing.Ordering;

public sealed record LocalVendorCheckoutPlan(
    LocalVendorCheckoutMode Mode,
    IReadOnlyList<LocalVendorCheckoutProviderReadiness> Providers)
{
    public bool IsReviewOnly => Mode == LocalVendorCheckoutMode.ReviewOnly;

    public bool AllowsLiveOrders =>
        Mode == LocalVendorCheckoutMode.LiveOrder
        && Providers.Count > 0
        && Providers.All(provider => provider.IsReadyForLiveOrder);
}
