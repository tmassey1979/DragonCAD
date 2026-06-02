using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Drafts;

namespace DragonCAD.Core.Components.Promotion;

public static class LibraryPromotionBlockerCodes
{
    public const string InvalidDraft = "InvalidDraft";
    public const string MissingDecisionId = "MissingDecisionId";
    public const string MissingReviewer = "MissingReviewer";
    public const string MissingSourceProvenance = "MissingSourceProvenance";
    public const string MissingTargetLibrary = "MissingTargetLibrary";
}

public sealed record LibraryPromotionRequest(
    ComponentDraft Draft,
    string TargetLibraryId,
    string Reviewer,
    string DecisionId,
    string SourceProvenanceId,
    string TrustedLibraryPath);

public sealed record LibraryPromotionBlocker(string Code, string Message);

public sealed record LibraryPromotionPreview(
    bool IsBlocked,
    bool MutatesLibrary,
    string PatchPreviewJson,
    IReadOnlyList<LibraryPromotionBlocker> Blockers);

public sealed class LibraryPromotionPlanner
{
    private const string PreviewSchema = "dragoncad.trustedLibraryPromotionPreview.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LibraryPromotionPreview Plan(LibraryPromotionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Draft);

        LibraryPromotionBlocker[] blockers = CreateBlockers(request)
            .OrderBy(blocker => blocker.Code, StringComparer.Ordinal)
            .ThenBy(blocker => blocker.Message, StringComparer.Ordinal)
            .ToArray();

        bool isBlocked = blockers.Length > 0;
        PromotionPatchPreviewDto dto = new(
            PreviewSchema,
            isBlocked ? "Blocked" : "Ready",
            MutatesLibrary: false,
            CreateDecisionDto(request),
            CreateComponentDto(request.Draft),
            isBlocked ? null : [CreateOperationDto(request)],
            blockers);

        string json = JsonSerializer.Serialize(dto, JsonOptions).ReplaceLineEndings("\n");

