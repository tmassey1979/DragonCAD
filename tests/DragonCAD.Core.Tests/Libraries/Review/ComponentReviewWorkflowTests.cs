using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;
using DragonCAD.Core.Libraries.Review;

namespace DragonCAD.Core.Tests.Libraries.Review;

public sealed class ComponentReviewWorkflowTests
{
    private static readonly DateTimeOffset ImportedAt = new(2026, 6, 3, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReviewedAt = new(2026, 6, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReviewItemShowsSourceMetadataPreviewStatusConflictsAndWarnings()
    {
        ComponentDefinition candidate = FixtureComponent(
            id: "candidate:sparkfun/resistor-0603",
            displayName: "SparkFun RESISTOR-0603",
            manufacturer: "Yageo",
            manufacturerPartNumber: "RC0603FR-0710KL",
            description: "Imported resistor");
        ComponentReviewCandidateSource source = ComponentReviewCandidateSource.ImportedLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            ImportedAt);
        ComponentReviewQueue queue = new();

        ComponentReviewItem item = queue.Enqueue(
            candidate,
            source,
            new ComponentReviewPreviewStatus(ComponentReviewPreviewState.Ready, "2 symbol pins rendered."),
            new ComponentReviewPreviewStatus(ComponentReviewPreviewState.Warning, "Courtyard missing."),
            [new ComponentReviewConflict("ManufacturerPartNumber", "Existing MPN differs.")],
            [new ComponentReviewWarning("Footprint", "Courtyard outline is missing.")]);

        Assert.Equal(source, item.Source);
        Assert.Equal("SparkFun RESISTOR-0603", item.Metadata.DisplayName);
        Assert.Equal("Yageo", item.Metadata.Manufacturer);
        Assert.Equal("RC0603FR-0710KL", item.Metadata.ManufacturerPartNumber);
        Assert.Equal("Imported resistor", item.Metadata.Description);
        Assert.Equal(ComponentReviewPreviewState.Ready, item.SymbolPreview.State);
        Assert.Equal(ComponentReviewPreviewState.Warning, item.FootprintPreview.State);
        Assert.Equal("ManufacturerPartNumber", Assert.Single(item.Conflicts).Field);
        Assert.Equal("Footprint", Assert.Single(item.Warnings).Field);
    }

    [Fact]
    public void PromoteNewCreatesCanonicalComponentDecisionWithReviewerTimestampSourceAndChangedFields()
    {
        ComponentDefinition candidate = FixtureComponent(
            id: "candidate:acme/header-2x03",
            displayName: "ACME HEADER-2X03",
            manufacturer: "Acme",
            manufacturerPartNumber: "BH-2X03",
            description: "Bench header");
        ComponentReviewCandidateSource source = ComponentReviewCandidateSource.ImportedLibrary(
            "bench-library",
            "C:/cad/libs/bench-connectors.hclib.json",
            ImportedAt);
        ComponentReviewQueue queue = new();
        ComponentReviewItem item = queue.Enqueue(
            candidate,
            source,
            ComponentReviewPreviewStatus.Ready("Symbol preview rendered."),
            ComponentReviewPreviewStatus.Ready("Footprint preview rendered."),
            conflicts: [],
            warnings: []);

        ComponentPromotionRecord record = queue.PromoteNew(
            item.Id,
            new ComponentId("perm:acme/header-2x03"),
            reviewer: "terri.maintainer",
            reviewedAt: ReviewedAt,
            changedFields: ["DisplayName", "ManufacturerPartNumber"]);

        Assert.Equal(ComponentPromotionDecision.CreateCanonical, record.Decision);
        Assert.Equal(item.Id, record.ReviewItemId);
        Assert.Equal("candidate:acme/header-2x03", record.CandidateComponentId.Value);
        Assert.Equal("perm:acme/header-2x03", record.CanonicalComponentId.GetValueOrDefault().Value);
        Assert.Equal("terri.maintainer", record.Reviewer);
        Assert.Equal(ReviewedAt, record.ReviewedAt);
        Assert.Equal(source, record.Source);
        Assert.Equal(["DisplayName", "ManufacturerPartNumber"], record.ChangedFields);
        Assert.Equal(ComponentReviewItemState.Promoted, queue.Get(item.Id).State);
    }

    [Fact]
    public void LinkExistingRecordsCanonicalLinkWithoutReplacingSourceProvenance()
    {
        ComponentDefinition candidate = FixtureComponent(
            id: "candidate:sparkfun/resistor-0603",
            displayName: "SparkFun RESISTOR-0603",
            manufacturer: "Yageo",
            manufacturerPartNumber: "RC0603FR-0710KL",
            description: "Imported resistor");
        ComponentReviewCandidateSource source = ComponentReviewCandidateSource.ImportedLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            ImportedAt);
        ComponentReviewQueue queue = new();
        ComponentReviewItem item = queue.Enqueue(
            candidate,
            source,
            ComponentReviewPreviewStatus.Ready("Symbol preview rendered."),
            ComponentReviewPreviewStatus.Ready("Footprint preview rendered."),
            conflicts: [],
            warnings: []);

        ComponentPromotionRecord record = queue.LinkExisting(
            item.Id,
            new ComponentId("perm:sparkfun/resistor-0603"),
            reviewer: "terri.maintainer",
            reviewedAt: ReviewedAt,
            changedFields: ["Provenance"]);

        Assert.Equal(ComponentPromotionDecision.LinkExistingCanonical, record.Decision);
        Assert.Equal("perm:sparkfun/resistor-0603", record.CanonicalComponentId.GetValueOrDefault().Value);
        Assert.Equal(source, record.Source);
        Assert.Equal(candidate.Provenance, queue.Get(item.Id).Candidate.Provenance);
        Assert.Equal(ComponentReviewItemState.Promoted, queue.Get(item.Id).State);
    }

