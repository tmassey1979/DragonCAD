using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;
using DragonCAD.Core.Components.Promotion.Persistence;
using DragonCAD.Core.Geometry;
using System.Text.Json;

namespace DragonCAD.Core.Tests.Components.Promotion.Persistence;

public sealed class TrustedLibraryPromotionStoreTests
{
    private static readonly DateTimeOffset ReviewedAt = new(2026, 6, 3, 14, 15, 16, TimeSpan.Zero);

    [Fact]
    public void PromoteNewPersistsReviewedCandidateWithProvenancePackageMappingRollbackAndAudit()
    {
        using TempPromotionFile temp = new();
        TrustedLibraryPromotionStore store = new(temp.Path);

        TrustedLibraryPromotionResult result = store.Apply(CreateRequest(TrustedLibraryPromotionAction.PromoteNew));

        Assert.Equal(TrustedLibraryPromotionStatus.Promoted, result.Status);
        Assert.False(result.IsBlocked);
        TrustedLibraryPromotionLibrary library = store.Load();
        TrustedLibraryPromotionRecord record = Assert.Single(library.Records);
        Assert.Equal("draft-ne555p", record.ComponentId);
        Assert.Equal("prov-datasheet-ne555p", record.SourceProvenanceId);
        Assert.Equal("Jamie Reviewer", record.Reviewer);
        Assert.Equal(ReviewedAt, record.ReviewedAt);
        Assert.Equal("DIP-8", record.Package.Name);
        Assert.Equal("U", record.Package.ReferencePrefix);
        Assert.Equal(["pin-1->dip-8:pad-1", "pin-2->dip-8:pad-2"], record.Package.FootprintMappings);
        Assert.Equal("sha256:datasheet", record.Provenance.DatasheetChecksum);
        Assert.Equal(TrustedLibraryPromotionRecordState.Trusted, record.State);
        Assert.Equal("remove:draft-ne555p", Assert.Single(record.RollbackActions));
        TrustedLibraryPromotionAuditRecord audit = Assert.Single(library.AuditRecords);
        Assert.Equal(TrustedLibraryPromotionAuditKind.PromotedNew, audit.Kind);
        Assert.Equal("CMP-004-DECISION-001", audit.DecisionId);
    }

    [Fact]
    public void LinkExistingKeepsExistingTrustedRecordAndAddsReviewAudit()
    {
        using TempPromotionFile temp = new();
        TrustedLibraryPromotionStore store = new(temp.Path);
        store.Apply(CreateRequest(TrustedLibraryPromotionAction.PromoteNew));

        TrustedLibraryPromotionResult result = store.Apply(
            CreateRequest(
                TrustedLibraryPromotionAction.LinkExisting,
                decisionId: "CMP-004-DECISION-002",
                existingComponentId: "draft-ne555p"));

        Assert.Equal(TrustedLibraryPromotionStatus.LinkedExisting, result.Status);
        TrustedLibraryPromotionLibrary library = store.Load();
        TrustedLibraryPromotionRecord record = Assert.Single(library.Records);
        Assert.Equal("CMP-004-DECISION-002", record.LastDecisionId);
        Assert.Equal(["remove:draft-ne555p", "unlink:draft-ne555p:CMP-004-DECISION-002"], record.RollbackActions);
        Assert.Equal(
            [TrustedLibraryPromotionAuditKind.PromotedNew, TrustedLibraryPromotionAuditKind.LinkedExisting],
            library.AuditRecords.Select(audit => audit.Kind).ToArray());
    }

    [Fact]
    public void RejectPersistsNoTrustedRecordButAppendsDecisionAudit()
    {
        using TempPromotionFile temp = new();
        TrustedLibraryPromotionStore store = new(temp.Path);

        TrustedLibraryPromotionResult result = store.Apply(
            CreateRequest(TrustedLibraryPromotionAction.Reject, reviewState: MarketplaceReviewState.Rejected));

        Assert.Equal(TrustedLibraryPromotionStatus.Rejected, result.Status);
        TrustedLibraryPromotionLibrary library = store.Load();
        Assert.Empty(library.Records);
        TrustedLibraryPromotionAuditRecord audit = Assert.Single(library.AuditRecords);
        Assert.Equal(TrustedLibraryPromotionAuditKind.Rejected, audit.Kind);
        Assert.Equal("Rejected", audit.ReviewState);
        Assert.Equal("prov-datasheet-ne555p", audit.SourceProvenanceId);
    }

