using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Deduplication;

namespace DragonCAD.Sourcing.Tests.Deduplication;

public sealed class ComponentDeduplicatorTests
{
    [Fact]
    public void GroupsVendorListingsForSameRealPartByNormalizedIdentitySignals()
    {
        var listings = new[]
        {
            Listing(
                providerName: "Digi-Key",
                vendorSku: "296-1389-5-ND",
                manufacturerPartNumber: "LM7805CT/NOPB",
                manufacturer: "Texas Instruments",
                description: "Linear voltage regulator 5V TO-220",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "7805, LM7805",
                    ["Package"] = "TO-220",
                    ["Value"] = "5 V",
                }),
            Listing(
                providerName: "Jameco",
                vendorSku: "51262",
                manufacturerPartNumber: "7805",
                manufacturer: "TI",
                description: "7805 positive voltage regulator to220 5 volt",
                fields: new Dictionary<string, string>
                {
                    ["Alias"] = "LM7805CT",
                    ["Package"] = "TO220",
                    ["Value"] = "5V",
                }),
        };

        var candidates = ComponentDeduplicator.GroupCandidates(listings);

        var candidate = Assert.Single(candidates);
        Assert.Equal("LM7805CT/NOPB", candidate.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", candidate.Manufacturer);
        Assert.Equal("TO-220", candidate.Package);
        Assert.Equal("5 V", candidate.Value);
        Assert.Equal(["7805", "LM7805", "LM7805CT", "LM7805CT/NOPB"], candidate.Aliases);
        Assert.Equal(["Digi-Key:296-1389-5-ND", "Jameco:51262"], candidate.SourceKeys);
        Assert.Empty(candidate.Warnings);
    }

    [Fact]
    public void EmitsMergeWarningsWhenGroupedListingsDisagreeOnPackageOrValue()
    {
        var listings = new[]
        {
            Listing(
                providerName: "Mouser",
                vendorSku: "595-NE555P",
                manufacturerPartNumber: "NE555P",
                manufacturer: "Texas Instruments",
                description: "555 timer PDIP-8",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "NE555",
                    ["Package"] = "PDIP-8",
                    ["Value"] = "Timer",
                }),
            Listing(
                providerName: "VendorX",
                vendorSku: "NE555P-SOIC",
                manufacturerPartNumber: "NE555P",
                manufacturer: "TI",
                description: "555 timer SOIC-8",
                fields: new Dictionary<string, string>
                {
                    ["Package"] = "SOIC-8",
                    ["Value"] = "Oscillator",
                }),
        };

        var candidate = Assert.Single(ComponentDeduplicator.GroupCandidates(listings));

        Assert.Collection(
            candidate.Warnings.OrderBy(warning => warning.Kind),
            warning =>
            {
                Assert.Equal(ComponentMergeWarningKind.PackageDisagreement, warning.Kind);
                Assert.Equal(["PDIP-8", "SOIC-8"], warning.Values);
                Assert.Equal(["Mouser:595-NE555P", "VendorX:NE555P-SOIC"], warning.SourceKeys);
            },
            warning =>
            {
                Assert.Equal(ComponentMergeWarningKind.ValueDisagreement, warning.Kind);
                Assert.Equal(["Oscillator", "Timer"], warning.Values);
                Assert.Equal(["Mouser:595-NE555P", "VendorX:NE555P-SOIC"], warning.SourceKeys);
            });
    }

    [Fact]
    public void GroupsDevelopmentBoardAliasesAndWarnsWhenManufacturerNamesDisagree()
    {
        var listings = new[]
        {
            Listing(
                providerName: "SparkFun",
                vendorSku: "DEV-13975",
                manufacturerPartNumber: "ESP32-THING",
                manufacturer: "SparkFun",
                description: "ESP32 development board",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "ESP32 DevKit, ESP32 dev board",
                    ["Package"] = "Dev Kit",
                    ["Value"] = "ESP32",
                }),
            Listing(
                providerName: "Marketplace",
                vendorSku: "ESP32-DEVKIT-V1",
                manufacturerPartNumber: "ESP32 DevKit V1",
                manufacturer: "Generic",
                description: "ESP32 devkit module",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "ESP32 DevKit",
                    ["Package"] = "DevKit",
                    ["Value"] = "ESP32",
                }),
        };

        var candidate = Assert.Single(ComponentDeduplicator.GroupCandidates(listings));

        Assert.Equal("ESP32 DevKit V1", candidate.ManufacturerPartNumber);
        var warning = Assert.Single(candidate.Warnings);
        Assert.Equal(ComponentMergeWarningKind.ManufacturerDisagreement, warning.Kind);
        Assert.Equal(["Generic", "SparkFun"], warning.Values);
    }

    [Fact]
    public void DoesNotMergeAliasOnlyListingsWhenManufacturerAndSecondarySignalsDoNotAgree()
    {
        var listings = new[]
        {
            Listing(
                providerName: "VendorA",
                vendorSku: "555-CMOS",
                manufacturerPartNumber: "555",
                manufacturer: "Manufacturer A",
                description: "555 timer",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "Timer 555",
                }),
            Listing(
                providerName: "VendorB",
                vendorSku: "555-BIPOLAR",
                manufacturerPartNumber: "555",
                manufacturer: "Manufacturer B",
                description: "555 timer",
                fields: new Dictionary<string, string>
                {
                    ["Aliases"] = "Timer 555",
                }),
        };

        var candidates = ComponentDeduplicator.GroupCandidates(listings);

        Assert.Equal(2, candidates.Count);
        Assert.All(candidates, candidate => Assert.Empty(candidate.Warnings));
    }

    private static NormalizedCatalogListing Listing(
        string providerName,
        string vendorSku,
        string manufacturerPartNumber,
        string manufacturer,
        string description,
        IReadOnlyDictionary<string, string> fields)
    {
        return new NormalizedCatalogListing(
            providerName,
            vendorSku,
            manufacturerPartNumber,
            manufacturer,
            description,
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(1m))]),
            stockQuantity: 10,
            datasheetUrl: null,
            productUrl: null,
            fields,
            CatalogProviderCapabilities.Feed);
    }
}
