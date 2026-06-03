using DragonCAD.ComponentIntelligence.Datasheets;

namespace DragonCAD.ComponentIntelligence.Tests.Datasheets;

public sealed class DatasheetDraftGeneratorTests
{
    [Fact]
    public void RequestValidationRequiresDatasheetSourceAndComponentIdentifier()
    {
        var missingSource = DatasheetDraftGenerationRequest.Create(
            pdfPath: null,
            sourceUrl: null,
            vendorProductId: "296-1415-5-ND",
            manufacturerPartNumber: "LM7805CT",
            targetPackage: "TO-220-3");
        var missingIdentifier = DatasheetDraftGenerationRequest.Create(
            pdfPath: @"C:\datasheets\lm7805.pdf",
            sourceUrl: null,
            vendorProductId: null,
            manufacturerPartNumber: " ",
            targetPackage: "TO-220-3");
        var valid = DatasheetDraftGenerationRequest.Create(
            pdfPath: @"C:\datasheets\lm7805.pdf",
            sourceUrl: new Uri("https://vendor.example.test/lm7805.pdf"),
            vendorProductId: "296-1415-5-ND",
            manufacturerPartNumber: " LM7805CT ",
            targetPackage: " TO-220-3 ");

        AssertDiagnostic(missingSource, DatasheetDraftGenerationDiagnosticCode.MissingDatasheetSource);
        AssertDiagnostic(missingIdentifier, DatasheetDraftGenerationDiagnosticCode.MissingComponentIdentifier);
        Assert.True(valid.Accepted);
        Assert.Equal(@"C:\datasheets\lm7805.pdf", valid.Request!.PdfPath);
        Assert.Equal("https://vendor.example.test/lm7805.pdf", valid.Request.SourceUrl!.ToString());
        Assert.Equal("LM7805CT", valid.Request.ManufacturerPartNumber);
        Assert.Equal("TO-220-3", valid.Request.TargetPackage);
    }

    [Fact]
    public void GenerateDraftMapsExtractedPinsPackageMetadataConfidenceAndWarnings()
    {
        var generator = new DatasheetDraftGenerator(
            new FakeDatasheetDraftExtractor(
                new DatasheetDraftExtraction(
                    Pins:
                    [
                        new DatasheetDraftPinExtraction("1", "IN", DatasheetDraftPinElectricalType.PowerInput, 0.93m),
                        new DatasheetDraftPinExtraction("2", "GND", DatasheetDraftPinElectricalType.PowerInput, 0.91m),
                        new DatasheetDraftPinExtraction("3", "OUT", DatasheetDraftPinElectricalType.PowerOutput, 0.94m),
                    ],
                    PackageHints: ["TO-220", "TO-220-3"],
                    Metadata: new DatasheetDraftMetadata(
                        Manufacturer: "Texas Instruments",
                        ManufacturerPartNumber: "LM7805CT",
                        VendorProductId: "296-1415-5-ND",
                        Description: "5 V linear regulator"),
                    ConfidenceScore: 0.89m,
                    Warnings: [new DatasheetDraftWarning(DatasheetDraftWarningCode.AmbiguousPinType, "GND can be passive or power input.")])),
            new FakePackageCatalog(
                new DatasheetFootprintCandidate("Package_TO_SOT_THT:TO-220-3_Vertical", "TO-220-3", 0.87m)));

        DatasheetDraftGenerationResult result = generator.Generate(CreateRequest());

        Assert.True(result.Accepted);
        DatasheetDraftComponent draft = result.Draft!;
        Assert.False(draft.IsVerified);
        Assert.Equal(DatasheetDraftVerificationStatus.Unverified, draft.VerificationStatus);
        Assert.Equal("TO-220-3", draft.TargetPackage);
        Assert.Equal(["TO-220", "TO-220-3"], draft.PackageHints);
        Assert.Equal("Texas Instruments", draft.Metadata.Manufacturer);
        Assert.Equal("LM7805CT", draft.Metadata.ManufacturerPartNumber);
        Assert.Equal(0.89m, draft.ConfidenceScore);
        Assert.Collection(
            draft.Pins,
            pin =>
            {
                Assert.Equal("1", pin.Number);
                Assert.Equal("IN", pin.Name);
                Assert.Equal(DatasheetDraftPinElectricalType.PowerInput, pin.ElectricalType);
                Assert.Equal(0.93m, pin.ConfidenceScore);
            },
            pin => Assert.Equal("GND", pin.Name),
            pin => Assert.Equal(DatasheetDraftPinElectricalType.PowerOutput, pin.ElectricalType));
        DatasheetFootprintCandidate footprint = Assert.Single(draft.FootprintCandidates);
        Assert.Equal("Package_TO_SOT_THT:TO-220-3_Vertical", footprint.LibraryId);
        Assert.Equal(0.87m, footprint.ConfidenceScore);
        DatasheetDraftWarning warning = Assert.Single(draft.Warnings);
        Assert.Equal(DatasheetDraftWarningCode.AmbiguousPinType, warning.Code);
    }

