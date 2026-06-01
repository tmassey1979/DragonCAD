using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Audit;

namespace DragonCAD.Core.Tests.Components.Marketplace.Audit;

public sealed class MarketplaceComponentAuditEventFormatterTests
{
    [Fact]
    public void VendorImportEventCapturesVendorIdentityAndImportedCount()
    {
        DateTimeOffset occurredAt = new(2026, 5, 31, 18, 30, 0, TimeSpan.Zero);

        MarketplaceComponentAuditEvent auditEvent = MarketplaceComponentAuditEvent.VendorImport(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            sourceVendor: " Digi-Key ",
            vendorSku: " 296-13996-5-ND ",
            importedCount: 4,
            occurredAt);

        Assert.Equal(MarketplaceComponentAuditEventKind.VendorImport, auditEvent.Kind);
        Assert.Equal(CanonicalComponentKey.FromPartNumber("LM7805CT"), auditEvent.ComponentKey);
        Assert.Equal("Digi-Key", auditEvent.SourceVendor);
        Assert.Equal("296-13996-5-ND", auditEvent.VendorSku);
        Assert.Equal(4, auditEvent.Quantity);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
    }

    [Fact]
    public void DatasheetGeneratedEventCapturesGeneratorAndChecksum()
    {
        DateTimeOffset occurredAt = new(2026, 5, 31, 18, 35, 0, TimeSpan.Zero);

        MarketplaceComponentAuditEvent auditEvent = MarketplaceComponentAuditEvent.DatasheetGenerated(
            CanonicalComponentKey.FromPartNumber("NE555P"),
            sourceVendor: "Mouser",
            datasheetUrl: " https://example.invalid/ne555p.pdf ",
            datasheetChecksum: " sha256:abcdef0123456789 ",
            generatorName: " Codex ",
            occurredAt);

        Assert.Equal(MarketplaceComponentAuditEventKind.DatasheetGenerated, auditEvent.Kind);
        Assert.Equal("Mouser", auditEvent.SourceVendor);
        Assert.Equal("https://example.invalid/ne555p.pdf", auditEvent.DatasheetUrl);
        Assert.Equal("sha256:abcdef0123456789", auditEvent.DatasheetChecksum);
        Assert.Equal("Codex", auditEvent.Actor);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
    }

    [Fact]
    public void ManualOverrideEventCapturesReviewerAndChangedField()
    {
        DateTimeOffset occurredAt = new(2026, 5, 31, 18, 40, 0, TimeSpan.Zero);

        MarketplaceComponentAuditEvent auditEvent = MarketplaceComponentAuditEvent.ManualOverride(
            CanonicalComponentKey.FromPartNumber("ESP32-DEVKITC"),
            reviewer: " tmassey ",
            changedField: " DefaultPackage ",
            previousValue: " DevKit ",
            newValue: " DevKitC ",
            occurredAt);

        Assert.Equal(MarketplaceComponentAuditEventKind.ManualOverride, auditEvent.Kind);
        Assert.Equal("tmassey", auditEvent.Actor);
        Assert.Equal("DefaultPackage", auditEvent.ChangedField);
        Assert.Equal("DevKit", auditEvent.PreviousValue);
        Assert.Equal("DevKitC", auditEvent.NewValue);
    }

    [Fact]
    public void LocalOrderRecordEventCapturesOrderIdentityAndQuantity()
    {
        MarketplaceComponentAuditEvent auditEvent = MarketplaceComponentAuditEvent.LocalOrderRecord(
            CanonicalComponentKey.FromPartNumber("RC0603FR-0710KL"),
            sourceVendor: "Digi-Key",
            vendorSku: "311-10.0KHRCT-ND",
            localOrderId: "PO-2026-00042",
            quantity: 125,
            unitPrice: 0.0125m,
            occurredAt: new DateTimeOffset(2026, 5, 31, 18, 45, 0, TimeSpan.Zero));

        Assert.Equal(MarketplaceComponentAuditEventKind.LocalOrderRecord, auditEvent.Kind);
        Assert.Equal("Digi-Key", auditEvent.SourceVendor);
        Assert.Equal("311-10.0KHRCT-ND", auditEvent.VendorSku);
        Assert.Equal("PO-2026-00042", auditEvent.LocalOrderId);
        Assert.Equal(125, auditEvent.Quantity);
        Assert.Equal(0.0125m, auditEvent.UnitPrice);
    }

