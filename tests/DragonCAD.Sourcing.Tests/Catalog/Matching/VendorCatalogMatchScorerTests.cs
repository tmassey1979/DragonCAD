using DragonCAD.Sourcing.Catalog.Matching;

namespace DragonCAD.Sourcing.Tests.Catalog.Matching;

public sealed class VendorCatalogMatchScorerTests
{
    [Fact]
    public void ScoresExactWhenAllIdentitySignalsMatch()
    {
        var query = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: " LM7805CT/NOPB ",
            Manufacturer: "Texas Instruments",
            Package: "TO-220",
            DatasheetUrl: new Uri("https://www.ti.com/lit/ds/symlink/lm7805.pdf"));
        var listing = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "lm7805ct/nopb",
            Manufacturer: "TI",
            Package: "TO220",
            DatasheetUrl: new Uri("https://www.ti.com/lit/ds/symlink/lm7805.pdf?ts=123"));

        var result = VendorCatalogMatchScorer.Score(query, listing);

        Assert.Equal(VendorCatalogMatchQuality.Exact, result.Quality);
        Assert.Equal(100, result.Score);
        Assert.True(result.ManufacturerPartNumberMatches);
        Assert.True(result.ManufacturerMatches);
        Assert.True(result.PackageMatches);
        Assert.True(result.DatasheetUrlMatches);
    }

    [Fact]
    public void ScoresDuplicateWhenCoreIdentityMatchesButSecondarySignalsDiffer()
    {
        var query = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "STM32F103C8T6",
            Manufacturer: "STMicroelectronics",
            Package: "LQFP-48",
            DatasheetUrl: new Uri("https://example.test/stm32f103.pdf"));
        var listing = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "stm32f103c8t6",
            Manufacturer: "ST Micro",
            Package: "Tray",
            DatasheetUrl: new Uri("https://cdn.example.test/stm32f103-rev9.pdf"));

        var result = VendorCatalogMatchScorer.Score(query, listing);

        Assert.Equal(VendorCatalogMatchQuality.Duplicate, result.Quality);
        Assert.InRange(result.Score, 70, 99);
        Assert.True(result.ManufacturerPartNumberMatches);
        Assert.True(result.ManufacturerMatches);
        Assert.False(result.PackageMatches);
        Assert.False(result.DatasheetUrlMatches);
    }

    [Fact]
    public void ScoresWeakWhenOnlyManufacturerPartNumberMatches()
    {
        var query = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "NE555P",
            Manufacturer: "Texas Instruments",
            Package: "PDIP-8",
            DatasheetUrl: new Uri("https://www.ti.com/lit/ds/symlink/ne555.pdf"));
        var listing = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "ne555p",
            Manufacturer: "Major Brands",
            Package: "SOIC-8",
            DatasheetUrl: null);

        var result = VendorCatalogMatchScorer.Score(query, listing);

        Assert.Equal(VendorCatalogMatchQuality.Weak, result.Quality);
        Assert.InRange(result.Score, 40, 69);
        Assert.True(result.ManufacturerPartNumberMatches);
        Assert.False(result.ManufacturerMatches);
        Assert.False(result.PackageMatches);
        Assert.False(result.DatasheetUrlMatches);
    }

    [Fact]
    public void ScoresNoMatchWhenManufacturerPartNumberDiffers()
    {
        var query = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "ATMEGA328P-PU",
            Manufacturer: "Microchip",
            Package: "DIP-28",
            DatasheetUrl: null);
        var listing = new VendorCatalogMatchCandidate(
            ManufacturerPartNumber: "ATMEGA328PB-AU",
            Manufacturer: "Microchip",
            Package: "TQFP-32",
            DatasheetUrl: null);

        var result = VendorCatalogMatchScorer.Score(query, listing);

        Assert.Equal(VendorCatalogMatchQuality.NoMatch, result.Quality);
        Assert.Equal(0, result.Score);
        Assert.False(result.ManufacturerPartNumberMatches);
    }
}
