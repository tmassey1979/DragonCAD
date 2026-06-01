namespace DragonCAD.Sourcing.Ordering;

public static class LocalVendorCheckoutPlanner
{
    public static LocalVendorCheckoutPlan Plan(
        IEnumerable<LocalVendorCheckoutProvider> providers,
        LocalVendorCheckoutMode mode)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var readiness = providers
            .Select(provider => PlanProvider(provider, mode))
            .OrderBy(provider => provider.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LocalVendorCheckoutPlan(mode, readiness);
    }

    private static LocalVendorCheckoutProviderReadiness PlanProvider(
        LocalVendorCheckoutProvider provider,
        LocalVendorCheckoutMode mode)
    {
        var blockers = FindBlockers(provider);
        var action = blockers.Count > 0
            ? LocalVendorCheckoutAction.Blocked
            : mode == LocalVendorCheckoutMode.ReviewOnly
                ? LocalVendorCheckoutAction.ReviewOnly
                : LocalVendorCheckoutAction.ReadyForLiveOrder;

        return new LocalVendorCheckoutProviderReadiness(
            provider.ProviderId,
            provider.DisplayName,
            blockers,
            action);
    }

    private static IReadOnlyList<LocalVendorCheckoutBlocker> FindBlockers(LocalVendorCheckoutProvider provider)
    {
        var blockers = new List<LocalVendorCheckoutBlocker>();

        if (!provider.HasCredentials)
        {
            blockers.Add(new LocalVendorCheckoutBlocker(
                LocalVendorCheckoutBlockerKind.MissingCredentials,
                $"{provider.DisplayName} requires vendor API or account credentials before checkout."));
        }

        if (!provider.HasShippingAddress)
        {
            blockers.Add(new LocalVendorCheckoutBlocker(
                LocalVendorCheckoutBlockerKind.MissingShippingAddress,
                $"{provider.DisplayName} requires a shipping address before checkout."));
        }

        if (!provider.HasPaymentMethod)
        {
            blockers.Add(new LocalVendorCheckoutBlocker(
                LocalVendorCheckoutBlockerKind.MissingPaymentMethod,
                $"{provider.DisplayName} requires a payment method before checkout."));
        }

        return blockers;
    }
}