    [Fact]
    public void RejectRecordsReviewerTimestampSourceAndReason()
    {
        ComponentReviewQueue queue = new();
        ComponentReviewItem item = queue.Enqueue(
            FixtureComponent(
                id: "candidate:vendor/unstable-part",
                displayName: "Vendor unstable part",
                manufacturer: "Vendor",
                manufacturerPartNumber: "UNKNOWN",
                description: "Candidate lacks verifiable geometry"),
            ComponentReviewCandidateSource.Generated("datasheet-extraction", "datasheets/unstable.pdf", ImportedAt),
            ComponentReviewPreviewStatus.Failed("Symbol extraction failed."),
            ComponentReviewPreviewStatus.Ready("Footprint preview rendered."),
            conflicts: [],
            warnings: [new ComponentReviewWarning("Symbol", "Symbol preview failed.")]);

        ComponentPromotionRecord record = queue.Reject(
            item.Id,
            reviewer: "terri.maintainer",
            reviewedAt: ReviewedAt,
            reason: "Symbol geometry could not be verified.");

        Assert.Equal(ComponentPromotionDecision.RejectCandidate, record.Decision);
        Assert.Null(record.CanonicalComponentId);
        Assert.Equal("Symbol geometry could not be verified.", record.RejectionReason);
        Assert.Equal("datasheet-extraction", record.Source.SourceName);
        Assert.Equal(["State", "RejectionReason"], record.ChangedFields);
        Assert.Equal(ComponentReviewItemState.Rejected, queue.Get(item.Id).State);
    }

