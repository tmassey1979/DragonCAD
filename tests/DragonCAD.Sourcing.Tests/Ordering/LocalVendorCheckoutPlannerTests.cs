using DragonCAD.Sourcing.Ordering;

namespace DragonCAD.Sourcing.Tests.Ordering;

public sealed class LocalVendorCheckoutPlannerTests
{
    [Fact]
    public void PlanReportsCredentialShippingAndPaymentBlockersForProvider()
    {
        var plan = LocalVendorCheckoutPlanner.Plan(
            [
                new LocalVendorCheckoutProvider(
                    ProviderId: "digikey",
                    DisplayName: "Digi-Key",
                    HasCredentials: false,
                    HasShippingAddress: false,
                    HasPaymentMethod: false),
            ],
            LocalVendorCheckoutMode.LiveOrder);

        var provider = Assert.Single(plan.Providers);
        Assert.False(provider.IsReadyForLiveOrder);
        Assert.Equal(LocalVendorCheckoutAction.Blocked, provider.Action);
        Assert.Equal(
            [
                LocalVendorCheckoutBlockerKind.MissingCredentials,
                LocalVendorCheckoutBlockerKind.MissingShippingAddress,
                LocalVendorCheckoutBlockerKind.MissingPaymentMethod,
            ],
            provider.Blockers.Select(blocker => blocker.Kind));
    }

    [Fact]
    public void PlanKeepsCompleteProvidersReviewOnlyWhenModeDisallowsLiveOrders()
    {
        var plan = LocalVendorCheckoutPlanner.Plan(
            [
                new LocalVendorCheckoutProvider(
                    ProviderId: "mouser",
                    DisplayName: "Mouser",
                    HasCredentials: true,
                    HasShippingAddress: true,
                    HasPaymentMethod: true),
            ],
            LocalVendorCheckoutMode.ReviewOnly);

        var provider = Assert.Single(plan.Providers);
        Assert.True(plan.IsReviewOnly);
        Assert.False(plan.AllowsLiveOrders);
        Assert.True(provider.IsReadyForLiveOrder);
        Assert.Empty(provider.Blockers);
        Assert.Equal(LocalVendorCheckoutAction.ReviewOnly, provider.Action);
    }

    [Fact]
    public void PlanAllowsLiveOrdersOnlyWhenEveryProviderIsReadyInLiveOrderMode()
    {
        var plan = LocalVendorCheckoutPlanner.Plan(
            [
                new LocalVendorCheckoutProvider(
                    ProviderId: "jameco",
                    DisplayName: "Jameco",
                    HasCredentials: true,
                    HasShippingAddress: true,
                    HasPaymentMethod: true),
            ],
            LocalVendorCheckoutMode.LiveOrder);

        var provider = Assert.Single(plan.Providers);
        Assert.False(plan.IsReviewOnly);
        Assert.True(plan.AllowsLiveOrders);
        Assert.True(provider.IsReadyForLiveOrder);
        Assert.Equal(LocalVendorCheckoutAction.ReadyForLiveOrder, provider.Action);
    }
}
