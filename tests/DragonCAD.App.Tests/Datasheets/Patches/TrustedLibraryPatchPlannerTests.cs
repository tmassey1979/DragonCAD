using DragonCAD.App.Datasheets.Patches;

namespace DragonCAD.App.Tests.Datasheets.Patches;

public sealed class TrustedLibraryPatchPlannerTests
{
    [Fact]
    public void ApprovedCandidateCreatesDeterministicPatchSummaryWithoutMutatingTrustedLibrary()
    {
        TrustedLibraryPatchPlan plan = TrustedLibraryPatchPlanner.CreatePlan(
            Candidate(
                approval: new TrustedLibraryPatchApproval("reviewer@hawkcad.test", "CMP-008 approval", ApprovedAtUtc),
                diagnostics:
                [
                    TrustedLibraryPatchDiagnostic.Resolved("PIN_COUNT_REVIEWED", "Pin count reviewed by maintainer.")
                ]));

        Assert.True(plan.CanCreatePatch);
        Assert.Equal(TrustedLibraryPatchPlanState.Ready, plan.State);
        Assert.False(plan.MutatesTrustedLibrary);
        Assert.Empty(plan.BlockingDiagnostics);
        Assert.Equal("Trusted library patch ready for 1 component candidate.", plan.Summary);

        TrustedLibraryPatchSummary patch = Assert.Single(plan.Patches);
        Assert.Equal("cmp-008-lm7805", patch.CandidateId);
        Assert.Equal("reviewer@hawkcad.test", patch.Approval.Reviewer);
        Assert.Equal("CMP-008 approval", patch.Approval.Note);
        Assert.Equal(ApprovedAtUtc, patch.Approval.ApprovedAtUtc);
        Assert.Equal("https://datasheets.example/lm7805.pdf", Assert.Single(patch.Datasheets).Location);
        Assert.Equal("LM7805", Assert.Single(patch.Components).Name);
        Assert.Equal("LM7805_TO220", Assert.Single(patch.Symbols).Name);
        Assert.Equal("TO-220-3", Assert.Single(patch.Footprints).Name);
        Assert.Equal(("GND", "2"), (patch.Mappings[0].SymbolPin, patch.Mappings[0].FootprintPad));
        Assert.Equal(("VI", "1"), (patch.Mappings[1].SymbolPin, patch.Mappings[1].FootprintPad));
        Assert.Equal(("VO", "3"), (patch.Mappings[2].SymbolPin, patch.Mappings[2].FootprintPad));
        Assert.All(
            patch.ContentHashes,
            hash => Assert.Matches("^[a-f0-9]{64}$", hash.Sha256));
    }

    [Fact]
    public void UnapprovedCandidateBlocksPatchCreation()
    {
        TrustedLibraryPatchPlan plan = TrustedLibraryPatchPlanner.CreatePlan(
            new TrustedLibraryPatchCandidate(
                CandidateId: "cmp-008-lm7805",
                TargetLibraryId: "hawkcad-core",
                Approval: null,
                Diagnostics: [],
                Components: [Component("LM7805")],
                Symbols: [Symbol("LM7805_TO220")],
                Footprints: [Footprint("TO-220-3")],
                Mappings: [Mapping("VI", "1"), Mapping("GND", "2"), Mapping("VO", "3")],
                Datasheets: [Datasheet("lm7805-datasheet", "https://datasheets.example/lm7805.pdf")]));

        Assert.False(plan.CanCreatePatch);
        Assert.Equal(TrustedLibraryPatchPlanState.Blocked, plan.State);
        TrustedLibraryPatchDiagnostic diagnostic = Assert.Single(plan.BlockingDiagnostics);
        Assert.Equal("TRUSTED_LIBRARY_PATCH_APPROVAL_REQUIRED", diagnostic.Code);
        Assert.Empty(plan.Patches);
    }

    [Fact]
    public void UnresolvedDiagnosticsBlockPatchCreation()
    {
        TrustedLibraryPatchPlan plan = TrustedLibraryPatchPlanner.CreatePlan(
            Candidate(
                diagnostics:
                [
                    TrustedLibraryPatchDiagnostic.Unresolved("PIN_MISMATCH", "Pin 2 is unresolved.")
                ]));

        Assert.False(plan.CanCreatePatch);
        Assert.Equal(TrustedLibraryPatchPlanState.Blocked, plan.State);
        TrustedLibraryPatchDiagnostic diagnostic = Assert.Single(plan.BlockingDiagnostics);
        Assert.Equal("TRUSTED_LIBRARY_PATCH_UNRESOLVED_DIAGNOSTIC", diagnostic.Code);
        Assert.Contains("PIN_MISMATCH", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Patches);
    }

    [Fact]
    public void MissingRequiredSymbolAndFootprintDataBlocksPatchCreation()
    {
        TrustedLibraryPatchPlan plan = TrustedLibraryPatchPlanner.CreatePlan(
            Candidate(symbols: [], footprints: []));

        Assert.False(plan.CanCreatePatch);
        Assert.Equal(TrustedLibraryPatchPlanState.Blocked, plan.State);
        Assert.Contains(plan.BlockingDiagnostics, diagnostic => diagnostic.Code == "TRUSTED_LIBRARY_PATCH_SYMBOL_REQUIRED");
        Assert.Contains(plan.BlockingDiagnostics, diagnostic => diagnostic.Code == "TRUSTED_LIBRARY_PATCH_FOOTPRINT_REQUIRED");
        Assert.Empty(plan.Patches);
    }