    [Fact]
    public void ConflictAgainstVerifiedGeometryBlocksWithoutExplicitDecisionAndDoesNotOverwriteRecord()
    {
        using TempPromotionFile temp = new();
        TrustedLibraryPromotionStore store = new(temp.Path);
        store.Apply(CreateRequest(TrustedLibraryPromotionAction.PromoteNew));

        TrustedLibraryPromotionResult result = store.Apply(
            CreateRequest(
                TrustedLibraryPromotionAction.LinkExisting,
                decisionId: "CMP-004-DECISION-002",
                draft: CreateValidDraft() with
                {
                    Footprints =
                    [
                        new ComponentDraftFootprint(
                            new ComponentFootprintId("dip-8"),
                            "DIP-8 changed",
                            [new ComponentDraftPad(new ComponentPadId("pad-1"), "1", new CadPoint(0, 0), new CadVector(1800, 1800), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800)],
                            [],
                            []),
                    ],
                    DeviceMappings = [new ComponentDraftDeviceMapping(new ComponentPinId("pin-1"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-1"))],
                },
                existingComponentId: "draft-ne555p"));

        Assert.Equal(TrustedLibraryPromotionStatus.Blocked, result.Status);
        TrustedLibraryPromotionConflict conflict = Assert.Single(result.Conflicts);
        Assert.Equal(TrustedLibraryPromotionConflictKind.VerifiedGeometry, conflict.Kind);
        TrustedLibraryPromotionLibrary library = store.Load();
        TrustedLibraryPromotionRecord record = Assert.Single(library.Records);
        Assert.Equal("CMP-004-DECISION-001", record.LastDecisionId);
        Assert.Single(library.AuditRecords);
    }

    [Fact]
    public void AuditReplayReconstructsTrustedRecordsDeterministically()
    {
        using TempPromotionFile temp = new();
        TrustedLibraryPromotionStore store = new(temp.Path);
        store.Apply(CreateRequest(TrustedLibraryPromotionAction.PromoteNew));
        store.Apply(CreateRequest(TrustedLibraryPromotionAction.LinkExisting, decisionId: "CMP-004-DECISION-002", existingComponentId: "draft-ne555p"));

        TrustedLibraryPromotionLibrary saved = store.Load();
        TrustedLibraryPromotionLibrary replayed = TrustedLibraryPromotionAuditReplay.Replay(saved.AuditRecords);

        Assert.Equal(JsonSerializer.Serialize(saved.Records), JsonSerializer.Serialize(replayed.Records));
        Assert.Equal(JsonSerializer.Serialize(saved.AuditRecords), JsonSerializer.Serialize(replayed.AuditRecords));
    }

    private static TrustedLibraryPromotionRequest CreateRequest(
        TrustedLibraryPromotionAction action,
        string decisionId = "CMP-004-DECISION-001",
        string? existingComponentId = null,
        MarketplaceReviewState reviewState = MarketplaceReviewState.Approved,
        ComponentDraft? draft = null) =>
        new(
            action,
            draft ?? CreateValidDraft(),
            TargetLibraryId: "core-timers",
            Reviewer: "Jamie Reviewer",
            DecisionId: decisionId,
            ReviewedAt,
            SourceProvenanceId: "prov-datasheet-ne555p",
            SourceProvenance: MarketplaceComponentProvenance.DatasheetGenerated(
                new CanonicalComponentKey("draft-ne555p"),
                sourceVendor: "Texas Instruments",
                productUrl: "https://example.test/ne555p",
                datasheetUrl: "https://example.test/ne555p.pdf",
                datasheetChecksum: "sha256:datasheet",
                generatorName: "datasheet-bot",
                reviewState,
                timestamp: ReviewedAt.AddMinutes(-5)),
            ExistingComponentId: existingComponentId,
            ConflictDecisions: new Dictionary<string, TrustedLibraryPromotionConflictDecision>());

    private static ComponentDraft CreateValidDraft() =>
        new(
            new ComponentId("draft-ne555p"),
            "NE555P timer",
            new ComponentDraftPackage("DIP-8", "U", []),
            [new ComponentDraftAttribute("manufacturer", "Texas Instruments")],
            [
                new ComponentDraftPin(new ComponentPinId("pin-1"), "GND", "1", ComponentDraftPinElectricalType.Power),
                new ComponentDraftPin(new ComponentPinId("pin-2"), "TRIG", "2", ComponentDraftPinElectricalType.Input),
            ],
            [
                new ComponentDraftSymbol(
                    new ComponentSymbolId("symbol-ne555"),
                    "Timer symbol",
                    [
                        new ComponentDraftSymbolPin(new ComponentPinId("pin-1"), new CadPoint(-1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Right),
                        new ComponentDraftSymbolPin(new ComponentPinId("pin-2"), new CadPoint(1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Left),
                    ],
                    [new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Rectangle, new CadPoint(-500, -500), new CadPoint(500, 500))]),
            ],
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("dip-8"),
                    "DIP-8",
                    [
                        new ComponentDraftPad(new ComponentPadId("pad-1"), "1", new CadPoint(0, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800),
                        new ComponentDraftPad(new ComponentPadId("pad-2"), "2", new CadPoint(2540, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800),
                    ],
                    [],
                    []),
            ],
            [
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-1"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-1")),
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-2"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-2")),
            ]);

    private sealed class TempPromotionFile : IDisposable
    {
        public TempPromotionFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.trusted-promotions.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