    [Fact]
    public void GenerateDraftRejectsUnsupportedPackageWithoutCallingExtractor()
    {
        var extractor = new RecordingDatasheetDraftExtractor();
        var generator = new DatasheetDraftGenerator(extractor, new FakePackageCatalog());

        DatasheetDraftGenerationResult result = generator.Generate(CreateRequest(targetPackage: "QFN-99"));

        Assert.False(result.Accepted);
        Assert.Null(result.Draft);
        Assert.False(extractor.WasCalled);
        AssertDiagnostic(result, DatasheetDraftGenerationDiagnosticCode.UnsupportedTargetPackage);
    }

    [Fact]
    public void GenerateDraftPropagatesExtractorWarningsAndLowConfidencePinDiagnostics()
    {
        var generator = new DatasheetDraftGenerator(
            new FakeDatasheetDraftExtractor(
                new DatasheetDraftExtraction(
                    Pins: [new DatasheetDraftPinExtraction("1", "NC", DatasheetDraftPinElectricalType.NoConnect, 0.42m)],
                    PackageHints: ["DIP-8"],
                    Metadata: new DatasheetDraftMetadata("Texas Instruments", "NE555P", null, "Timer"),
                    ConfidenceScore: 0.58m,
                    Warnings: [new DatasheetDraftWarning(DatasheetDraftWarningCode.PackageDrawingAmbiguous, "Package drawing spans two tables.")])),
            new FakePackageCatalog(new DatasheetFootprintCandidate("Package_DIP:DIP-8_W7.62mm", "DIP-8", 0.81m)));

        DatasheetDraftGenerationResult result = generator.Generate(CreateRequest(manufacturerPartNumber: "NE555P", targetPackage: "DIP-8"));

        DatasheetDraftComponent draft = result.Draft!;
        Assert.Equal(2, draft.Warnings.Count);
        Assert.Contains(draft.Warnings, warning => warning.Code == DatasheetDraftWarningCode.PackageDrawingAmbiguous);
        Assert.Contains(draft.Warnings, warning => warning.Code == DatasheetDraftWarningCode.LowPinConfidence);
    }

    private static DatasheetDraftGenerationRequest CreateRequest(
        string manufacturerPartNumber = "LM7805CT",
        string targetPackage = "TO-220-3") =>
        DatasheetDraftGenerationRequest.Create(
            pdfPath: @"C:\datasheets\component.pdf",
            sourceUrl: new Uri("https://vendor.example.test/component.pdf"),
            vendorProductId: "296-1415-5-ND",
            manufacturerPartNumber: manufacturerPartNumber,
            targetPackage: targetPackage).Request!;

    private static void AssertDiagnostic(
        DatasheetDraftGenerationRequestValidationResult result,
        DatasheetDraftGenerationDiagnosticCode expectedCode)
    {
        Assert.False(result.Accepted);
        DatasheetDraftGenerationDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Null(result.Request);
    }

    private static void AssertDiagnostic(
        DatasheetDraftGenerationResult result,
        DatasheetDraftGenerationDiagnosticCode expectedCode)
    {
        DatasheetDraftGenerationDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
    }

    private sealed class FakeDatasheetDraftExtractor(DatasheetDraftExtraction extraction) : IDatasheetDraftExtractor
    {
        public DatasheetDraftExtraction Extract(DatasheetDraftGenerationRequest request) => extraction;
    }

    private sealed class RecordingDatasheetDraftExtractor : IDatasheetDraftExtractor
    {
        public bool WasCalled { get; private set; }

        public DatasheetDraftExtraction Extract(DatasheetDraftGenerationRequest request)
        {
            WasCalled = true;
            return new DatasheetDraftExtraction([], [], new DatasheetDraftMetadata(null, null, null, null), 0m, []);
        }
    }

    private sealed class FakePackageCatalog(params DatasheetFootprintCandidate[] candidates) : IDatasheetPackageCatalog
    {
        public IReadOnlyList<DatasheetFootprintCandidate> FindFootprintCandidates(string targetPackage) =>
            candidates
                .Where(candidate => string.Equals(candidate.PackageName, targetPackage, StringComparison.OrdinalIgnoreCase))
                .ToArray();
    }
}
