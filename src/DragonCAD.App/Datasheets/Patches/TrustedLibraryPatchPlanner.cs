using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DragonCAD.App.Datasheets.Patches;

public static class TrustedLibraryPatchPlanner
{
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = false
    };

    public static TrustedLibraryPatchPlan CreatePlan(params TrustedLibraryPatchCandidate[] candidates) =>
        CreatePlan((IReadOnlyList<TrustedLibraryPatchCandidate>)candidates);

    public static TrustedLibraryPatchPlan CreatePlan(IReadOnlyList<TrustedLibraryPatchCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        List<TrustedLibraryPatchDiagnostic> blockingDiagnostics = [];
        List<TrustedLibraryPatchSummary> patches = [];

        foreach (TrustedLibraryPatchCandidate candidate in candidates.OrderBy(candidate => candidate.CandidateId, StringComparer.Ordinal))
        {
            IReadOnlyList<TrustedLibraryPatchDiagnostic> candidateDiagnostics = CreateBlockingDiagnostics(candidate);
            blockingDiagnostics.AddRange(candidateDiagnostics);

            if (candidateDiagnostics.Count == 0)
            {
                patches.Add(CreateSummary(candidate));
            }
        }

        TrustedLibraryPatchPlanState state = blockingDiagnostics.Count == 0
            ? TrustedLibraryPatchPlanState.Ready
            : TrustedLibraryPatchPlanState.Blocked;

        return new TrustedLibraryPatchPlan(
            State: state,
            MutatesTrustedLibrary: false,
            Patches: patches,
            BlockingDiagnostics: blockingDiagnostics);
    }

    private static IReadOnlyList<TrustedLibraryPatchDiagnostic> CreateBlockingDiagnostics(TrustedLibraryPatchCandidate candidate)
    {
        List<TrustedLibraryPatchDiagnostic> diagnostics = [];

        if (string.IsNullOrWhiteSpace(candidate.CandidateId))
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_CANDIDATE_ID_REQUIRED",
                "Candidate id is required.",
                IsResolved: false));
        }

        if (string.IsNullOrWhiteSpace(candidate.TargetLibraryId))
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_TARGET_LIBRARY_REQUIRED",
                $"Candidate {candidate.CandidateId} requires a target library id.",
                IsResolved: false));
        }

        if (candidate.Approval is null)
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_APPROVAL_REQUIRED",
                $"Candidate {candidate.CandidateId} requires reviewer approval.",
                IsResolved: false));
        }

        if (candidate.Components.Count == 0)
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_COMPONENT_REQUIRED",
                $"Candidate {candidate.CandidateId} requires at least one component.",
                IsResolved: false));
        }

        if (candidate.Symbols.Count == 0 || candidate.Symbols.Any(symbol => symbol.Pins.Count == 0))
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_SYMBOL_REQUIRED",
                $"Candidate {candidate.CandidateId} requires symbol data with pins.",
                IsResolved: false));
        }

        if (candidate.Footprints.Count == 0 || candidate.Footprints.Any(footprint => footprint.Pads.Count == 0))
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_FOOTPRINT_REQUIRED",
                $"Candidate {candidate.CandidateId} requires footprint data with pads.",
                IsResolved: false));
        }

        foreach (TrustedLibraryPatchDiagnostic unresolved in candidate.Diagnostics.Where(diagnostic => !diagnostic.IsResolved))
        {
            diagnostics.Add(new TrustedLibraryPatchDiagnostic(
                "TRUSTED_LIBRARY_PATCH_UNRESOLVED_DIAGNOSTIC",
                $"Candidate {candidate.CandidateId} has unresolved diagnostic {unresolved.Code}: {unresolved.Message}",
                IsResolved: false));
        }

        return diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static TrustedLibraryPatchSummary CreateSummary(TrustedLibraryPatchCandidate candidate)
    {
        TrustedLibraryPatchApproval approval = candidate.Approval
            ?? throw new InvalidOperationException("Patch summaries require approval.");

        TrustedLibraryPatchSummaryContent content = new(
            CandidateId: candidate.CandidateId.Trim(),
            TargetLibraryId: candidate.TargetLibraryId.Trim(),
            Approval: approval,
            Diagnostics: SortDiagnostics(candidate.Diagnostics),
            Components: SortComponents(candidate.Components),
            Symbols: SortSymbols(candidate.Symbols),
            Footprints: SortFootprints(candidate.Footprints),
            Mappings: SortMappings(candidate.Mappings),
            Datasheets: SortDatasheets(candidate.Datasheets));

        IReadOnlyList<TrustedLibraryPatchContentHash> contentHashes =
        [
            Hash("approval", content.Approval),
            Hash("components", content.Components),
            Hash("symbols", content.Symbols),
            Hash("footprints", content.Footprints),
            Hash("mappings", content.Mappings),
            Hash("datasheets", content.Datasheets),
            Hash("diagnostics", content.Diagnostics)
        ];

        return new TrustedLibraryPatchSummary(
            CandidateId: content.CandidateId,
            TargetLibraryId: content.TargetLibraryId,
            Components: content.Components,
            Symbols: content.Symbols,
            Footprints: content.Footprints,
            Mappings: content.Mappings,
            Datasheets: content.Datasheets,
            Approval: content.Approval,
            Diagnostics: content.Diagnostics,
            ContentHashes: contentHashes,
            PatchHash: HashValue(content));
    }

    private static IReadOnlyList<TrustedLibraryPatchComponent> SortComponents(IReadOnlyList<TrustedLibraryPatchComponent> components) =>
        components
            .OrderBy(component => component.Name, StringComparer.Ordinal)
            .ThenBy(component => component.Manufacturer, StringComparer.Ordinal)
            .ThenBy(component => component.ManufacturerPartNumber, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPatchSymbol> SortSymbols(IReadOnlyList<TrustedLibraryPatchSymbol> symbols) =>
        symbols
            .OrderBy(symbol => symbol.Name, StringComparer.Ordinal)
            .Select(symbol => symbol with
            {
                Pins = symbol.Pins
                    .OrderBy(pin => pin.Name, StringComparer.Ordinal)
                    .ThenBy(pin => pin.Label, StringComparer.Ordinal)
                    .ToArray()
            })
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPatchFootprint> SortFootprints(IReadOnlyList<TrustedLibraryPatchFootprint> footprints) =>
        footprints
            .OrderBy(footprint => footprint.Name, StringComparer.Ordinal)
            .Select(footprint => footprint with
            {
                Pads = footprint.Pads
                    .OrderBy(pad => pad.Name, StringComparer.Ordinal)
                    .ThenBy(pad => pad.Label, StringComparer.Ordinal)
                    .ToArray()
            })
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPatchMapping> SortMappings(IReadOnlyList<TrustedLibraryPatchMapping> mappings) =>
        mappings
            .OrderBy(mapping => mapping.SymbolPin, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.FootprintPad, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPatchDatasheetReference> SortDatasheets(
        IReadOnlyList<TrustedLibraryPatchDatasheetReference> datasheets) =>
        datasheets
            .OrderBy(datasheet => datasheet.ReferenceId, StringComparer.Ordinal)
            .ThenBy(datasheet => datasheet.Location, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPatchDiagnostic> SortDiagnostics(IReadOnlyList<TrustedLibraryPatchDiagnostic> diagnostics) =>
        diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.IsResolved)
            .ToArray();

    private static TrustedLibraryPatchContentHash Hash(string contentKind, object value) =>
        new(contentKind, HashValue(value));

    private static string HashValue(object value)
    {
        string json = JsonSerializer.Serialize(value, CanonicalJsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record TrustedLibraryPatchSummaryContent(
        string CandidateId,
        string TargetLibraryId,
        TrustedLibraryPatchApproval Approval,
        IReadOnlyList<TrustedLibraryPatchDiagnostic> Diagnostics,
        IReadOnlyList<TrustedLibraryPatchComponent> Components,
        IReadOnlyList<TrustedLibraryPatchSymbol> Symbols,
        IReadOnlyList<TrustedLibraryPatchFootprint> Footprints,
        IReadOnlyList<TrustedLibraryPatchMapping> Mappings,
        IReadOnlyList<TrustedLibraryPatchDatasheetReference> Datasheets);
}

public sealed record TrustedLibraryPatchPlan(
    TrustedLibraryPatchPlanState State,
    bool MutatesTrustedLibrary,
    IReadOnlyList<TrustedLibraryPatchSummary> Patches,
    IReadOnlyList<TrustedLibraryPatchDiagnostic> BlockingDiagnostics)
{
    public bool CanCreatePatch => State == TrustedLibraryPatchPlanState.Ready && BlockingDiagnostics.Count == 0;

    public string Summary => CanCreatePatch
        ? Patches.Count == 1
            ? "Trusted library patch ready for 1 component candidate."
            : $"Trusted library patch ready for {Patches.Count} component candidates."
        : $"Trusted library patch blocked by {BlockingDiagnostics.Count} diagnostic(s).";
}

public sealed record TrustedLibraryPatchCandidate(
    string CandidateId,
    string TargetLibraryId,
    TrustedLibraryPatchApproval? Approval,
    IReadOnlyList<TrustedLibraryPatchDiagnostic> Diagnostics,
    IReadOnlyList<TrustedLibraryPatchComponent> Components,
    IReadOnlyList<TrustedLibraryPatchSymbol> Symbols,
    IReadOnlyList<TrustedLibraryPatchFootprint> Footprints,
    IReadOnlyList<TrustedLibraryPatchMapping> Mappings,
    IReadOnlyList<TrustedLibraryPatchDatasheetReference> Datasheets);

public sealed record TrustedLibraryPatchSummary(
    string CandidateId,
    string TargetLibraryId,
    IReadOnlyList<TrustedLibraryPatchComponent> Components,
    IReadOnlyList<TrustedLibraryPatchSymbol> Symbols,
    IReadOnlyList<TrustedLibraryPatchFootprint> Footprints,
    IReadOnlyList<TrustedLibraryPatchMapping> Mappings,
    IReadOnlyList<TrustedLibraryPatchDatasheetReference> Datasheets,
    TrustedLibraryPatchApproval Approval,
    IReadOnlyList<TrustedLibraryPatchDiagnostic> Diagnostics,
    IReadOnlyList<TrustedLibraryPatchContentHash> ContentHashes,
    string PatchHash);

public sealed record TrustedLibraryPatchComponent(
    string Name,
    string Manufacturer,
    string ManufacturerPartNumber,
    string Description);

public sealed record TrustedLibraryPatchSymbol(
    string Name,
    IReadOnlyList<TrustedLibraryPatchSymbolPin> Pins);

public sealed record TrustedLibraryPatchSymbolPin(string Name, string Label);

public sealed record TrustedLibraryPatchFootprint(
    string Name,
    IReadOnlyList<TrustedLibraryPatchFootprintPad> Pads);

public sealed record TrustedLibraryPatchFootprintPad(string Name, string Label);

public sealed record TrustedLibraryPatchMapping(string SymbolPin, string FootprintPad);

public sealed record TrustedLibraryPatchDatasheetReference(
    string ReferenceId,
    string Title,
    string Location);

public sealed record TrustedLibraryPatchApproval(
    string Reviewer,
    string Note,
    DateTimeOffset ApprovedAtUtc);

public sealed record TrustedLibraryPatchDiagnostic(
    string Code,
    string Message,
    bool IsResolved)
{
    public static TrustedLibraryPatchDiagnostic Resolved(string code, string message) =>
        new(code, message, IsResolved: true);

    public static TrustedLibraryPatchDiagnostic Unresolved(string code, string message) =>
        new(code, message, IsResolved: false);
}

public sealed record TrustedLibraryPatchContentHash(string ContentKind, string Sha256);

public enum TrustedLibraryPatchPlanState
{
    Blocked,
    Ready,
}
