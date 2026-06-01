using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.App.Marketplace.Cart.Ordering;

namespace DragonCAD.App.Tests.Marketplace.Cart.Ordering;

public sealed class MarketplaceOrderPlanViewModelTests
{
    [Fact]
    public void FromCartGroupsPurchasableLinesByProviderWithDeterministicSummaries()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Mouser", "555 Timer", "ST", "NE555N", "dragon:555", price: 0.51m), quantity: 2);
        cart.AddItem(Row("Digi-Key", "10k Resistor", "Yageo", "CFR-25JB-52-10K", "dragon:r-10k", price: 0.021m), quantity: 10);
        cart.AddItem(Row("Adafruit", "ESP32 Feather", "Espressif", "HUZZAH32", "dragon:esp32-feather", price: 19.95m), quantity: 1);
        cart.AddItem(Row("Digi-Key", "7805 Regulator", "Texas Instruments", "LM7805CT", "dragon:7805", price: 0.8125m), quantity: 4);

        MarketplaceOrderPlanViewModel plan = MarketplaceOrderPlanViewModel.FromCart(cart);

        Assert.Equal(["Adafruit", "Digi-Key", "Mouser"], plan.Providers.Select(provider => provider.Provider));
        Assert.Equal(4, plan.ItemCount);
        Assert.Equal(17, plan.UnitCount);
        Assert.Equal(24.43m, plan.TotalUsd);
        Assert.Equal("$24.43", plan.TotalSummary);
        Assert.Equal("Plan ready", plan.Status);
        Assert.Equal("Review draft", plan.PrimaryActionLabel);