    [Fact]
    public void FormatterProducesDeterministicLinesRegardlessOfInputOrder()
    {
        MarketplaceComponentAuditEvent laterImport = MarketplaceComponentAuditEvent.VendorImport(
            CanonicalComponentKey.FromPartNumber("NE555P"),
            sourceVendor: "Mouser",
            vendorSku: "595-NE555P",
            importedCount: 1,
            occurredAt: new DateTimeOffset(2026, 5, 31, 19, 0, 0, TimeSpan.Zero));
        MarketplaceComponentAuditEvent order = MarketplaceComponentAuditEvent.LocalOrderRecord(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            sourceVendor: "Digi-Key",
            vendorSku: "296-13996-5-ND",
            localOrderId: "PO-2026-00042",
            quantity: 25,
            unitPrice: 0.48m,
            occurredAt: new DateTimeOffset(2026, 5, 31, 18, 55, 0, TimeSpan.Zero));
        MarketplaceComponentAuditEvent datasheet = MarketplaceComponentAuditEvent.DatasheetGenerated(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            sourceVendor: "Digi-Key",
            datasheetUrl: "https://example.invalid/lm7805ct.pdf",
            datasheetChecksum: "sha256:0123456789abcdef",
            generatorName: "Codex",
            occurredAt: new DateTimeOffset(2026, 5, 31, 18, 50, 0, TimeSpan.Zero));
        MarketplaceComponentAuditEvent manual = MarketplaceComponentAuditEvent.ManualOverride(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            reviewer: "tmassey",
            changedField: "ReviewState",
            previousValue: "PendingReview",
            newValue: "Approved",
            occurredAt: new DateTimeOffset(2026, 5, 31, 18, 52, 0, TimeSpan.Zero));

        string formatted = MarketplaceComponentAuditEventFormatter.Format([laterImport, order, datasheet, manual]);

        Assert.Equal(
            string.Join(
                Environment.NewLine,
                [
                    "2026-05-31T18:50:00.0000000+00:00 | PART:LM7805CT | DatasheetGenerated | vendor=Digi-Key | datasheet=https://example.invalid/lm7805ct.pdf | checksum=sha256:0123456789abcdef | actor=Codex",
                    "2026-05-31T18:52:00.0000000+00:00 | PART:LM7805CT | ManualOverride | actor=tmassey | field=ReviewState | previous=PendingReview | new=Approved",
                    "2026-05-31T18:55:00.0000000+00:00 | PART:LM7805CT | LocalOrderRecord | vendor=Digi-Key | sku=296-13996-5-ND | order=PO-2026-00042 | quantity=25 | unitPrice=0.48",
                    "2026-05-31T19:00:00.0000000+00:00 | PART:NE555P | VendorImport | vendor=Mouser | sku=595-NE555P | quantity=1",
                ]),
            formatted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantityEventsRequirePositiveQuantity(int quantity)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MarketplaceComponentAuditEvent.LocalOrderRecord(
                CanonicalComponentKey.FromPartNumber("LM7805CT"),
                sourceVendor: "Digi-Key",
                vendorSku: "296-13996-5-ND",
                localOrderId: "PO-2026-00042",
                quantity,
                unitPrice: 0.48m,
                occurredAt: new DateTimeOffset(2026, 5, 31, 19, 5, 0, TimeSpan.Zero)));

        Assert.Equal("quantity", exception.ParamName);
    }
}
