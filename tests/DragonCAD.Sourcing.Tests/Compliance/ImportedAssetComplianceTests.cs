using DragonCAD.Sourcing.Compliance;

namespace DragonCAD.Sourcing.Tests.Compliance;

public sealed class ImportedAssetComplianceTests
{
    [Fact]
    public void OpenHardwareImportPreservesRepositoryLicenseAndAttribution()
    {
        var provenance = new OpenHardwareAssetProvenance(
            sourceRepository: new Uri("https://github.com/sparkfun/SparkFun-KiCad-Libraries"),
            sourcePath: "Symbols/SparkFun-Connectors.kicad_sym",
            licenseName: "CC BY-SA 4.0",
            licenseText: "Creative Commons Attribution ShareAlike license text.",
            licenseUrl: new Uri("https://creativecommons.org/licenses/by-sa/4.0/"),
            attributionNotes: "SparkFun Electronics KiCad library");
        var asset = new ImportedOpenHardwareAsset(
            assetId: "sparkfun:conn-usb-c",
            providerId: "sparkfun",
            displayName: "USB-C connector symbol",
            provenance: provenance);

        Assert.Equal("https://github.com/sparkfun/SparkFun-KiCad-Libraries", asset.Provenance.SourceRepository.ToString());
        Assert.Equal("CC BY-SA 4.0", asset.Provenance.LicenseName);
        Assert.Equal("Creative Commons Attribution ShareAlike license text.", asset.Provenance.LicenseText);
        Assert.Equal("SparkFun Electronics KiCad library", asset.Provenance.AttributionNotes);
    }

    [Fact]
    public void DatasheetDerivedComponentPreservesDatasheetSourceAndReviewWarnings()
    {
        var component = new DatasheetDerivedComponentCompliance(
            componentId: "vendor:lm7805",
            providerId: "vendor",
            datasheetSource: new DatasheetSource(
                url: new Uri("https://example.test/lm7805.pdf"),
                documentTitle: "LM7805 Voltage Regulator Datasheet",
                retrievedAtUtc: new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero)),
            reviewWarnings:
            [
                new DatasheetReviewWarning(
                    code: "pinout-review",
                    message: "Pinout was derived from a datasheet table and needs human review."),
                new DatasheetReviewWarning(
                    code: "parameter-review",
                    message: "Electrical limits were extracted from OCR text and need human review."),
            ]);

        Assert.Equal("https://example.test/lm7805.pdf", component.DatasheetSource.Url.ToString());
        Assert.Equal(
            ["pinout-review", "parameter-review"],
            component.ReviewWarnings.Select(warning => warning.Code));
        Assert.All(component.ReviewWarnings, warning => Assert.False(string.IsNullOrWhiteSpace(warning.Message)));
    }
}
