using DragonCAD.Sourcing.Orders;

namespace DragonCAD.Sourcing.Tests.Orders;

public sealed class VendorCartHandoffPlannerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 3, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PlanBuildsReviewableDirectCartRecordForCartCapableProvider()
    {
        var plan = VendorCartHandoffPlanner.Plan(
            [Line("bom-1", "offer-1", quantity: 12)],
            [Offer("offer-1", "digikey", "296-6501-1-ND", Money.Usd(0.42m), CreatedAt.AddHours(-1))],
            [Provider("digikey", "Digi-Key", VendorCartSupportMode.DirectCart, hasCredentials: true)],
            CreatedAt,
            TimeSpan.FromDays(7));

        Assert.False(plan.HasBlockingDiagnostics);
        var record = Assert.Single(plan.Records);
        Assert.Equal("296-6501-1-ND", record.VendorPartNumber);
        Assert.Equal(12, record.Quantity);
        Assert.Equal("offer-1", record.SourceOfferId);
        Assert.Equal(Money.Usd(0.42m), record.UnitPriceSnapshot);
        Assert.Equal(Money.Usd(5.04m), record.ExtendedPriceSnapshot);
        Assert.Equal(CreatedAt.AddHours(-1), record.PriceSnapshotAt);
        Assert.Equal(CreatedAt, record.CreatedAt);
        Assert.Equal(VendorCartSupportMode.DirectCart, record.SupportMode);
        Assert.Equal(VendorCartReviewStatus.PendingUserReview, record.ReviewStatus);
    }

    [Fact]
    public void PlanKeepsCsvOnlyProvidersAsCsvUploadHandoffs()
    {
        var plan = VendorCartHandoffPlanner.Plan(
            [Line("bom-2", "offer-2", quantity: 5)],
            [Offer("offer-2", "mouser", "595-TLV9002IDR", Money.Usd(0.31m), CreatedAt)],
            [Provider("mouser", "Mouser", VendorCartSupportMode.CsvUpload, hasCredentials: false)],
            CreatedAt,
            TimeSpan.FromDays(7));

        var record = Assert.Single(plan.Records);
        Assert.Empty(plan.Diagnostics);
        Assert.Equal(VendorCartSupportMode.CsvUpload, record.SupportMode);
        Assert.Equal("595-TLV9002IDR", record.VendorPartNumber);
    }

    [Fact]
    public void PlanKeepsManualProvidersAsCopyPasteHandoffs()
    {
        var plan = VendorCartHandoffPlanner.Plan(
            [Line("bom-3", "offer-3", quantity: 2)],
            [Offer("offer-3", "jameco", "2125258", Money.Usd(1.15m), CreatedAt)],
            [Provider("jameco", "Jameco", VendorCartSupportMode.CopyPaste, hasCredentials: false)],
            CreatedAt,
            TimeSpan.FromDays(7));

        var record = Assert.Single(plan.Records);
        Assert.Empty(plan.Diagnostics);
        Assert.Equal(VendorCartSupportMode.CopyPaste, record.SupportMode);
        Assert.Equal(2, record.Quantity);
    }

    [Fact]
    public void PlanReportsStaleQuotesForUserVisibleRefreshDiagnostics()
    {
        var plan = VendorCartHandoffPlanner.Plan(
            [Line("bom-4", "offer-4", quantity: 1)],
            [Offer("offer-4", "digikey", "497-1234-1-ND", Money.Usd(2.10m), CreatedAt.AddDays(-8))],
            [Provider("digikey", "Digi-Key", VendorCartSupportMode.DirectCart, hasCredentials: true)],
            CreatedAt,
            TimeSpan.FromDays(7));

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal(VendorCartDiagnosticCode.StaleQuote, diagnostic.Code);
        Assert.Contains("should be refreshed", diagnostic.Message);
        Assert.Single(plan.Records);
    }

    [Fact]
    public void PlanReportsMissingOffersAndMissingVendorPartNumbers()
    {
        var plan = VendorCartHandoffPlanner.Plan(
            [
                Line("bom-5", "missing-offer", quantity: 3),
                Line("bom-6", "offer-6", quantity: 3),
            ],
            [Offer("offer-6", "digikey", vendorPartNumber: null, Money.Usd(0.25m), CreatedAt)],
            [Provider("digikey", "Digi-Key", VendorCartSupportMode.DirectCart, hasCredentials: false)],
            CreatedAt,
            TimeSpan.FromDays(7));

        Assert.Empty(plan.Records);
        Assert.Equal(
            [
                VendorCartDiagnosticCode.MissingOffer,
                VendorCartDiagnosticCode.MissingCredentials,
                VendorCartDiagnosticCode.MissingVendorPartNumber,
            ],
            plan.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(plan.Diagnostics, diagnostic => Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message)));
    }

    private static VendorCartLine Line(string bomLineId, string offerId, int quantity)
    {
        return new VendorCartLine(bomLineId, manufacturerPartNumber: "NE555DR", quantity, offerId);
    }

    private static VendorCartOffer Offer(
        string offerId,
        string providerId,
        string? vendorPartNumber,
        Money unitPrice,
        DateTimeOffset quotedAt)
    {
        return new VendorCartOffer(offerId, providerId, vendorPartNumber, unitPrice, quotedAt);
    }

    private static VendorCartProvider Provider(
        string providerId,
        string displayName,
        VendorCartSupportMode mode,
        bool hasCredentials)
    {
        return new VendorCartProvider(providerId, displayName, mode, hasCredentials);
    }
}
