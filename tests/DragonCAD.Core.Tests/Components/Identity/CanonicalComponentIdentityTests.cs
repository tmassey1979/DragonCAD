using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Tests.Components.Identity;

public sealed class CanonicalComponentIdentityTests
{
    [Fact]
    public void CanonicalIdentityCapturesRequiredIdentityFields()
    {
        var identity = new CanonicalComponentIdentity(
            new ComponentId("canonical:lm7805"),
            "Texas Instruments",
            "LM7805CT",
            "7805 regulator",
            "5 V",
            "4%",
            "35 V",
            "1 A",
            "TO-220",
            3,
            "through-hole regulator",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);

        Assert.Equal("LM7805CT", identity.ManufacturerPartNumber);
        Assert.Equal("7805-regulator", identity.NormalizedGenericFamily);
        Assert.Equal("5-v", identity.NormalizedElectricalValue);
        Assert.Equal("4%", identity.Tolerance);
        Assert.Equal("35-v", identity.NormalizedVoltageRating);
        Assert.Equal("1-a", identity.NormalizedCurrentRating);
        Assert.Equal("TO-220", identity.Package);
        Assert.Equal(3, identity.PinCount);
        Assert.Equal("through-hole-regulator", identity.NormalizedFootprintClass);
        Assert.Equal(ComponentLifecycle.Active, identity.Lifecycle);
        Assert.Equal(ComponentSourceConfidence.Verified, identity.SourceConfidence);
    }

    [Fact]
    public void VendorOffersAttachWithoutReplacingVerifiedGeometry()
    {
        var geometry = new VerifiedComponentGeometry(
            new ComponentSymbolId("symbol:regulator-3pin"),
            new ComponentFootprintId("footprint:to-220-3"),
            "reviewed from datasheet");
        CanonicalComponentIdentity identity = Lm7805().WithVerifiedGeometry(geometry);
        var offer = new VendorComponentOffer("Digi-Key", "296-13996-5-ND", "Texas Instruments", "LM7805CT", "5 V", "TO-220");

        CanonicalComponentIdentity updated = identity.AttachOffer(offer);

        Assert.Same(geometry, updated.VerifiedGeometry);
        Assert.Equal([offer], updated.VendorOffers);
    }

    [Fact]
    public void DedupeClassifies7805VendorPartAsExactMatch()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Lm7805(),
            new VendorComponentOffer("Mouser", "595-LM7805CT", "Texas Instruments", "LM7805CT", "5V", "TO220"));

        Assert.Equal(ComponentDedupeSuggestionKind.ExactMatch, suggestion.Kind);
        Assert.Equal("mpn", suggestion.Reason);
    }

    [Fact]
    public void DedupeClassifiesAlternate7805RegulatorsAsLikelyAlternates()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Lm7805(),
            new VendorComponentOffer("SparkFun", "COM-00107", "STMicroelectronics", "L7805CV", "5 V", "TO-220"));

        Assert.Equal(ComponentDedupeSuggestionKind.LikelyAlternate, suggestion.Kind);
        Assert.Equal("family-value-package", suggestion.Reason);
    }

    [Fact]
    public void DedupeClassifiesNe555PartsAsLikelyAlternates()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Ne555(),
            new VendorComponentOffer("Jameco", "27422", "Signetics", "SE555", "Timer", "PDIP-8", pinCount: 8));

        Assert.Equal(ComponentDedupeSuggestionKind.LikelyAlternate, suggestion.Kind);
    }

    [Fact]
    public void DedupeClassifiesEsp32DevBoardsAsLikelyAlternates()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Esp32DevBoard(),
            new VendorComponentOffer("Mouser", "356-ESP32-DEVKITC32E", "Espressif Systems", "ESP32 DevKitC V4", "ESP32-WROOM-32E", "DevKitC"));

        Assert.Equal(ComponentDedupeSuggestionKind.LikelyAlternate, suggestion.Kind);
    }

    [Fact]
    public void DedupeClassifiesResistorValueDifferencesAsValueVariants()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Resistor("canonical:resistor-10k-0603", "10 kOhm", "0603"),
            new VendorComponentOffer("Digi-Key", "311-1.00KHRCT-ND", "Yageo", "RC0603FR-071KL", "1 kOhm", "0603", tolerance: "1%"));

        Assert.Equal(ComponentDedupeSuggestionKind.ValueVariant, suggestion.Kind);
    }

    [Fact]
    public void DedupeClassifiesCapacitorValueDifferencesAsValueVariants()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Capacitor("canonical:capacitor-100nf-0603", "100 nF", "0603"),
            new VendorComponentOffer("Digi-Key", "1276-1044-1-ND", "Murata", "GRM188R71H103KA01D", "10 nF", "0603", tolerance: "10%", voltageRating: "50 V"));

        Assert.Equal(ComponentDedupeSuggestionKind.ValueVariant, suggestion.Kind);
    }

    [Fact]
    public void DedupeClassifiesSameValueDifferentPackageAsPackageVariant()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Resistor("canonical:resistor-10k-0603", "10 kOhm", "0603"),
            new VendorComponentOffer("Digi-Key", "311-10.0KCRCT-ND", "Yageo", "RC0805FR-0710KL", "10 kOhm", "0805", tolerance: "1%"));

        Assert.Equal(ComponentDedupeSuggestionKind.PackageVariant, suggestion.Kind);
    }

    [Fact]
    public void DedupeClassifiesSameMpnDifferentPackageAsConflict()
    {
        var suggestion = CanonicalComponentDedupeSuggester.Suggest(
            Lm7805(),
            new VendorComponentOffer("BadFeed", "BAD-LM7805CT", "Texas Instruments", "LM7805CT", "5 V", "SOT-223", pinCount: 4));

        Assert.Equal(ComponentDedupeSuggestionKind.Conflict, suggestion.Kind);
        Assert.Equal("mpn-conflict", suggestion.Reason);
    }

    private static CanonicalComponentIdentity Lm7805() =>
        new(
            new ComponentId("canonical:lm7805"),
            "Texas Instruments",
            "LM7805CT",
            "7805 regulator",
            "5 V",
            "4%",
            "35 V",
            "1 A",
            "TO-220",
            3,
            "through-hole regulator",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);

    private static CanonicalComponentIdentity Ne555() =>
        new(
            new ComponentId("canonical:ne555"),
            "Texas Instruments",
            "NE555P",
            "555 timer",
            "Timer",
            null,
            "16 V",
            null,
            "PDIP-8",
            8,
            "dip logic",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);

    private static CanonicalComponentIdentity Esp32DevBoard() =>
        new(
            new ComponentId("canonical:esp32-devkitc"),
            "Espressif",
            "ESP32-DEVKITC-32E",
            "ESP32 dev board",
            "ESP32-WROOM-32E",
            null,
            "5 V",
            null,
            "DevKitC",
            null,
            "development board",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);

    private static CanonicalComponentIdentity Resistor(string id, string value, string package) =>
        new(
            new ComponentId(id),
            "Yageo",
            "RC0603FR-0710KL",
            "resistor",
            value,
            "1%",
            "75 V",
            null,
            package,
            2,
            "chip resistor",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);

    private static CanonicalComponentIdentity Capacitor(string id, string value, string package) =>
        new(
            new ComponentId(id),
            "Murata",
            "GRM188R71H104KA93D",
            "capacitor",
            value,
            "10%",
            "50 V",
            null,
            package,
            2,
            "mlcc",
            ComponentLifecycle.Active,
            ComponentSourceConfidence.Verified);
}