    [Fact]
    public void PatchSummaryOrdersContentDeterministically()
    {
        TrustedLibraryPatchCandidate unordered = Candidate(
            components:
            [
                Component("zz-regulator"),
                Component("aa-regulator")
            ],
            symbols:
            [
                Symbol("zz-symbol"),
                Symbol("aa-symbol")
            ],
            footprints:
            [
                Footprint("zz-footprint"),
                Footprint("aa-footprint")
            ],
            mappings:
            [
                Mapping("VO", "3"),
                Mapping("GND", "2"),
                Mapping("VI", "1")
            ],
            datasheets:
            [
                Datasheet("zz-datasheet", "https://datasheets.example/z.pdf"),
                Datasheet("aa-datasheet", "https://datasheets.example/a.pdf")
            ]);

        TrustedLibraryPatchSummary patch = Assert.Single(TrustedLibraryPatchPlanner.CreatePlan(unordered).Patches);

        Assert.Equal(["aa-regulator", "zz-regulator"], patch.Components.Select(component => component.Name));
        Assert.Equal(["aa-symbol", "zz-symbol"], patch.Symbols.Select(symbol => symbol.Name));
        Assert.Equal(["aa-footprint", "zz-footprint"], patch.Footprints.Select(footprint => footprint.Name));
        Assert.Equal(["GND", "VI", "VO"], patch.Mappings.Select(mapping => mapping.SymbolPin));
        Assert.Equal(["aa-datasheet", "zz-datasheet"], patch.Datasheets.Select(datasheet => datasheet.ReferenceId));
    }

    [Fact]
    public void ContentHashesAreStableAcrossEquivalentInputOrdering()
    {
        TrustedLibraryPatchCandidate first = Candidate(
            mappings:
            [
                Mapping("VO", "3"),
                Mapping("VI", "1"),
                Mapping("GND", "2")
            ]);
        TrustedLibraryPatchCandidate second = Candidate(
            mappings:
            [
                Mapping("GND", "2"),
                Mapping("VO", "3"),
                Mapping("VI", "1")
            ]);

        TrustedLibraryPatchSummary firstPatch = Assert.Single(TrustedLibraryPatchPlanner.CreatePlan(first).Patches);
        TrustedLibraryPatchSummary secondPatch = Assert.Single(TrustedLibraryPatchPlanner.CreatePlan(second).Patches);

        Assert.Equal(firstPatch.PatchHash, secondPatch.PatchHash);
        Assert.Equal(firstPatch.ContentHashes, secondPatch.ContentHashes);
    }

    private static readonly DateTimeOffset ApprovedAtUtc = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    private static TrustedLibraryPatchCandidate Candidate(
        TrustedLibraryPatchApproval? approval = null,
        IReadOnlyList<TrustedLibraryPatchDiagnostic>? diagnostics = null,
        IReadOnlyList<TrustedLibraryPatchComponent>? components = null,
        IReadOnlyList<TrustedLibraryPatchSymbol>? symbols = null,
        IReadOnlyList<TrustedLibraryPatchFootprint>? footprints = null,
        IReadOnlyList<TrustedLibraryPatchMapping>? mappings = null,
        IReadOnlyList<TrustedLibraryPatchDatasheetReference>? datasheets = null) =>
        new(
            CandidateId: "cmp-008-lm7805",
            TargetLibraryId: "hawkcad-core",
            Approval: approval ?? new TrustedLibraryPatchApproval("reviewer@hawkcad.test", "Approved.", ApprovedAtUtc),
            Diagnostics: diagnostics ?? [],
            Components: components ?? [Component("LM7805")],
            Symbols: symbols ?? [Symbol("LM7805_TO220")],
            Footprints: footprints ?? [Footprint("TO-220-3")],
            Mappings: mappings ?? [Mapping("VI", "1"), Mapping("GND", "2"), Mapping("VO", "3")],
            Datasheets: datasheets ?? [Datasheet("lm7805-datasheet", "https://datasheets.example/lm7805.pdf")]);

    private static TrustedLibraryPatchComponent Component(string name) =>
        new(
            Name: name,
            Manufacturer: "Texas Instruments",
            ManufacturerPartNumber: "LM7805CT",
            Description: "5V linear regulator");

    private static TrustedLibraryPatchSymbol Symbol(string name) =>
        new(
            Name: name,
            Pins:
            [
                new TrustedLibraryPatchSymbolPin("VI", "Voltage input"),
                new TrustedLibraryPatchSymbolPin("GND", "Ground"),
                new TrustedLibraryPatchSymbolPin("VO", "Voltage output")
            ]);

    private static TrustedLibraryPatchFootprint Footprint(string name) =>
        new(
            Name: name,
            Pads:
            [
                new TrustedLibraryPatchFootprintPad("1", "VI"),
                new TrustedLibraryPatchFootprintPad("2", "GND"),
                new TrustedLibraryPatchFootprintPad("3", "VO")
            ]);

    private static TrustedLibraryPatchMapping Mapping(string symbolPin, string footprintPad) =>
        new(symbolPin, footprintPad);

    private static TrustedLibraryPatchDatasheetReference Datasheet(string referenceId, string location) =>
        new(referenceId, "LM7805 datasheet", location);
}
