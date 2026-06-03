using DragonCAD.App.Datasheets.Extraction;

namespace DragonCAD.App.Tests.Datasheets.Extraction;

public sealed class DatasheetExtractionProviderRegistryTests
{
    [Fact]
    public async Task DefaultRegistryReturnsDisabledResultWhenNoProviderIsConfigured()
    {
        DatasheetExtractionProviderRunner runner = new(DatasheetExtractionProviderRegistry.Default);

        DatasheetExtractionResult result = await runner.ExtractAsync(
            "fake-provider",
            Request(DatasheetExtractionCapability.PinExtraction));

        Assert.False(result.IsSupported);
        Assert.Equal("fake-provider", result.ProviderId);
        Assert.Empty(result.Pins);
        Assert.Empty(result.Packages);
        Assert.Empty(result.ComponentFacts);
        Assert.Empty(result.ThreeDimensionalModelProposals);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DatasheetExtractionDiagnosticSeverity.Warning &&
            diagnostic.Message == "No datasheet extraction provider is configured for 'fake-provider'.");
        Assert.Contains(result.UnsupportedFeatures, warning =>
            warning.Capability == DatasheetExtractionCapability.PinExtraction &&
            warning.Reason == "Provider is not enabled.");
    }

    [Fact]
    public async Task FakeProviderReturnsDeterministicPinsPackagesFactsAnd3DProposalMetadata()
    {
        DeterministicFakeExtractionProvider provider = new();
        DatasheetExtractionProviderRunner runner = new(
            new DatasheetExtractionProviderRegistry([provider]));

        DatasheetExtractionResult result = await runner.ExtractAsync(
            provider.ProviderId,
            Request(
                DatasheetExtractionCapability.PinExtraction,
                DatasheetExtractionCapability.PackageFootprintExtraction,
                DatasheetExtractionCapability.ComponentFactsExtraction,
                DatasheetExtractionCapability.ThreeDimensionalModelProposal));

        Assert.True(result.IsSupported);
        Assert.Equal(0.91m, result.Confidence.Score);
        Assert.Equal("fake-provider", result.ProviderId);
        Assert.Equal("VIN", Assert.Single(result.Pins, pin => pin.Number == "1").Name);
        Assert.Equal("SOT-23-5", Assert.Single(result.Packages).PackageName);
        Assert.Equal("5", Assert.Single(result.ComponentFacts, fact => fact.Name == "pin-count").Value);
        Assert.Equal("SOT-23-5.step", Assert.Single(result.ThreeDimensionalModelProposals).ModelFileName);
        Assert.Equal("datasheet://lm2842#page=3", Assert.Single(result.SourceReferences).Location);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DatasheetExtractionDiagnosticSeverity.Info &&
            diagnostic.Message == "Fake provider completed deterministic extraction.");
    }

    [Fact]
    public async Task ProviderDiagnosticsArePropagated()
    {
        DiagnosticFakeExtractionProvider provider = new();
        DatasheetExtractionProviderRunner runner = new(
            new DatasheetExtractionProviderRegistry([provider]));

        DatasheetExtractionResult result = await runner.ExtractAsync(
            provider.ProviderId,
            Request(DatasheetExtractionCapability.ComponentFactsExtraction));

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DatasheetExtractionDiagnosticSeverity.Error &&
            diagnostic.Code == "FACT_PARSE_AMBIGUOUS" &&
            diagnostic.Message == "Operating temperature could not be resolved deterministically.");
    }

    [Fact]
    public async Task UnsupportedRequestedCapabilityIsReportedWithoutSuppressingSupportedOutput()
    {
        PinOnlyFakeExtractionProvider provider = new();
        DatasheetExtractionProviderRunner runner = new(
            new DatasheetExtractionProviderRegistry([provider]));

        DatasheetExtractionResult result = await runner.ExtractAsync(
            provider.ProviderId,
            Request(
                DatasheetExtractionCapability.PinExtraction,
                DatasheetExtractionCapability.ThreeDimensionalModelProposal));

        Assert.True(result.IsSupported);
        Assert.Single(result.Pins);
        DatasheetUnsupportedFeatureWarning warning = Assert.Single(result.UnsupportedFeatures);
        Assert.Equal(DatasheetExtractionCapability.ThreeDimensionalModelProposal, warning.Capability);
        Assert.Equal("Provider 'pin-only-fake' does not support 3D model proposal.", warning.Reason);
    }

    private static DatasheetExtractionRequest Request(params DatasheetExtractionCapability[] capabilities) =>
        new(
            DatasheetId: "lm2842",
            SourceName: "LM2842 datasheet",
            RequestedCapabilities: capabilities,
            SourceReferences:
            [
                new DatasheetSourceReference("datasheet://lm2842#page=3", "Electrical characteristics table")
            ]);

    private sealed class DeterministicFakeExtractionProvider : IDatasheetExtractionProvider
    {
        public string ProviderId => "fake-provider";

        public string DisplayName => "Deterministic fake provider";

        public DatasheetExtractionCapabilitySet Capabilities { get; } = DatasheetExtractionCapabilitySet.From(
            DatasheetExtractionCapability.PinExtraction,
            DatasheetExtractionCapability.PackageFootprintExtraction,
            DatasheetExtractionCapability.ComponentFactsExtraction,
            DatasheetExtractionCapability.ThreeDimensionalModelProposal);

        public Task<DatasheetExtractionResult> ExtractAsync(
            DatasheetExtractionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new DatasheetExtractionResult(
                ProviderId,
                IsSupported: true,
                new DatasheetExtractionConfidence(0.91m, "Fake provider fixed score"),
                SourceReferences: request.SourceReferences,
                Pins:
                [
                    new DatasheetExtractedPin("1", "VIN", "Power input", request.SourceReferences[0]),
                    new DatasheetExtractedPin("2", "GND", "Ground", request.SourceReferences[0])
                ],
                Packages:
                [
                    new DatasheetExtractedPackage("SOT-23-5", "IPC nominal footprint", request.SourceReferences[0])
                ],
                ComponentFacts:
                [
                    new DatasheetExtractedFact("pin-count", "5", request.SourceReferences[0])
                ],
                ThreeDimensionalModelProposals:
                [
                    new DatasheetThreeDimensionalModelProposal("SOT-23-5.step", "SOT-23-5", request.SourceReferences[0])
                ],
                Diagnostics:
                [
                    new DatasheetExtractionDiagnostic(
                        DatasheetExtractionDiagnosticSeverity.Info,
                        "FAKE_COMPLETE",
                        "Fake provider completed deterministic extraction.",
                        request.SourceReferences[0])
                ],
                UnsupportedFeatures: []));
        }
    }

    private sealed class DiagnosticFakeExtractionProvider : IDatasheetExtractionProvider
    {
        public string ProviderId => "diagnostic-fake";

        public string DisplayName => "Diagnostic fake provider";

        public DatasheetExtractionCapabilitySet Capabilities { get; } = DatasheetExtractionCapabilitySet.From(
            DatasheetExtractionCapability.ComponentFactsExtraction);

        public Task<DatasheetExtractionResult> ExtractAsync(
            DatasheetExtractionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(DatasheetExtractionResult.Supported(
                ProviderId,
                confidence: new DatasheetExtractionConfidence(0.2m, "Ambiguous fake extraction"),
                sourceReferences: request.SourceReferences,
                diagnostics:
                [
                    new DatasheetExtractionDiagnostic(
                        DatasheetExtractionDiagnosticSeverity.Error,
                        "FACT_PARSE_AMBIGUOUS",
                        "Operating temperature could not be resolved deterministically.",
                        request.SourceReferences[0])
                ]));
        }
    }

    private sealed class PinOnlyFakeExtractionProvider : IDatasheetExtractionProvider
    {
        public string ProviderId => "pin-only-fake";

        public string DisplayName => "Pin only fake provider";

        public DatasheetExtractionCapabilitySet Capabilities { get; } = DatasheetExtractionCapabilitySet.From(
            DatasheetExtractionCapability.PinExtraction);

        public Task<DatasheetExtractionResult> ExtractAsync(
            DatasheetExtractionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(DatasheetExtractionResult.Supported(
                ProviderId,
                confidence: new DatasheetExtractionConfidence(0.75m, "Pin-only fake extraction"),
                sourceReferences: request.SourceReferences,
                pins:
                [
                    new DatasheetExtractedPin("1", "VIN", "Power input", request.SourceReferences[0])
                ]));
        }
    }
}
