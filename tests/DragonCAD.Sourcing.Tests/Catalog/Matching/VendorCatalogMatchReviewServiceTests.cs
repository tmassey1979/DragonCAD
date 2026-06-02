using DragonCAD.Sourcing.Catalog.Matching;

namespace DragonCAD.Sourcing.Tests.Catalog.Matching;

public sealed class VendorCatalogMatchReviewServiceTests
{
    [Fact]
    public void ClassifiesExactManufacturerPartNumberMatchAsAcceptedComponentLinkCandidate()
    {
        var component = Component(
            componentKey: "core:lm7805",
            manufacturerPartNumber: "LM7805CT/NOPB",
            manufacturer: "Texas Instruments",
            package: "TO-220",
            value: "5V regulator");
        var listing = Listing(
            vendorSku: "DIGI-LM7805",
            manufacturerPartNumber: "lm7805ct/nopb",
            manufacturer: "TI",
            package: "TO220",
            value: "5 V regulator");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.ExactComponentMatch, item.Classification);
        Assert.Equal("core:lm7805", item.ComponentKey);
        Assert.True(item.IsAvailableForDirectPlacement);
        Assert.Empty(item.Conflicts);
        Assert.Equal(100, item.Confidence);
    }

    [Fact]
    public void ClassifiesPackageMismatchOnSameManufacturerPartNumberAsConflict()
    {
        var component = Component(
            componentKey: "core:stm32-lqfp",
            manufacturerPartNumber: "STM32F103C8T6",
            manufacturer: "STMicroelectronics",
            package: "LQFP-48",
            value: "ARM Cortex-M3");
        var listing = Listing(
            vendorSku: "MOU-ST32",
            manufacturerPartNumber: "STM32F103C8T6",
            manufacturer: "ST Micro",
            package: "UFQFPN-48",
            value: "ARM Cortex-M3");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.Conflict, item.Classification);
        Assert.Equal(VendorCatalogMatchConflictKind.PackageMismatch, Assert.Single(item.Conflicts).Kind);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void ClassifiesObsoleteLifecycleAsConflict()
    {
        var component = Component(
            componentKey: "core:ne555",
            manufacturerPartNumber: "NE555P",
            manufacturer: "Texas Instruments",
            package: "PDIP-8",
            value: "Timer");
        var listing = Listing(
            vendorSku: "ADA-555",
            manufacturerPartNumber: "NE555P",
            manufacturer: "TI",
            package: "PDIP-8",
            value: "Timer",
            lifecycleStatus: "Obsolete");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.Conflict, item.Classification);
        Assert.Contains(item.Conflicts, conflict => conflict.Kind == VendorCatalogMatchConflictKind.ObsoleteLifecycle);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void ClassifiesSameValueAndPackageWithDifferentManufacturerPartNumberAsLikelyAlternate()
    {
        var component = Component(
            componentKey: "core:led-red",
            manufacturerPartNumber: "LTST-C170KRKT",
            manufacturer: "Lite-On",
            package: "0603",
            value: "Red LED");
        var listing = Listing(
            vendorSku: "ALT-LED",
            manufacturerPartNumber: "APT1608SURCK",
            manufacturer: "Kingbright",
            package: "0603",
            value: "Red LED");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.LikelyAlternate, item.Classification);
        Assert.Equal("core:led-red", item.ComponentKey);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void ClassifiesSameValueWithDifferentPackageAsPackageVariant()
    {
        var component = Component(
            componentKey: "core:res-10k-0603",
            manufacturerPartNumber: "RC0603FR-0710KL",
            manufacturer: "Yageo",
            package: "0603",
            value: "10k");
        var listing = Listing(
            vendorSku: "RES-10K-0805",
            manufacturerPartNumber: "RC0805FR-0710KL",
            manufacturer: "Yageo",
            package: "0805",
            value: "10 kOhm");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.PackageVariant, item.Classification);
        Assert.Contains(item.Conflicts, conflict => conflict.Kind == VendorCatalogMatchConflictKind.PackageMismatch);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void ClassifiesSamePackageWithDifferentValueAsValueVariant()
    {
        var component = Component(
            componentKey: "core:cap-100nf",
            manufacturerPartNumber: "CL10B104KB8NNNC",
            manufacturer: "Samsung",
            package: "0603",
            value: "100 nF");
        var listing = Listing(
            vendorSku: "CAP-1UF",
            manufacturerPartNumber: "CL10B105KB8NNNC",
            manufacturer: "Samsung",
            package: "0603",
            value: "1 uF");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.ValueVariant, item.Classification);
        Assert.Contains(item.Conflicts, conflict => conflict.Kind == VendorCatalogMatchConflictKind.ValueMismatch);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void OrdersDuplicateOffersDeterministicallyAndMarksLowerRankedDuplicates()
    {
        var component = Component(
            componentKey: "core:atmega",
            manufacturerPartNumber: "ATMEGA328P-PU",
            manufacturer: "Microchip",
            package: "DIP-28",
            value: "8-bit MCU");
        var firstListing = Listing(
            providerName: "Zeta",
            vendorSku: "Z-2",
            manufacturerPartNumber: "ATMEGA328P-PU",
            manufacturer: "Microchip",
            package: "DIP-28",
            value: "8-bit MCU");
        var secondListing = Listing(
            providerName: "Alpha",
            vendorSku: "A-1",
            manufacturerPartNumber: "ATMEGA328P-PU",
            manufacturer: "Microchip",
            package: "DIP-28",
            value: "8-bit MCU");

        var result = VendorCatalogMatchReviewService.BuildReview([component], [firstListing, secondListing]);

        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal("Alpha", item.ProviderName);
                Assert.DoesNotContain(item.Conflicts, conflict => conflict.Kind == VendorCatalogMatchConflictKind.DuplicateOffer);
            },
            item =>
            {
                Assert.Equal("Zeta", item.ProviderName);
                Assert.Contains(item.Conflicts, conflict => conflict.Kind == VendorCatalogMatchConflictKind.DuplicateOffer);
            });
    }

    [Fact]
    public void RecordsRejectedDecisionAndPreventsPlacement()
    {
        var reviewedAt = new DateTimeOffset(2026, 6, 2, 12, 30, 0, TimeSpan.Zero);
        var component = Component(
            componentKey: "core:usb-c",
            manufacturerPartNumber: "USB-C-16P",
            manufacturer: "Generic",
            package: "SMD-16",
            value: "USB-C receptacle");
        var listing = Listing(
            vendorSku: "JAM-USB",
            manufacturerPartNumber: "USB-C-16P",
            manufacturer: "Generic",
            package: "SMD-16",
            value: "USB-C receptacle");
        var decision = new VendorCatalogMatchReviewDecision(
            VendorCatalogMatchReviewOutcome.Rejected,
            Reviewer: "tmass",
            Notes: "Vendor row is keyed to the wrong footprint.",
            Timestamp: reviewedAt);

        var result = VendorCatalogMatchReviewService.BuildReview([component], [listing], [decision.For("Jameco", "JAM-USB")]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchReviewOutcome.Rejected, item.Decision?.Outcome);
        Assert.Equal("tmass", item.Decision?.Reviewer);
        Assert.Equal("Vendor row is keyed to the wrong footprint.", item.Decision?.Notes);
        Assert.Equal(reviewedAt, item.Decision?.Timestamp);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    [Fact]
    public void KeepsCatalogOnlyRowsUnavailableForDirectPlacement()
    {
        var listing = Listing(
            vendorSku: "SPK-SENSOR",
            manufacturerPartNumber: "BME280-BREAKOUT",
            manufacturer: "SparkFun",
            package: "Breakout",
            value: "Environmental sensor");

        var result = VendorCatalogMatchReviewService.BuildReview([], [listing]);

        var item = Assert.Single(result.Items);
        Assert.Equal(VendorCatalogMatchClassification.CatalogOnly, item.Classification);
        Assert.Null(item.ComponentKey);
        Assert.False(item.IsAvailableForDirectPlacement);
    }

    private static DragonCadComponentMatchProfile Component(
        string componentKey,
        string manufacturerPartNumber,
        string manufacturer,
        string package,
        string value)
    {
        return new DragonCadComponentMatchProfile(componentKey, manufacturerPartNumber, manufacturer, package, value);
    }

    private static VendorCatalogMatchReviewRow Listing(
        string vendorSku,
        string manufacturerPartNumber,
        string manufacturer,
        string package,
        string value,
        string providerName = "Jameco",
        string lifecycleStatus = "Active")
    {
        return new VendorCatalogMatchReviewRow(
            providerName,
            vendorSku,
            manufacturerPartNumber,
            manufacturer,
            package,
            value,
            lifecycleStatus);
    }
}
