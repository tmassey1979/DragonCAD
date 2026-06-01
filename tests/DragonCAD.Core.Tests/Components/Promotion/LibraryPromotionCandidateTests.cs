using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;
using DragonCAD.Core.Components.Promotion;

namespace DragonCAD.Core.Tests.Components.Promotion;

public sealed class LibraryPromotionCandidateTests
{
    [Fact]
    public void ApprovedCandidateProducesNonMutatingPromotionPackageSummary()
    {
        LibraryPromotionCandidate candidate = LibraryPromotionCandidate.Create(
            componentId: CanonicalComponentKey.FromPartNumber("LM7805CT"),
            componentName: "7805 5V regulator",
            sourceProvenanceId: "prov-digikey-lm7805ct",
            targetLibraryId: "core-regulators",
            reviewState: MarketplaceReviewState.Approved,
            assets:
            [
                new PromotionAssetSummary(PromotionAssetKind.Symbol, "regulator-3pin", "3-pin regulator symbol", "sha256:symbol"),
                new PromotionAssetSummary(PromotionAssetKind.Footprint, "to-220-3", "TO-220-3 footprint", "sha256:footprint"),
                new PromotionAssetSummary(PromotionAssetKind.Model3D, "to-220-3-step", "TO-220-3 STEP model", "sha256:model"),
            ],
            diagnostics: []);

        LibraryPromotionPackage package = candidate.ToPackage();

        Assert.True(package.CanPromote);
        Assert.False(package.MutatesLibrary);
        Assert.Equal("PART:LM7805CT -> core-regulators | Approved | 3 assets | 0 diagnostics", package.Summary);
        Assert.Equal(["Footprint:to-220-3", "Model3D:to-220-3-step", "Symbol:regulator-3pin"], package.AssetLines);
    }

    [Fact]
    public void CandidateWithDiagnosticsIsBlockedEvenWhenReviewIsApproved()
    {
        LibraryPromotionCandidate candidate = LibraryPromotionCandidate.Create(
            componentId: CanonicalComponentKey.FromPartNumber("NE555P"),
            componentName: "NE555 timer",
            sourceProvenanceId: "prov-mouser-ne555p",
            targetLibraryId: "core-timers",
            reviewState: MarketplaceReviewState.Approved,
            assets: [new PromotionAssetSummary(PromotionAssetKind.Symbol, "timer-8pin", "Timer symbol", "sha256:timer")],
            diagnostics: [new PromotionDiagnostic(PromotionDiagnosticSeverity.Error, "Missing DIP-8 footprint.")]);

        LibraryPromotionPackage package = candidate.ToPackage();

        Assert.False(package.CanPromote);
        Assert.Equal(["Error: Missing DIP-8 footprint."], package.DiagnosticLines);
        Assert.Equal("PART:NE555P -> core-timers | Blocked | 1 assets | 1 diagnostics", package.Summary);
    }

    [Fact]
    public void TargetLibraryIsRequired()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            LibraryPromotionCandidate.Create(
                componentId: CanonicalComponentKey.FromPartNumber("ESP32-DEVKITC"),
                componentName: "ESP32 DevKitC",
                sourceProvenanceId: "prov-sparkfun-esp32",
                targetLibraryId: " ",
                reviewState: MarketplaceReviewState.Approved,
                assets: [],
                diagnostics: []));

        Assert.Contains("targetLibraryId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceProvenanceIdIsRequired()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            LibraryPromotionCandidate.Create(
                componentId: CanonicalComponentKey.FromPartNumber("L7805CV"),
                componentName: "7805 regulator",
                sourceProvenanceId: "",
                targetLibraryId: "core-regulators",
                reviewState: MarketplaceReviewState.Approved,
                assets: [],
                diagnostics: []));

        Assert.Contains("sourceProvenanceId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssetsAreOrderedDeterministically()
    {
        LibraryPromotionCandidate candidate = LibraryPromotionCandidate.Create(
            componentId: CanonicalComponentKey.FromPartNumber("ESP32-DEVKITC"),
            componentName: "ESP32 DevKitC",
            sourceProvenanceId: "prov-adafruit-esp32",
            targetLibraryId: "core-microcontrollers",
            reviewState: MarketplaceReviewState.Approved,
            assets:
            [
                new PromotionAssetSummary(PromotionAssetKind.Symbol, "esp32-symbol", "ESP32 symbol", "sha256:symbol"),
                new PromotionAssetSummary(PromotionAssetKind.Model3D, "esp32-board", "ESP32 dev board model", "sha256:model"),
                new PromotionAssetSummary(PromotionAssetKind.Footprint, "esp32-devkitc", "ESP32 DevKitC footprint", "sha256:footprint"),
            ],
            diagnostics: []);

        Assert.Equal(
            ["Footprint:esp32-devkitc", "Model3D:esp32-board", "Symbol:esp32-symbol"],
            candidate.ToPackage().AssetLines);
    }

    [Fact]
    public void PendingReviewBlocksPromotionWithoutDiagnostics()
    {
        LibraryPromotionCandidate candidate = LibraryPromotionCandidate.Create(
            componentId: CanonicalComponentKey.FromPartNumber("TLC555CP"),
            componentName: "TLC555 timer",
            sourceProvenanceId: "prov-datasheet-tlc555",
            targetLibraryId: "core-timers",
            reviewState: MarketplaceReviewState.PendingReview,
            assets: [new PromotionAssetSummary(PromotionAssetKind.Symbol, "timer-cmos", "CMOS timer symbol", "sha256:timer")],
            diagnostics: []);

        LibraryPromotionPackage package = candidate.ToPackage();

        Assert.False(package.CanPromote);
        Assert.Equal("PART:TLC555CP -> core-timers | Blocked | 1 assets | 0 diagnostics", package.Summary);
    }
}
