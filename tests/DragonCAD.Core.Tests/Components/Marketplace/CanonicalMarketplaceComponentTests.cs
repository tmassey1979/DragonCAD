using DragonCAD.Core.Components.Marketplace;

namespace DragonCAD.Core.Tests.Components.Marketplace;

public sealed class CanonicalMarketplaceComponentTests
{
    [Fact]
    public void CanonicalKeysNormalizeCommonPartTextForVendorDeduplication()
    {
        CanonicalComponentKey first = CanonicalComponentKey.FromPartNumber(" NE-555 P ");
        CanonicalComponentKey second = CanonicalComponentKey.FromPartNumber("ne555p");

        Assert.Equal("PART:NE555P", first.Value);
        Assert.Equal(first, second);
    }

    [Fact]
    public void PassiveKeysNormalizeValuePackageAndTolerance()
    {
        CanonicalComponentKey first = CanonicalComponentKey.FromPassive(" resistor ", "10 kΩ", " 0603 ", "1 %");
        CanonicalComponentKey second = CanonicalComponentKey.FromPassive("RESISTOR", "10KOHM", "0603", "1%");

        Assert.Equal("PASSIVE:RESISTOR:10KOHM:0603:1%", first.Value);
        Assert.Equal(first, second);
    }

    [Fact]
    public void OffersAttachAsVendorLinksAndKeepOrderingOverrides()
    {
        CanonicalMarketplaceComponent component = CanonicalMarketplaceComponent.Create(
                CanonicalComponentKey.FromPartNumber("LM7805"),
                "7805 regulator",
                DefaultValue: "5 V",
                DefaultPackage: "TO-220")
            .AttachOffer(Offer("Mouser", "595-LM7805CT", "Texas Instruments", "LM7805CT", "5 V", "TO-220-3"))
            .AttachOffer(Offer("Digi-Key", "296-13996-5-ND", "Texas Instruments", "LM7805CT", "", ""));

        Assert.Equal(["Digi-Key:296-13996-5-ND", "Mouser:595-LM7805CT"], component.Offers.Select(offer => offer.LinkKey));
        Assert.Equal("5 V", component.GetOrderingValue("Digi-Key", "296-13996-5-ND"));
        Assert.Equal("TO-220", component.GetOrderingPackage("Digi-Key", "296-13996-5-ND"));
        Assert.Equal("TO-220-3", component.GetOrderingPackage("Mouser", "595-LM7805CT"));
    }

    [Fact]
    public void PreferredDefaultValueCanBeOverriddenWithoutLosingVendorSpecificValues()
    {
        CanonicalMarketplaceComponent component = CanonicalMarketplaceComponent.Create(
                CanonicalComponentKey.FromPartNumber("ESP32-DEVKITC"),
                "ESP32 DevKit",
                DefaultValue: "ESP32-WROOM",
                DefaultPackage: "DevKit")
            .WithDefaults(DefaultValue: "ESP32-WROOM-32E", DefaultPackage: "DevKitC")
            .AttachOffer(Offer("SparkFun", "DEV-13907", "Espressif", "ESP32-DEVKITC", "ESP32-WROOM-32D", ""));

        Assert.Equal("ESP32-WROOM-32E", component.DefaultValue);
        Assert.Equal("DevKitC", component.DefaultPackage);
        Assert.Equal("ESP32-WROOM-32D", component.GetOrderingValue("SparkFun", "DEV-13907"));
        Assert.Equal("DevKitC", component.GetOrderingPackage("SparkFun", "DEV-13907"));
    }

    [Fact]
    public void DisplayMetadataIsDeterministicRegardlessOfOfferInsertionOrder()
    {
        MarketplaceVendorOffer adafruit = Offer("Adafruit", "123", "Espressif", "ESP32-DEVKITC", "", "");
        MarketplaceVendorOffer digikey = Offer("Digi-Key", "1965-ESP32-DEVKITC-32E-ND", "Espressif Systems", "ESP32-DEVKITC-32E", "", "");

        CanonicalMarketplaceComponent first = CanonicalMarketplaceComponent.FromOffers(
            CanonicalComponentKey.FromPartNumber("ESP32 DevKitC 32E"),
            [adafruit, digikey],
            DefaultValue: "ESP32-WROOM-32E",
            DefaultPackage: "DevKitC");

        CanonicalMarketplaceComponent second = CanonicalMarketplaceComponent.FromOffers(
            CanonicalComponentKey.FromPartNumber("ESP32 DevKitC 32E"),
            [digikey, adafruit],
            DefaultValue: "ESP32-WROOM-32E",
            DefaultPackage: "DevKitC");

        Assert.Equal(first.DisplayName, second.DisplayName);
        Assert.Equal(first.Manufacturer, second.Manufacturer);
        Assert.Equal(first.ManufacturerPartNumber, second.ManufacturerPartNumber);
        Assert.Equal("ESP32-DEVKITC-32E", first.ManufacturerPartNumber);
    }

    private static MarketplaceVendorOffer Offer(
        string vendor,
        string vendorSku,
        string manufacturer,
        string manufacturerPartNumber,
        string valueOverride,
        string packageOverride) =>
        new(
            VendorName: vendor,
            VendorSku: vendorSku,
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            DisplayName: manufacturerPartNumber,
            ProductUrl: $"https://example.invalid/{vendorSku}",
            ValueOverride: valueOverride,
            PackageOverride: packageOverride);
}
