namespace DragonCAD.Core.Components.Promotion.Persistence;

public static class TrustedLibraryPromotionAuditReplay
{
    public static TrustedLibraryPromotionLibrary Replay(IReadOnlyList<TrustedLibraryPromotionAuditRecord> auditRecords)
    {
        ArgumentNullException.ThrowIfNull(auditRecords);

        Dictionary<string, TrustedLibraryPromotionRecord> records = new(StringComparer.Ordinal);
        TrustedLibraryPromotionAuditRecord[] orderedAudit = auditRecords
            .OrderBy(audit => audit.ReviewedAt)
            .ThenBy(audit => audit.ComponentId, StringComparer.Ordinal)
            .ThenBy(audit => audit.DecisionId, StringComparer.Ordinal)
            .ThenBy(audit => audit.Kind)
            .ToArray();

        foreach (TrustedLibraryPromotionAuditRecord audit in orderedAudit)
        {
            if (audit.Kind is TrustedLibraryPromotionAuditKind.PromotedNew or TrustedLibraryPromotionAuditKind.LinkedExisting &&
                audit.RecordSnapshot is not null)
            {
                records[audit.RecordSnapshot.ComponentId] = audit.RecordSnapshot;
            }
        }

        return new TrustedLibraryPromotionLibrary(
            records.Values
                .OrderBy(record => record.TargetLibraryId, StringComparer.Ordinal)
                .ThenBy(record => record.ComponentId, StringComparer.Ordinal)
                .ToArray(),
            orderedAudit);
    }
}
