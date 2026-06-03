namespace DragonCAD.Sourcing.Orders;

public static class VendorCartHandoffPlanner
{
    public static VendorCartHandoffPlan Plan(
        IEnumerable<VendorCartLine> lines,
        IEnumerable<VendorCartOffer> offers,
        IEnumerable<VendorCartProvider> providers,
        DateTimeOffset createdAt,
        TimeSpan quoteFreshnessWindow)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(providers);

        if (quoteFreshnessWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quoteFreshnessWindow),
                quoteFreshnessWindow,
                "Quote freshness window must be greater than zero.");
        }

        var offersById = offers.ToDictionary(offer => Normalize(offer.OfferId), StringComparer.OrdinalIgnoreCase);
        var providersById = providers.ToDictionary(provider => Normalize(provider.ProviderId), StringComparer.OrdinalIgnoreCase);
        var records = new List<VendorCartHandoffRecord>();
        var diagnostics = new List<VendorCartDiagnostic>();

        foreach (var line in lines.OrderBy(line => line.BomLineId, StringComparer.OrdinalIgnoreCase))
        {
            if (!offersById.TryGetValue(Normalize(line.SourceOfferId), out var offer))
            {
                diagnostics.Add(new VendorCartDiagnostic(
                    VendorCartDiagnosticCode.MissingOffer,
                    line.BomLineId,
                    line.SourceOfferId,
                    $"No source offer was found for BOM line {line.BomLineId}."));
                continue;
            }

            if (!providersById.TryGetValue(Normalize(offer.ProviderId), out var provider))
            {
                diagnostics.Add(new VendorCartDiagnostic(
                    VendorCartDiagnosticCode.MissingOffer,
                    line.BomLineId,
                    offer.OfferId,
                    $"No provider declaration was found for offer {offer.OfferId}."));
                continue;
            }

            AddDiagnostics(line, offer, provider, createdAt, quoteFreshnessWindow, diagnostics);

            if (provider.SupportMode == VendorCartSupportMode.Unsupported || string.IsNullOrWhiteSpace(offer.VendorPartNumber))
            {
                continue;
            }

            records.Add(new VendorCartHandoffRecord(
                line.BomLineId,
                provider.ProviderId,
                provider.SupportMode,
                offer.VendorPartNumber,
                line.Quantity,
                offer.OfferId,
                offer.UnitPrice,
                new Money(offer.UnitPrice.Amount * line.Quantity, offer.UnitPrice.CurrencyCode),
                offer.QuotedAt,
                createdAt,
                VendorCartReviewStatus.PendingUserReview));
        }

        return new VendorCartHandoffPlan(
            createdAt,
            records
                .OrderBy(record => record.ProviderId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            diagnostics
                .OrderBy(diagnostic => diagnostic.BomLineId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(diagnostic => diagnostic.Code)
                .ToArray());
    }

    private static void AddDiagnostics(
        VendorCartLine line,
        VendorCartOffer offer,
        VendorCartProvider provider,
        DateTimeOffset createdAt,
        TimeSpan quoteFreshnessWindow,
        ICollection<VendorCartDiagnostic> diagnostics)
    {
        if (!provider.HasCredentials && provider.SupportMode == VendorCartSupportMode.DirectCart)
        {
            diagnostics.Add(new VendorCartDiagnostic(
                VendorCartDiagnosticCode.MissingCredentials,
                line.BomLineId,
                offer.OfferId,
                $"{provider.DisplayName} needs credentials before DragonCAD can build a direct cart handoff."));
        }

        if (provider.SupportMode == VendorCartSupportMode.Unsupported)
        {
            diagnostics.Add(new VendorCartDiagnostic(
                VendorCartDiagnosticCode.UnsupportedProvider,
                line.BomLineId,
                offer.OfferId,
                $"{provider.DisplayName} does not support direct carts, CSV upload, or copy/paste handoff."));
        }

        if (string.IsNullOrWhiteSpace(offer.VendorPartNumber))
        {
            diagnostics.Add(new VendorCartDiagnostic(
                VendorCartDiagnosticCode.MissingVendorPartNumber,
                line.BomLineId,
                offer.OfferId,
                $"Offer {offer.OfferId} for BOM line {line.BomLineId} is missing a vendor part number."));
        }

        if (createdAt - offer.QuotedAt > quoteFreshnessWindow)
        {
            diagnostics.Add(new VendorCartDiagnostic(
                VendorCartDiagnosticCode.StaleQuote,
                line.BomLineId,
                offer.OfferId,
                $"Offer {offer.OfferId} was quoted at {offer.QuotedAt:u} and should be refreshed before handoff."));
        }
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
