namespace DragonCAD.Core.Components.Marketplace.Audit;

public enum MarketplaceComponentAuditEventKind
{
    VendorImport,
    DatasheetGenerated,
    ManualOverride,
    LocalOrderRecord,
}

public sealed record MarketplaceComponentAuditEvent
{
    private MarketplaceComponentAuditEvent(
        MarketplaceComponentAuditEventKind kind,
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string vendorSku,
        string datasheetUrl,
        string datasheetChecksum,
        string actor,
        string changedField,
        string previousValue,
        string newValue,
        string localOrderId,
        int quantity,
        decimal unitPrice,
        DateTimeOffset occurredAt)
    {
        Kind = kind;
        ComponentKey = componentKey;
        SourceVendor = CanonicalComponentKey.NormalizeOptional(sourceVendor);
        VendorSku = CanonicalComponentKey.NormalizeOptional(vendorSku);
        DatasheetUrl = CanonicalComponentKey.NormalizeOptional(datasheetUrl);
        DatasheetChecksum = CanonicalComponentKey.NormalizeOptional(datasheetChecksum);
        Actor = CanonicalComponentKey.NormalizeOptional(actor);
        ChangedField = CanonicalComponentKey.NormalizeOptional(changedField);
        PreviousValue = CanonicalComponentKey.NormalizeOptional(previousValue);
        NewValue = CanonicalComponentKey.NormalizeOptional(newValue);
        LocalOrderId = CanonicalComponentKey.NormalizeOptional(localOrderId);
        Quantity = quantity;
        UnitPrice = unitPrice;
        OccurredAt = occurredAt;
    }

    public MarketplaceComponentAuditEventKind Kind { get; }

    public CanonicalComponentKey ComponentKey { get; }

    public string SourceVendor { get; }

    public string VendorSku { get; }

    public string DatasheetUrl { get; }

    public string DatasheetChecksum { get; }

    public string Actor { get; }

    public string ChangedField { get; }

    public string PreviousValue { get; }

    public string NewValue { get; }

    public string LocalOrderId { get; }

    public int Quantity { get; }

    public decimal UnitPrice { get; }

    public DateTimeOffset OccurredAt { get; }

    public static MarketplaceComponentAuditEvent VendorImport(
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string vendorSku,
        int importedCount,
        DateTimeOffset occurredAt)
    {
        RequirePositive(importedCount, nameof(importedCount));

        return new MarketplaceComponentAuditEvent(
            MarketplaceComponentAuditEventKind.VendorImport,
            componentKey,
            Required(sourceVendor, nameof(sourceVendor)),
            Required(vendorSku, nameof(vendorSku)),
            datasheetUrl: string.Empty,
            datasheetChecksum: string.Empty,
            actor: string.Empty,
            changedField: string.Empty,
            previousValue: string.Empty,
            newValue: string.Empty,
            localOrderId: string.Empty,
            quantity: importedCount,
            unitPrice: 0m,
            occurredAt);
    }

    public static MarketplaceComponentAuditEvent DatasheetGenerated(
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string datasheetUrl,
        string datasheetChecksum,
        string generatorName,
        DateTimeOffset occurredAt) =>
        new(
            MarketplaceComponentAuditEventKind.DatasheetGenerated,
            componentKey,
            Required(sourceVendor, nameof(sourceVendor)),
            vendorSku: string.Empty,
            Required(datasheetUrl, nameof(datasheetUrl)),
            Required(datasheetChecksum, nameof(datasheetChecksum)),
            Required(generatorName, nameof(generatorName)),
            changedField: string.Empty,
            previousValue: string.Empty,
            newValue: string.Empty,
            localOrderId: string.Empty,
            quantity: 0,
            unitPrice: 0m,
            occurredAt);

    public static MarketplaceComponentAuditEvent ManualOverride(
        CanonicalComponentKey componentKey,
        string reviewer,
        string changedField,
        string previousValue,
        string newValue,
        DateTimeOffset occurredAt) =>
        new(
            MarketplaceComponentAuditEventKind.ManualOverride,
            componentKey,
            sourceVendor: string.Empty,
            vendorSku: string.Empty,
            datasheetUrl: string.Empty,
            datasheetChecksum: string.Empty,
            Required(reviewer, nameof(reviewer)),
            Required(changedField, nameof(changedField)),
            CanonicalComponentKey.NormalizeOptional(previousValue),
            CanonicalComponentKey.NormalizeOptional(newValue),
            localOrderId: string.Empty,
            quantity: 0,
            unitPrice: 0m,
            occurredAt);

    public static MarketplaceComponentAuditEvent LocalOrderRecord(
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string vendorSku,
        string localOrderId,
        int quantity,
        decimal unitPrice,
        DateTimeOffset occurredAt)
    {
        RequirePositive(quantity, nameof(quantity));
        if (unitPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        return new MarketplaceComponentAuditEvent(
            MarketplaceComponentAuditEventKind.LocalOrderRecord,
            componentKey,
            Required(sourceVendor, nameof(sourceVendor)),
            Required(vendorSku, nameof(vendorSku)),
            datasheetUrl: string.Empty,
            datasheetChecksum: string.Empty,
            actor: string.Empty,
            changedField: string.Empty,
            previousValue: string.Empty,
            newValue: string.Empty,
            Required(localOrderId, nameof(localOrderId)),
            quantity,
            unitPrice,
            occurredAt);
    }

    private static string Required(string value, string parameterName) =>
        CanonicalComponentKey.NormalizeRequired(value, parameterName);

    private static void RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Quantity must be greater than zero.");
        }
    }
}
