using System.Globalization;

namespace DragonCAD.App.Marketplace.Cart.Ordering;

public sealed class MarketplaceOrderPlanViewModel
{
    private MarketplaceOrderPlanViewModel(
        IReadOnlyList<MarketplaceOrderProviderViewModel> providers,
        IReadOnlyList<MarketplaceOrderDiagnosticViewModel> unavailableDiagnostics)
    {
        Providers = providers;
        UnavailableDiagnostics = unavailableDiagnostics;
    }

    public IReadOnlyList<MarketplaceOrderProviderViewModel> Providers { get; }

    public IReadOnlyList<MarketplaceOrderDiagnosticViewModel> UnavailableDiagnostics { get; }

    public int ItemCount => Providers.Sum(provider => provider.ItemCount);

    public int UnitCount => Providers.Sum(provider => provider.UnitCount);

    public decimal TotalUsd => Providers.Sum(provider => provider.SubtotalUsd);

    public string TotalSummary => FormatCurrency(TotalUsd);

    public static MarketplaceOrderPlanViewModel FromCart(MarketplaceCartViewModel cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        MarketplaceOrderProviderViewModel[] providers = cart.Lines
            .GroupBy(line => line.Provider)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => MarketplaceOrderProviderViewModel.FromLines(group.Key, group))
            .ToArray();

        MarketplaceOrderDiagnosticViewModel[] diagnostics = cart.Diagnostics
            .Select(diagnostic => new MarketplaceOrderDiagnosticViewModel(
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.Provider,
                diagnostic.ManufacturerPartNumber))
            .ToArray();

        return new MarketplaceOrderPlanViewModel(providers, diagnostics);
    }

    internal static string FormatCurrency(decimal value) =>
        value.ToString("$0.00##", CultureInfo.InvariantCulture);
}

public sealed class MarketplaceOrderProviderViewModel
{
    private MarketplaceOrderProviderViewModel(
        string provider,
        IReadOnlyList<MarketplaceCartLine> lines,
        string actionLabel,
        string actionUrl)
    {
        Provider = provider;
        Lines = lines;
        ActionLabel = actionLabel;
        ActionUrl = actionUrl;
    }

    public string Provider { get; }

    public IReadOnlyList<MarketplaceCartLine> Lines { get; }

    public int ItemCount => Lines.Count;

    public int UnitCount => Lines.Sum(line => line.Quantity);

    public decimal SubtotalUsd => Lines.Sum(line => line.SubtotalUsd);

    public string SubtotalSummary => MarketplaceOrderPlanViewModel.FormatCurrency(SubtotalUsd);

    public string ActionLabel { get; }

    public string ActionUrl { get; }

    public static MarketplaceOrderProviderViewModel FromLines(
        string provider,
        IEnumerable<MarketplaceCartLine> lines)
    {
        MarketplaceCartLine[] orderedLines = lines
            .OrderBy(line => line.DisplayName, StringComparer.Ordinal)
            .ThenBy(line => line.ManufacturerPartNumber, StringComparer.Ordinal)
            .ToArray();

        MarketplaceOrderProviderAction action = MarketplaceOrderProviderAction.ForProvider(provider);
        return new MarketplaceOrderProviderViewModel(provider, orderedLines, action.Label, action.Url);
    }
}

public sealed record MarketplaceOrderDiagnosticViewModel(
    string Code,
    string Message,
    string Provider,
    string ManufacturerPartNumber);

public sealed class MarketplaceInAppOrderDraftViewModel
{
    private MarketplaceInAppOrderDraftViewModel(
        string draftId,
        IReadOnlyList<MarketplaceProviderOrderDraftViewModel> providerOrders,
        IReadOnlyList<MarketplaceOrderDiagnosticViewModel> diagnostics)
    {
        DraftId = draftId;
        ProviderOrders = providerOrders;
        Diagnostics = diagnostics;
    }

    public string DraftId { get; }

    public string Status => ProviderOrders.Count == 0
        ? "Add BOM items before checkout"
        : "Ready for in-app checkout review";

    public IReadOnlyList<MarketplaceProviderOrderDraftViewModel> ProviderOrders { get; }

    public IReadOnlyList<MarketplaceOrderDiagnosticViewModel> Diagnostics { get; }

    public decimal TotalUsd => ProviderOrders.Sum(provider => provider.SubtotalUsd);

    public string TotalSummary => MarketplaceOrderPlanViewModel.FormatCurrency(TotalUsd);

    public static MarketplaceInAppOrderDraftViewModel Create(
        string draftId,
        MarketplaceOrderPlanViewModel orderPlan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);
        ArgumentNullException.ThrowIfNull(orderPlan);

        MarketplaceProviderOrderDraftViewModel[] providers = orderPlan.Providers
            .Select(MarketplaceProviderOrderDraftViewModel.FromProvider)
            .ToArray();

        return new MarketplaceInAppOrderDraftViewModel(draftId, providers, orderPlan.UnavailableDiagnostics);
    }
}

public sealed class MarketplaceProviderOrderDraftViewModel
{
    private MarketplaceProviderOrderDraftViewModel(
        string provider,
        IReadOnlyList<MarketplaceCartLine> lines,
        decimal subtotalUsd)
    {
        Provider = provider;
        Lines = lines;
        SubtotalUsd = subtotalUsd;
    }

    public string Provider { get; }

    public IReadOnlyList<MarketplaceCartLine> Lines { get; }

    public int LineCount => Lines.Count;

    public int UnitCount => Lines.Sum(line => line.Quantity);

    public decimal SubtotalUsd { get; }

    public string SubtotalSummary => MarketplaceOrderPlanViewModel.FormatCurrency(SubtotalUsd);

    public string ActionLabel => "Review order in DragonCAD";

