using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.Core.Components.Promotion.Persistence;

public sealed class TrustedLibraryPromotionStore
{
    private const string ConflictKeyPrefix = "VerifiedGeometry:";
    private readonly string path;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public TrustedLibraryPromotionStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.path = path;
    }

    public TrustedLibraryPromotionLibrary Load()
    {
        if (!File.Exists(path))
        {
            return TrustedLibraryPromotionLibrary.Empty;
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return TrustedLibraryPromotionLibrary.Empty;
        }

        return JsonSerializer.Deserialize<TrustedLibraryPromotionLibrary>(json, JsonOptions)
            ?? TrustedLibraryPromotionLibrary.Empty;
    }

    public TrustedLibraryPromotionResult Apply(TrustedLibraryPromotionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Draft);
        ArgumentNullException.ThrowIfNull(request.SourceProvenance);

        TrustedLibraryPromotionLibrary library = Load();
        TrustedLibraryPromotionResult result = request.Action switch
        {
            TrustedLibraryPromotionAction.PromoteNew => PromoteNew(request, library),
            TrustedLibraryPromotionAction.LinkExisting => LinkExisting(request, library),
            TrustedLibraryPromotionAction.Reject => Reject(request, library),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unknown promotion action."),
        };

        return result;
    }

    private TrustedLibraryPromotionResult PromoteNew(
        TrustedLibraryPromotionRequest request,
        TrustedLibraryPromotionLibrary library)
    {
        TrustedLibraryPromotionRecord record = CreateRecord(request, rollbackActions: [$"remove:{request.Draft.Id.Value}"]);
        TrustedLibraryPromotionAuditRecord audit = CreateAudit(TrustedLibraryPromotionAuditKind.PromotedNew, request, record);
        Save(WithRecordAndAudit(library, record, audit));
        return new TrustedLibraryPromotionResult(TrustedLibraryPromotionStatus.Promoted, []);
    }

    private TrustedLibraryPromotionResult LinkExisting(
        TrustedLibraryPromotionRequest request,
        TrustedLibraryPromotionLibrary library)
    {
        string existingComponentId = NormalizeRequired(request.ExistingComponentId, nameof(request.ExistingComponentId));
        TrustedLibraryPromotionRecord? existing = library.Records
            .FirstOrDefault(record => string.Equals(record.ComponentId, existingComponentId, StringComparison.Ordinal));

        if (existing is null)
        {
            return Blocked(
                TrustedLibraryPromotionConflictKind.VerifiedGeometry,
                $"{ConflictKeyPrefix}{existingComponentId}",
                existingComponentId,
                $"Trusted component '{existingComponentId}' does not exist.");
        }

        string incomingFingerprint = CreateVerifiedGeometryFingerprint(request.Draft);
        if (!string.Equals(existing.VerifiedGeometryFingerprint, incomingFingerprint, StringComparison.Ordinal))
        {
            string conflictKey = $"{ConflictKeyPrefix}{existing.ComponentId}";
            if (!request.ConflictDecisions.TryGetValue(conflictKey, out TrustedLibraryPromotionConflictDecision decision) ||
                decision != TrustedLibraryPromotionConflictDecision.KeepExistingVerifiedGeometry)
            {
                return Blocked(
                    TrustedLibraryPromotionConflictKind.VerifiedGeometry,
                    conflictKey,
                    existing.ComponentId,
                    $"Trusted component '{existing.ComponentId}' already has verified geometry; an explicit decision is required before linking conflicting geometry.");
            }
        }

        TrustedLibraryPromotionRecord linked = existing with
        {
            SourceProvenanceId = NormalizeRequired(request.SourceProvenanceId, nameof(request.SourceProvenanceId)),
            Provenance = CreateProvenance(request.SourceProvenance),
            Reviewer = NormalizeRequired(request.Reviewer, nameof(request.Reviewer)),
            ReviewedAt = request.ReviewedAt,
            LastDecisionId = NormalizeRequired(request.DecisionId, nameof(request.DecisionId)),
            RollbackActions = existing.RollbackActions
                .Append($"unlink:{existing.ComponentId}:{NormalizeRequired(request.DecisionId, nameof(request.DecisionId))}")
                .ToArray(),
        };

        TrustedLibraryPromotionAuditRecord audit = CreateAudit(TrustedLibraryPromotionAuditKind.LinkedExisting, request, linked);
        Save(WithRecordAndAudit(library, linked, audit));
        return new TrustedLibraryPromotionResult(TrustedLibraryPromotionStatus.LinkedExisting, []);
    }

    private TrustedLibraryPromotionResult Reject(
        TrustedLibraryPromotionRequest request,
        TrustedLibraryPromotionLibrary library)
    {
        TrustedLibraryPromotionAuditRecord audit = CreateAudit(TrustedLibraryPromotionAuditKind.Rejected, request, recordSnapshot: null);
        Save(new TrustedLibraryPromotionLibrary(SortRecords(library.Records), SortAudit(library.AuditRecords.Append(audit))));
        return new TrustedLibraryPromotionResult(TrustedLibraryPromotionStatus.Rejected, []);
    }

    private void Save(TrustedLibraryPromotionLibrary library)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            new TrustedLibraryPromotionLibrary(SortRecords(library.Records), SortAudit(library.AuditRecords)),
            JsonOptions).ReplaceLineEndings("\n");
        string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        File.Move(tempPath, path, overwrite: true);
    }

    private static TrustedLibraryPromotionLibrary WithRecordAndAudit(
        TrustedLibraryPromotionLibrary library,
        TrustedLibraryPromotionRecord record,
        TrustedLibraryPromotionAuditRecord audit)
    {
        TrustedLibraryPromotionRecord[] records = library.Records
            .Where(existing => !string.Equals(existing.ComponentId, record.ComponentId, StringComparison.Ordinal))
            .Append(record)
            .ToArray();

        return new TrustedLibraryPromotionLibrary(SortRecords(records), SortAudit(library.AuditRecords.Append(audit)));
    }

    private static TrustedLibraryPromotionRecord CreateRecord(
        TrustedLibraryPromotionRequest request,
        IReadOnlyList<string> rollbackActions) =>
        new(
            request.Draft.Id.Value,
            NormalizeRequired(request.Draft.DisplayName, nameof(request.Draft.DisplayName)),
            NormalizeRequired(request.TargetLibraryId, nameof(request.TargetLibraryId)),
            NormalizeRequired(request.SourceProvenanceId, nameof(request.SourceProvenanceId)),
            CreateProvenance(request.SourceProvenance),
            NormalizeRequired(request.Reviewer, nameof(request.Reviewer)),
            request.ReviewedAt,
            NormalizeRequired(request.DecisionId, nameof(request.DecisionId)),
            CreatePackage(request.Draft),
            CreateVerifiedGeometryFingerprint(request.Draft),
            TrustedLibraryPromotionRecordState.Trusted,
            rollbackActions.ToArray());

    private static TrustedLibraryPromotionPackage CreatePackage(ComponentDraft draft) =>
        new(
            NormalizeRequired(draft.Package.Name, nameof(draft.Package.Name)),
            NormalizeRequired(draft.Package.ReferencePrefix, nameof(draft.Package.ReferencePrefix)),
            draft.DeviceMappings
                .OrderBy(mapping => mapping.PinId.Value, StringComparer.Ordinal)
                .ThenBy(mapping => mapping.FootprintId.Value, StringComparer.Ordinal)
                .ThenBy(mapping => mapping.PadId.Value, StringComparer.Ordinal)
                .Select(mapping => $"{mapping.PinId.Value}->{mapping.FootprintId.Value}:{mapping.PadId.Value}")
                .ToArray());

    private static TrustedLibraryPromotionProvenance CreateProvenance(MarketplaceComponentProvenance provenance) =>
        new(
            provenance.Kind.ToString(),
            provenance.SourceVendor,
            provenance.ProductUrl,
            provenance.DatasheetUrl,
            provenance.DatasheetChecksum,
            provenance.GeneratorName,
            provenance.ReviewState.ToString(),
            provenance.Timestamp);

    private static TrustedLibraryPromotionAuditRecord CreateAudit(
        TrustedLibraryPromotionAuditKind kind,
        TrustedLibraryPromotionRequest request,
        TrustedLibraryPromotionRecord? recordSnapshot) =>
        new(
            kind,
            request.Draft.Id.Value,
            NormalizeRequired(request.TargetLibraryId, nameof(request.TargetLibraryId)),
            NormalizeRequired(request.SourceProvenanceId, nameof(request.SourceProvenanceId)),
            NormalizeRequired(request.Reviewer, nameof(request.Reviewer)),
            NormalizeRequired(request.DecisionId, nameof(request.DecisionId)),
            request.ReviewedAt,
            request.SourceProvenance.ReviewState.ToString(),
            recordSnapshot);

    private static TrustedLibraryPromotionResult Blocked(
        TrustedLibraryPromotionConflictKind kind,
        string key,
        string componentId,
        string message) =>
        new(TrustedLibraryPromotionStatus.Blocked, [new TrustedLibraryPromotionConflict(kind, key, componentId, message)]);

    private static IReadOnlyList<TrustedLibraryPromotionRecord> SortRecords(IEnumerable<TrustedLibraryPromotionRecord> records) =>
        records
            .OrderBy(record => record.TargetLibraryId, StringComparer.Ordinal)
            .ThenBy(record => record.ComponentId, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<TrustedLibraryPromotionAuditRecord> SortAudit(IEnumerable<TrustedLibraryPromotionAuditRecord> auditRecords) =>
        auditRecords
            .OrderBy(audit => audit.ReviewedAt)
            .ThenBy(audit => audit.ComponentId, StringComparer.Ordinal)
            .ThenBy(audit => audit.DecisionId, StringComparer.Ordinal)
            .ThenBy(audit => audit.Kind)
            .ToArray();

    private static string CreateVerifiedGeometryFingerprint(ComponentDraft draft)
    {
        var geometry = new
        {
            footprints = draft.Footprints
                .OrderBy(footprint => footprint.Id.Value, StringComparer.Ordinal)
                .Select(footprint => new
                {
                    id = footprint.Id.Value,
                    name = footprint.Name,
                    pads = footprint.Pads
                        .OrderBy(pad => pad.Id.Value, StringComparer.Ordinal)
                        .Select(pad => new
                        {
                            id = pad.Id.Value,
                            pad.Name,
                            pad.Position,
                            pad.Size,
                            pad.Technology,
                            pad.Shape,
                            pad.DrillSize,
                        })
                        .ToArray(),
                    silkscreen = footprint.Silkscreen
                        .OrderBy(primitive => primitive.Kind)
                        .ThenBy(primitive => primitive.Start.X)
                        .ThenBy(primitive => primitive.Start.Y)
                        .ThenBy(primitive => primitive.End.X)
                        .ThenBy(primitive => primitive.End.Y)
                        .ToArray(),
                    courtyard = footprint.Courtyard
                        .OrderBy(primitive => primitive.Kind)
                        .ThenBy(primitive => primitive.Start.X)
                        .ThenBy(primitive => primitive.Start.Y)
                        .ThenBy(primitive => primitive.End.X)
                        .ThenBy(primitive => primitive.End.Y)
                        .ToArray(),
                })
                .ToArray(),
        };

        string json = JsonSerializer.Serialize(geometry, JsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