    [Fact]
    public void CandidateWithConflictsRequiresConflictReviewBeforePromotion()
    {
        ComponentReviewQueue queue = new();
        ComponentReviewItem item = queue.Enqueue(
            FixtureComponent(
                id: "candidate:sparkfun/resistor-0603",
                displayName: "SparkFun RESISTOR-0603",
                manufacturer: "Yageo",
                manufacturerPartNumber: "RC0603FR-0710KL",
                description: "Changed resistor"),
            ComponentReviewCandidateSource.ImportedLibrary(
                "user-modified-sparkfun",
                "C:/cad/libs/SparkFun-Resistors-modified.lbr",
                ImportedAt),
            ComponentReviewPreviewStatus.Ready("Symbol preview rendered."),
            ComponentReviewPreviewStatus.Ready("Footprint preview rendered."),
            [new ComponentReviewConflict("Footprints[0].Pads[1].Size", "Pad size differs from canonical geometry.")],
            warnings: []);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            queue.PromoteNew(
                item.Id,
                new ComponentId("perm:sparkfun/resistor-0603"),
                reviewer: "terri.maintainer",
                reviewedAt: ReviewedAt,
                changedFields: ["Footprints"]));
        Assert.Contains("conflict review", exception.Message, StringComparison.OrdinalIgnoreCase);

        ComponentPromotionRecord record = queue.PromoteNew(
            item.Id,
            new ComponentId("perm:sparkfun/resistor-0603"),
            reviewer: "terri.maintainer",
            reviewedAt: ReviewedAt,
            changedFields: ["Footprints"],
            conflictReviewed: true);

        Assert.Equal(ComponentPromotionDecision.CreateCanonical, record.Decision);
        Assert.Equal(ComponentReviewItemState.Promoted, queue.Get(item.Id).State);
    }

    private static ComponentDefinition FixtureComponent(
        string id,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string description)
    {
        ComponentSymbolId symbolId = new($"{id}:symbol");
        ComponentFootprintId footprintId = new($"{id}:footprint");
        ComponentVariantId variantId = new($"{id}:variant");

        return new ComponentDefinition(
            new ComponentId(id),
            displayName,
            ComponentKind.Passive,
            manufacturer,
            manufacturerPartNumber,
            description,
            [new ComponentAttribute("Package", "0603")],
            [
                new ComponentPin(new ComponentPinId($"{id}:pin:1"), "1", "1", ComponentPinElectricalType.Passive),
                new ComponentPin(new ComponentPinId($"{id}:pin:2"), "2", "2", ComponentPinElectricalType.Passive)
            ],
            [
                new ComponentGate(
                    new ComponentGateId($"{id}:gate"),
                    "G$1",
                    symbolId,
                    [new ComponentPinId($"{id}:pin:1"), new ComponentPinId($"{id}:pin:2")])
            ],
            [
                new ComponentSymbol(
                    symbolId,
                    $"{id}:symbol",
                    [
                        new ComponentSymbolPin(new ComponentPinId($"{id}:pin:1"), new CadPoint(-2_540_000, 0), ComponentPinOrientation.Right),
                        new ComponentSymbolPin(new ComponentPinId($"{id}:pin:2"), new CadPoint(2_540_000, 0), ComponentPinOrientation.Left)
                    ],
                    [new ComponentLine(new CadPoint(-1_270_000, 0), new CadPoint(1_270_000, 0))],
                    [new ComponentSymbolText(ComponentSymbolTextKind.Reference, ">NAME", new CadPoint(0, 1_270_000))])
            ],
            [
                new ComponentFootprint(
                    footprintId,
                    $"{id}:footprint",
                    [
                        new ComponentFootprintPad(new ComponentPadId($"{id}:pad:1"), "1", new CadPoint(-750_000, 0), new CadVector(900_000, 700_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle),
                        new ComponentFootprintPad(new ComponentPadId($"{id}:pad:2"), "2", new CadPoint(750_000, 0), new CadVector(900_000, 700_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)
                    ],
                    [new ComponentLine(new CadPoint(-1_300_000, -500_000), new CadPoint(1_300_000, -500_000))],
                    [])
            ],
            [new ComponentVariant(variantId, "0603", footprintId, [])],
            [
                new ComponentPinPadMapping(variantId, new ComponentPinId($"{id}:pin:1"), new ComponentPadId($"{id}:pad:1")),
                new ComponentPinPadMapping(variantId, new ComponentPinId($"{id}:pin:2"), new ComponentPadId($"{id}:pad:2"))
            ],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            [new ComponentProvenanceRecord(ComponentProvenanceKind.EagleImport, "fixture-source", "Immutable source fixture.")]);
    }
}
