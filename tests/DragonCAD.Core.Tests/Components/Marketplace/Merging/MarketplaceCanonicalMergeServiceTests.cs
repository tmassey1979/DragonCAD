using DragonCAD.Core.Components.Marketplace.Merging;

namespace DragonCAD.Core.Tests.Components.Marketplace.Merging;

public sealed class MarketplaceCanonicalMergeServiceTests
{
    [Fact]
    public void ExactManufacturerPartNumberMatchMergesVendorOffers()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("Digi-Key", "296-13996-5-ND", "Texas Instruments", "LM7805CT", "5 V", "TO-220-3"),
            Fact("Mouser", "595-LM7805CT", "Texas Instruments", "LM7805CT", "5 V", "TO-220-3")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        Assert.Equal("MPN", decision.MatchReason);
        Assert.Equal(["Digi-Key:296-13996-5-ND", "Mouser:595-LM7805CT"], decision.Component.Offers.Select(offer => offer.LinkKey));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void RegulatorFamilyAliasesMerge7805Variants()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("SparkFun", "COM-00107", "STMicroelectronics", "L7805CV", "5 V", "TO-220"),
            Fact("Jameco", "51262", "Texas Instruments", "LM7805CT", "5 V", "TO-220"),
            Fact("Adafruit", "2164", "Generic", "7805", "5 V", "TO-220")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        Assert.Equal("ALIAS:7805", decision.MatchReason);
        Assert.Equal("5 V", decision.Component.DefaultValue);
        Assert.Equal("TO-220", decision.Component.DefaultPackage);
        Assert.Equal(3, decision.Component.Offers.Count);
    }

    [Fact]
    public void Ne555FamilyAliasesMergeCommonTimerVariants()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("Digi-Key", "296-1411-5-ND", "Texas Instruments", "NE555P", "Timer", "PDIP-8"),
            Fact("Mouser", "595-NE555P", "Texas Instruments", "NE555PWR", "Timer", "SOIC-8"),
            Fact("Jameco", "27422", "Signetics", "SE555", "Timer", "PDIP-8")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        Assert.Equal("ALIAS:555", decision.MatchReason);
        Assert.Equal(3, decision.Component.Offers.Count);
    }

    [Fact]
    public void Esp32DevkitAliasesMergeAcrossVendorBoardNames()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("Digi-Key", "1965-ESP32-DEVKITC-32E-ND", "Espressif", "ESP32-DEVKITC-32E", "ESP32-WROOM-32E", "DevKitC"),
            Fact("Mouser", "356-ESP32-DEVKITC32E", "Espressif Systems", "ESP32 DevKitC V4", "ESP32-WROOM-32E", "DevKitC")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        Assert.Equal("ALIAS:ESP32-DEVKIT", decision.MatchReason);
        Assert.Equal("ESP32-WROOM-32E", decision.Component.DefaultValue);
        Assert.Equal(2, decision.Component.Offers.Count);
    }

    [Fact]
    public void PassiveValueAndPackageEquivalenceMergesVendorListings()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("Digi-Key", "311-10.0KHRCT-ND", "Yageo", "RC0603FR-0710KL", "10 kOhm", "0603", "1%", "Resistor"),
            Fact("Mouser", "603-RC0603FR-0710KL", "Yageo", "RC0603FR-0710KL", "10KOHM", "0603", "1 %", "resistor")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        Assert.Equal("PASSIVE", decision.MatchReason);
        Assert.Equal("10 kOhm", decision.Component.DefaultValue);
        Assert.Equal("0603", decision.Component.DefaultPackage);
        Assert.Equal(2, decision.Component.Offers.Count);
    }

    [Fact]
    public void ConflictingSpecsProduceDiagnosticsButKeepAlternatives()
    {
        MarketplaceCanonicalMergeService service = new();

        MarketplaceCanonicalMergeResult result = service.Merge(
        [
            Fact("Digi-Key", "296-13996-5-ND", "Texas Instruments", "LM7805CT", "5 V", "TO-220"),
            Fact("BadFeed", "LM7805-12V", "Texas Instruments", "LM7805CT", "12 V", "TO-220")
        ]);

        MarketplaceCanonicalMergeDecision decision = Assert.Single(result.Decisions);
        MarketplaceCanonicalMergeDiagnostic diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(MarketplaceCanonicalMergeDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(MarketplaceCanonicalMergeDiagnosticCodes.ConflictingValues, diagnostic.Code);
        Assert.Equal(decision.Component.Key, diagnostic.ComponentKey);
        Assert.Equal(2, decision.Component.Offers.Count);
    }

    [Fact]
    public void CanonicalWinnerIsDeterministicRegardlessOfInputOrder()
    {
        MarketplaceCanonicalMergeService service = new();
        MarketplaceComponentFact[] facts =
        [
            Fact("Mouser", "595-LM7805CT", "Texas Instruments", "LM7805CT", "5 V", "TO-220"),
            Fact("Digi-Key", "296-13996-5-ND", "Texas Instruments", "LM7805CT", "5 V", "TO-220"),
            Fact("SparkFun", "COM-00107", "STMicroelectronics", "L7805CV", "5 V", "TO-220")
        ];

        MarketplaceCanonicalMergeResult first = service.Merge(facts);
        MarketplaceCanonicalMergeResult second = service.Merge(facts.Reverse().ToArray());

        Assert.Equal(first.Decisions.Select(decision => decision.Component.Key), second.Decisions.Select(decision => decision.Component.Key));
        Assert.Equal(first.Decisions[0].Component.DisplayName, second.Decisions[0].Component.DisplayName);
        Assert.Equal(first.Decisions[0].Component.ManufacturerPartNumber, second.Decisions[0].Component.ManufacturerPartNumber);
        Assert.Equal(first.Decisions[0].Component.Offers.Select(offer => offer.LinkKey), second.Decisions[0].Component.Offers.Select(offer => offer.LinkKey));
    }

    private static MarketplaceComponentFact Fact(
        string vendor,
        string vendorSku,
        string manufacturer,
        string manufacturerPartNumber,
        string value,
        string package,
        string tolerance = "",
        string kind = "Integrated Circuit") =>
        new(
            VendorName: vendor,
            VendorSku: vendorSku,
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            DisplayName: manufacturerPartNumber,
            ProductUrl: $"https://example.invalid/{vendorSku}",
            Kind: kind,
            Value: value,
            Package: package,
            Tolerance: tolerance);
}