    public static MarketplaceProviderOrderDraftViewModel FromProvider(MarketplaceOrderProviderViewModel provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return new MarketplaceProviderOrderDraftViewModel(provider.Provider, provider.Lines, provider.SubtotalUsd);
    }
}

public sealed class MarketplaceCheckoutReadinessViewModel
{
    private MarketplaceCheckoutReadinessViewModel(IReadOnlyList<MarketplaceCheckoutBlockerViewModel> blockers)
    {
        Blockers = blockers;
    }

    public IReadOnlyList<MarketplaceCheckoutBlockerViewModel> Blockers { get; }

    public bool CanPlaceOrder => Blockers.Count == 0;

    public string Status => CanPlaceOrder
        ? "Ready for in-app order placement"
        : "Blocked: checkout setup required";

    public string PrimaryActionLabel => CanPlaceOrder
        ? "Place order inside DragonCAD"
        : $"Resolve {Blockers.Count} checkout blockers";

    public static MarketplaceCheckoutReadinessViewModel FromDraft(
        MarketplaceInAppOrderDraftViewModel draft,
        bool hasShippingProfile,
        bool hasPaymentMethod,
        IReadOnlySet<string> providersWithCredentials)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(providersWithCredentials);

        List<MarketplaceCheckoutBlockerViewModel> blockers = [];
        if (!hasShippingProfile)
        {
            blockers.Add(new MarketplaceCheckoutBlockerViewModel(
                "ShippingProfileMissing",
                "Add a shipping profile before placing orders.",
                ""));
        }

        if (!hasPaymentMethod)
        {
            blockers.Add(new MarketplaceCheckoutBlockerViewModel(
                "PaymentMethodMissing",
                "Add a payment method before placing orders.",
                ""));
        }

        foreach (MarketplaceProviderOrderDraftViewModel providerOrder in draft.ProviderOrders)
        {
            if (!providersWithCredentials.Contains(providerOrder.Provider))
            {
                blockers.Add(new MarketplaceCheckoutBlockerViewModel(
                    "ProviderCredentialsMissing",
                    $"Add {providerOrder.Provider} credentials before placing this provider order.",
                    providerOrder.Provider));
            }
        }

        return new MarketplaceCheckoutReadinessViewModel(blockers);
    }
}

public sealed record MarketplaceCheckoutBlockerViewModel(
    string Code,
    string Message,
    string Provider);

public sealed class MarketplacePlacedOrderViewModel
{
    private MarketplacePlacedOrderViewModel(
        string orderId,
        string draftId,
        IReadOnlyList<MarketplacePlacedProviderOrderViewModel> providerOrders)
    {
        OrderId = orderId;
        DraftId = draftId;
        ProviderOrders = providerOrders;
    }

    public string OrderId { get; }

    public string DraftId { get; }

    public string Status => "Local order record created";

    public IReadOnlyList<MarketplacePlacedProviderOrderViewModel> ProviderOrders { get; }

    public decimal TotalUsd => ProviderOrders.Sum(provider => provider.SubtotalUsd);

    public string TotalSummary => MarketplaceOrderPlanViewModel.FormatCurrency(TotalUsd);

    public static MarketplacePlacedOrderViewModel CreateLocalRecord(
        string orderId,
        MarketplaceInAppOrderDraftViewModel draft,
        MarketplaceCheckoutReadinessViewModel readiness)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(readiness);

        if (!readiness.CanPlaceOrder)
        {
            throw new InvalidOperationException("Checkout readiness is blocked; resolve setup blockers before creating an order record.");
        }

        MarketplacePlacedProviderOrderViewModel[] providerOrders = draft.ProviderOrders
            .Select(MarketplacePlacedProviderOrderViewModel.FromDraft)
            .ToArray();

        return new MarketplacePlacedOrderViewModel(orderId, draft.DraftId, providerOrders);
    }
}

public sealed class MarketplacePlacedProviderOrderViewModel
{
    private MarketplacePlacedProviderOrderViewModel(
        string provider,
        int lineCount,
        int unitCount,
        decimal subtotalUsd)
    {
        Provider = provider;
        LineCount = lineCount;
        UnitCount = unitCount;
        SubtotalUsd = subtotalUsd;
    }

    public string Provider { get; }

    public int LineCount { get; }

    public int UnitCount { get; }

    public decimal SubtotalUsd { get; }

    public string SubtotalSummary => MarketplaceOrderPlanViewModel.FormatCurrency(SubtotalUsd);

    public string ProviderSubmissionStatus => "No live vendor order was placed.";

    public static MarketplacePlacedProviderOrderViewModel FromDraft(MarketplaceProviderOrderDraftViewModel providerOrder)
    {
        ArgumentNullException.ThrowIfNull(providerOrder);

        return new MarketplacePlacedProviderOrderViewModel(
            providerOrder.Provider,
            providerOrder.LineCount,
            providerOrder.UnitCount,
            providerOrder.SubtotalUsd);
    }
}

internal sealed record MarketplaceOrderProviderAction(string Label, string Url)
{
    public static MarketplaceOrderProviderAction ForProvider(string provider) =>
        provider switch
        {
            "Adafruit" => new("Open Adafruit cart", "https://www.adafruit.com/shopping_cart"),
            "Digi-Key" => new("Open Digi-Key cart", "https://www.digikey.com/ordering/shoppingcart"),
            "Jameco" => new("Open Jameco cart", "https://www.jameco.com/webapp/wcs/stores/servlet/OrderItemDisplay"),
            "Mouser" => new("Open Mouser cart", "https://www.mouser.com/cart"),
            "SparkFun" => new("Open SparkFun cart", "https://www.sparkfun.com/cart"),
            _ => new($"Open {provider} cart", string.Empty),
        };
}