        MarketplaceOrderProviderViewModel digiKey = plan.Providers[1];
        Assert.Equal(2, digiKey.ItemCount);
        Assert.Equal(14, digiKey.UnitCount);
        Assert.Equal(3.46m, digiKey.SubtotalUsd);
        Assert.Equal("$3.46", digiKey.SubtotalSummary);
        Assert.Equal(["10k Resistor", "7805 Regulator"], digiKey.Lines.Select(line => line.DisplayName));
    }

    [Fact]
    public void FromCartExposesKnownProviderActionLabelsAndUrls()
    {
        MarketplaceCartViewModel cart = new();

        cart.AddItem(Row("Digi-Key", "Timer", "ST", "NE555N", "dragon:digikey"), quantity: 1);
        cart.AddItem(Row("Mouser", "Regulator", "TI", "LM7805CT", "dragon:mouser"), quantity: 1);
        cart.AddItem(Row("Jameco", "Resistor", "Yageo", "CF14JT10K0", "dragon:jameco"), quantity: 1);
        cart.AddItem(Row("SparkFun", "Breakout", "SparkFun", "BOB-15100", "dragon:sparkfun"), quantity: 1);
        cart.AddItem(Row("Adafruit", "Feather", "Adafruit", "3405", "dragon:adafruit"), quantity: 1);

        MarketplaceOrderPlanViewModel plan = MarketplaceOrderPlanViewModel.FromCart(cart);

        Assert.Equal(
            [
                ("Adafruit", "Open Adafruit cart", "https://www.adafruit.com/shopping_cart"),
                ("Digi-Key", "Open Digi-Key cart", "https://www.digikey.com/ordering/shoppingcart"),
                ("Jameco", "Open Jameco cart", "https://www.jameco.com/webapp/wcs/stores/servlet/OrderItemDisplay"),
                ("Mouser", "Open Mouser cart", "https://www.mouser.com/cart"),
                ("SparkFun", "Open SparkFun cart", "https://www.sparkfun.com/cart")
            ],
            plan.Providers.Select(provider => (provider.Provider, provider.ActionLabel, provider.ActionUrl)));
    }

    [Fact]
    public void FromCartLabelsEmptyOrderPlanWithCompactNextAction()
    {
        MarketplaceCartViewModel cart = new();

        MarketplaceOrderPlanViewModel plan = MarketplaceOrderPlanViewModel.FromCart(cart);

        Assert.Empty(plan.Providers);
        Assert.Equal("Cart empty", plan.Status);
        Assert.Equal("Add items", plan.PrimaryActionLabel);
    }

    [Fact]
    public void FromCartCarriesUnavailableDiagnosticsWithoutCreatingProviderOrders()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("SparkFun", "USB-C Breakout", "SparkFun", "BOB-15100", "dragon:usb-c", price: 5.95m), quantity: 2);
        cart.AddItem(Row("Jameco", "Obsolete Timer", "Dragon Test", "OLD555", "dragon:old555", stockQuantity: 0, price: null), quantity: 1);

        MarketplaceOrderPlanViewModel plan = MarketplaceOrderPlanViewModel.FromCart(cart);

        MarketplaceOrderDiagnosticViewModel diagnostic = Assert.Single(plan.UnavailableDiagnostics);
        Assert.Equal("Unavailable", diagnostic.Code);
        Assert.Equal("Jameco", diagnostic.Provider);
        Assert.Equal("OLD555", diagnostic.ManufacturerPartNumber);
        Assert.Contains("Obsolete Timer", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(["SparkFun"], plan.Providers.Select(provider => provider.Provider));
    }

    [Fact]
    public void InAppOrderDraftSnapshotsProviderOrdersForReviewWithoutLeavingTheApp()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Digi-Key", "7805 Regulator", "Texas Instruments", "LM7805CT", "dragon:7805", price: 0.8125m), quantity: 4);
        cart.AddItem(Row("Mouser", "555 Timer", "ST", "NE555N", "dragon:555", price: 0.51m), quantity: 2);

        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-0001",
            MarketplaceOrderPlanViewModel.FromCart(cart));

        Assert.Equal("DRAFT-0001", draft.DraftId);
        Assert.Equal("Draft ready", draft.Status);
        Assert.Equal("Place order", draft.PrimaryActionLabel);
        Assert.Equal("$4.27", draft.TotalSummary);
        Assert.Equal(2, draft.ProviderOrders.Count);
        Assert.Equal(["Digi-Key", "Mouser"], draft.ProviderOrders.Select(provider => provider.Provider));
        Assert.All(draft.ProviderOrders, provider => Assert.Equal("Review order in DragonCAD", provider.ActionLabel));
        Assert.Contains(draft.ProviderOrders, provider => provider.Provider == "Digi-Key" && provider.LineCount == 1 && provider.UnitCount == 4);
    }

    [Fact]
    public void InAppOrderDraftLabelsEmptyDraftWithCompactNextAction()
    {
        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-EMPTY",
            MarketplaceOrderPlanViewModel.FromCart(new MarketplaceCartViewModel()));

        Assert.Equal("Cart empty", draft.Status);
        Assert.Equal("Add items", draft.PrimaryActionLabel);
    }

    [Fact]
    public void CheckoutReadinessBlocksOrderPlacementUntilRequiredInAppPrerequisitesExist()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Digi-Key", "7805 Regulator", "Texas Instruments", "LM7805CT", "dragon:7805", price: 0.8125m), quantity: 4);
        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-0001",
            MarketplaceOrderPlanViewModel.FromCart(cart));

        MarketplaceCheckoutReadinessViewModel readiness = MarketplaceCheckoutReadinessViewModel.FromDraft(
            draft,
            hasShippingProfile: false,
            hasPaymentMethod: false,
            providersWithCredentials: new HashSet<string>(StringComparer.Ordinal));

        Assert.False(readiness.CanPlaceOrder);
        Assert.Equal("Blocked: checkout setup required", readiness.Status);
        Assert.Equal("Resolve 3 checkout blockers", readiness.PrimaryActionLabel);
        Assert.Contains(readiness.Blockers, blocker => blocker.Code == "ShippingProfileMissing");
        Assert.Contains(readiness.Blockers, blocker => blocker.Code == "PaymentMethodMissing");
        Assert.Contains(readiness.Blockers, blocker => blocker.Code == "ProviderCredentialsMissing" && blocker.Provider == "Digi-Key");
    }

    [Fact]
    public void CheckoutReadinessAllowsOrderPlacementWhenAllPrerequisitesExist()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Mouser", "555 Timer", "ST", "NE555N", "dragon:555", price: 0.51m), quantity: 2);
        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-0002",
            MarketplaceOrderPlanViewModel.FromCart(cart));

        MarketplaceCheckoutReadinessViewModel readiness = MarketplaceCheckoutReadinessViewModel.FromDraft(
            draft,
            hasShippingProfile: true,
            hasPaymentMethod: true,
            providersWithCredentials: new HashSet<string>(["Mouser"], StringComparer.Ordinal));

        Assert.True(readiness.CanPlaceOrder);
        Assert.Equal("Ready for in-app order placement", readiness.Status);
        Assert.Equal("Place order inside DragonCAD", readiness.PrimaryActionLabel);
        Assert.Empty(readiness.Blockers);
    }

    [Fact]
    public void PlacedOrderRecordSnapshotsReadyDraftWithoutCallingVendors()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Mouser", "555 Timer", "ST", "NE555N", "dragon:555", price: 0.51m), quantity: 2);
        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-0002",
            MarketplaceOrderPlanViewModel.FromCart(cart));
        MarketplaceCheckoutReadinessViewModel readiness = MarketplaceCheckoutReadinessViewModel.FromDraft(
            draft,
            hasShippingProfile: true,
            hasPaymentMethod: true,
            providersWithCredentials: new HashSet<string>(["Mouser"], StringComparer.Ordinal));

        MarketplacePlacedOrderViewModel order = MarketplacePlacedOrderViewModel.CreateLocalRecord("ORDER-0001", draft, readiness);

        Assert.Equal("ORDER-0001", order.OrderId);
        Assert.Equal("DRAFT-0002", order.DraftId);
        Assert.Equal("Order recorded", order.Status);
        Assert.Equal("View order", order.PrimaryActionLabel);
        Assert.Equal("$1.02", order.TotalSummary);
        Assert.Equal(["Mouser"], order.ProviderOrders.Select(provider => provider.Provider));
        Assert.Equal("No live vendor order was placed.", order.ProviderOrders[0].ProviderSubmissionStatus);
    }

    [Fact]
    public void PlacedOrderRecordRejectsBlockedReadiness()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Digi-Key", "7805 Regulator", "Texas Instruments", "LM7805CT", "dragon:7805", price: 0.8125m), quantity: 4);
        MarketplaceInAppOrderDraftViewModel draft = MarketplaceInAppOrderDraftViewModel.Create(
            "DRAFT-0001",
            MarketplaceOrderPlanViewModel.FromCart(cart));
        MarketplaceCheckoutReadinessViewModel readiness = MarketplaceCheckoutReadinessViewModel.FromDraft(
            draft,
            hasShippingProfile: false,
            hasPaymentMethod: false,
            providersWithCredentials: new HashSet<string>(StringComparer.Ordinal));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => MarketplacePlacedOrderViewModel.CreateLocalRecord("ORDER-0001", draft, readiness));

        Assert.Equal("Checkout readiness is blocked; resolve setup blockers before creating an order record.", error.Message);
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string canonicalComponentId,
        int stockQuantity = 100,
        decimal? price = 1.00m) =>
        new(
            Provider: provider,
            Category: "IC",
            DisplayName: displayName,
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: "",
            DatasheetUrl: "",
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: price);
}
