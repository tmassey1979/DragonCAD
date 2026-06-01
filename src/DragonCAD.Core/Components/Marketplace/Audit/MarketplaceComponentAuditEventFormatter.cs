using System.Globalization;

namespace DragonCAD.Core.Components.Marketplace.Audit;

public static class MarketplaceComponentAuditEventFormatter
{
    public static string Format(IReadOnlyList<MarketplaceComponentAuditEvent> auditEvents)
    {
        ArgumentNullException.ThrowIfNull(auditEvents);

        string[] lines = auditEvents
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ThenBy(auditEvent => auditEvent.ComponentKey)
            .ThenBy(auditEvent => auditEvent.Kind)
            .ThenBy(auditEvent => auditEvent.SourceVendor, StringComparer.Ordinal)
            .ThenBy(auditEvent => auditEvent.VendorSku, StringComparer.Ordinal)
            .ThenBy(auditEvent => auditEvent.Actor, StringComparer.Ordinal)
            .ThenBy(auditEvent => auditEvent.LocalOrderId, StringComparer.Ordinal)
            .Select(FormatLine)
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatLine(MarketplaceComponentAuditEvent auditEvent) =>
        auditEvent.Kind switch
        {
            MarketplaceComponentAuditEventKind.VendorImport => string.Join(
                " | ",
                Prefix(auditEvent),
                $"vendor={auditEvent.SourceVendor}",
                $"sku={auditEvent.VendorSku}",
                $"quantity={auditEvent.Quantity.ToString(CultureInfo.InvariantCulture)}"),
            MarketplaceComponentAuditEventKind.DatasheetGenerated => string.Join(
                " | ",
                Prefix(auditEvent),
                $"vendor={auditEvent.SourceVendor}",
                $"datasheet={auditEvent.DatasheetUrl}",
                $"checksum={auditEvent.DatasheetChecksum}",
                $"actor={auditEvent.Actor}"),
            MarketplaceComponentAuditEventKind.ManualOverride => string.Join(
                " | ",
                Prefix(auditEvent),
                $"actor={auditEvent.Actor}",
                $"field={auditEvent.ChangedField}",
                $"previous={auditEvent.PreviousValue}",
                $"new={auditEvent.NewValue}"),
            MarketplaceComponentAuditEventKind.LocalOrderRecord => string.Join(
                " | ",
                Prefix(auditEvent),
                $"vendor={auditEvent.SourceVendor}",
                $"sku={auditEvent.VendorSku}",
                $"order={auditEvent.LocalOrderId}",
                $"quantity={auditEvent.Quantity.ToString(CultureInfo.InvariantCulture)}",
                $"unitPrice={auditEvent.UnitPrice.ToString("0.#############################", CultureInfo.InvariantCulture)}"),
            _ => throw new ArgumentOutOfRangeException(nameof(auditEvent), auditEvent.Kind, "Unknown audit event kind."),
        };

    private static string Prefix(MarketplaceComponentAuditEvent auditEvent) =>
        string.Join(
            " | ",
            auditEvent.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
            auditEvent.ComponentKey.Value,
            auditEvent.Kind.ToString());
}