        return new LibraryPromotionPreview(
            isBlocked,
            MutatesLibrary: false,
            json,
            blockers);
    }

    private static IEnumerable<LibraryPromotionBlocker> CreateBlockers(LibraryPromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer))
        {
            yield return new LibraryPromotionBlocker(
                LibraryPromotionBlockerCodes.MissingReviewer,
                "Promotion preview requires a reviewer.");
        }

        if (string.IsNullOrWhiteSpace(request.DecisionId))
        {
            yield return new LibraryPromotionBlocker(
                LibraryPromotionBlockerCodes.MissingDecisionId,
                "Promotion preview requires a review decision id.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceProvenanceId))
        {
            yield return new LibraryPromotionBlocker(
                LibraryPromotionBlockerCodes.MissingSourceProvenance,
                "Promotion preview requires source provenance.");
        }

        if (string.IsNullOrWhiteSpace(request.TargetLibraryId))
        {
            yield return new LibraryPromotionBlocker(
                LibraryPromotionBlockerCodes.MissingTargetLibrary,
                "Promotion preview requires a target trusted library id.");
        }

        foreach (ComponentDraftDiagnostic diagnostic in ComponentDraftValidator.Validate(request.Draft).Diagnostics
            .OrderBy(diagnostic => diagnostic.Code)
            .ThenBy(diagnostic => diagnostic.Subject, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal))
        {
            yield return new LibraryPromotionBlocker(
                LibraryPromotionBlockerCodes.InvalidDraft,
                diagnostic.Message);
        }
    }

    private static PromotionDecisionDto CreateDecisionDto(LibraryPromotionRequest request) =>
        new(
            NormalizeOptional(request.DecisionId),
            NormalizeOptional(request.Reviewer),
            NormalizeOptional(request.SourceProvenanceId),
            NormalizeOptional(request.TargetLibraryId),
            NormalizeOptional(request.TrustedLibraryPath));

    private static PromotionComponentDto CreateComponentDto(ComponentDraft draft) =>
        new(
            draft.Id.Value,
            draft.DisplayName,
            draft.Package.ReferencePrefix,
            draft.Attributes
                .OrderBy(attribute => attribute.Name, StringComparer.Ordinal)
                .ThenBy(attribute => attribute.Value, StringComparer.Ordinal)
                .Select(attribute => new PromotionAttributeDto(attribute.Name, attribute.Value))
                .ToArray(),
            draft.Pins
                .OrderBy(pin => pin.Id.Value, StringComparer.Ordinal)
                .Select(pin => new PromotionPinDto(pin.Id.Value, pin.Name, pin.Number, pin.ElectricalType))
                .ToArray(),
            draft.Symbols
                .OrderBy(symbol => symbol.Id.Value, StringComparer.Ordinal)
                .Select(symbol => new PromotionSymbolDto(
                    symbol.Id.Value,
                    symbol.Name,
                    symbol.Pins
                        .Select(pin => pin.PinId.Value)
                        .Order(StringComparer.Ordinal)
                        .ToArray()))
                .ToArray(),
            draft.Footprints
                .OrderBy(footprint => footprint.Id.Value, StringComparer.Ordinal)
                .Select(footprint => new PromotionFootprintDto(
                    footprint.Id.Value,
                    footprint.Name,
                    footprint.Pads
                        .Select(pad => pad.Id.Value)
                        .Order(StringComparer.Ordinal)
                        .ToArray()))
                .ToArray(),
            draft.DeviceMappings
                .OrderBy(mapping => mapping.PinId.Value, StringComparer.Ordinal)
                .ThenBy(mapping => mapping.FootprintId.Value, StringComparer.Ordinal)
                .ThenBy(mapping => mapping.PadId.Value, StringComparer.Ordinal)
                .Select(mapping => new PromotionDeviceMappingDto(
                    mapping.PinId.Value,
                    mapping.FootprintId.Value,
                    mapping.PadId.Value))
                .ToArray());

    private static PromotionOperationDto CreateOperationDto(LibraryPromotionRequest request) =>
        new(
            "upsertComponentDraft",
            $"/libraries/{NormalizeOptional(request.TargetLibraryId)}/components/{request.Draft.Id.Value}",
            request.Draft.Id.Value);

    private static string NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private sealed record PromotionPatchPreviewDto(
        string Schema,
        string Status,
        bool MutatesLibrary,
        PromotionDecisionDto Decision,
        PromotionComponentDto Component,
        IReadOnlyList<PromotionOperationDto>? Operations,
        IReadOnlyList<LibraryPromotionBlocker> Blockers);

    private sealed record PromotionDecisionDto(
        string DecisionId,
        string Reviewer,
        string SourceProvenanceId,
        string TargetLibraryId,
        string TrustedLibraryPath);

    private sealed record PromotionComponentDto(
        string Id,
        string DisplayName,
        string ReferencePrefix,
        IReadOnlyList<PromotionAttributeDto> Attributes,
        IReadOnlyList<PromotionPinDto> Pins,
        IReadOnlyList<PromotionSymbolDto> Symbols,
        IReadOnlyList<PromotionFootprintDto> Footprints,
        IReadOnlyList<PromotionDeviceMappingDto> DeviceMappings);

    private sealed record PromotionAttributeDto(string Name, string Value);

    private sealed record PromotionPinDto(
        string Id,
        string Name,
        string Number,
        ComponentDraftPinElectricalType ElectricalType);

    private sealed record PromotionSymbolDto(
        string Id,
        string Name,
        IReadOnlyList<string> PinIds);

    private sealed record PromotionFootprintDto(
        string Id,
        string Name,
        IReadOnlyList<string> PadIds);

    private sealed record PromotionDeviceMappingDto(
        string PinId,
        string FootprintId,
        string PadId);

    private sealed record PromotionOperationDto(
        string Op,
        string Path,
        string Source);
}
