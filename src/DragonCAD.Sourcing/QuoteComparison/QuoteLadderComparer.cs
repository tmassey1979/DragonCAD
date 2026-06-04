namespace DragonCAD.Sourcing.QuoteComparison;

public static class QuoteLadderComparer
{
    private const int StaleOfferAgeDays = 90;

    public static QuoteLadderComparison Compare(QuoteLadderComparisonRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RequestedBuildQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.RequestedBuildQuantity,
                "Requested build quantity must be greater than zero.");
        }

        var results = new List<QuoteLadderComparisonResult>();
        var diagnostics = new List<QuoteComparisonDiagnostic>();

        foreach (var offer in request.Offers)
        {
            EvaluateOffer(request, offer, results, diagnostics);
        }

        return new QuoteLadderComparison(
            results
                .OrderBy(result => result.ExtendedCost.Amount)
                .ThenBy(result => result.AvailabilityRank)
                .ThenBy(result => result.Offer.LifecycleRisk)
                .ThenByDescending(result => result.Offer.IsPreferredVendor)
                .ThenBy(result => result.Offer.VendorName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(result => result.Offer.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            diagnostics.ToArray());
    }

    private static void EvaluateOffer(
        QuoteLadderComparisonRequest request,
        QuoteLadderOffer offer,
        ICollection<QuoteLadderComparisonResult> results,
        ICollection<QuoteComparisonDiagnostic> diagnostics)
    {
        var purchaseQuantity = Math.Max(request.RequestedBuildQuantity, offer.MinimumOrderQuantity);
        AddOfferDiagnostics(request, offer, purchaseQuantity, diagnostics);

        var selectedPriceBreak = FindApplicablePriceBreak(offer.PriceBreaks, purchaseQuantity);
        if (selectedPriceBreak is null)
        {
            diagnostics.Add(QuoteComparisonDiagnostic.ForOffer(
                QuoteComparisonDiagnosticCode.MissingPriceBreak,
                offer,
                $"No price break applies at purchase quantity {purchaseQuantity}."));

            return;
        }

        results.Add(new QuoteLadderComparisonResult(
            offer,
            request.RequestedBuildQuantity,
            purchaseQuantity,
            selectedPriceBreak.Quantity,
            selectedPriceBreak.UnitPrice,
            new Money(selectedPriceBreak.UnitPrice.Amount * purchaseQuantity, selectedPriceBreak.UnitPrice.CurrencyCode)));
    }

    private static void AddOfferDiagnostics(
        QuoteLadderComparisonRequest request,
        QuoteLadderOffer offer,
        int purchaseQuantity,
        ICollection<QuoteComparisonDiagnostic> diagnostics)
    {
        if (offer.QuantityAvailable < purchaseQuantity)
        {
            diagnostics.Add(QuoteComparisonDiagnostic.ForOffer(
                QuoteComparisonDiagnosticCode.InsufficientStock,
                offer,
                $"Only {offer.QuantityAvailable} units are available for purchase quantity {purchaseQuantity}."));
        }

        if (offer.MinimumOrderQuantity > request.RequestedBuildQuantity)
        {
            diagnostics.Add(QuoteComparisonDiagnostic.ForOffer(
                QuoteComparisonDiagnosticCode.MoqMismatch,
                offer,
                $"Minimum order quantity {offer.MinimumOrderQuantity} exceeds requested build quantity {request.RequestedBuildQuantity}."));
        }

        if (request.EvaluationDate.DayNumber - offer.LastUpdated.DayNumber > StaleOfferAgeDays)
        {
            diagnostics.Add(QuoteComparisonDiagnostic.ForOffer(
                QuoteComparisonDiagnosticCode.StaleOffer,
                offer,
                $"Offer was last updated on {offer.LastUpdated:yyyy-MM-dd}."));
        }

        if (offer.LifecycleRisk != QuoteLifecycleRisk.Active)
        {
            diagnostics.Add(QuoteComparisonDiagnostic.ForOffer(
                QuoteComparisonDiagnosticCode.LifecycleWarning,
                offer,
                $"Lifecycle risk is {offer.LifecycleRisk}."));
        }
    }

    private static QuantityPriceBreak? FindApplicablePriceBreak(
        IEnumerable<QuantityPriceBreak> priceBreaks,
        int purchaseQuantity)
    {
        return priceBreaks
            .Where(priceBreak => priceBreak.Quantity <= purchaseQuantity)
            .OrderByDescending(priceBreak => priceBreak.Quantity)
            .ThenBy(priceBreak => priceBreak.UnitPrice.Amount)
            .ThenBy(priceBreak => priceBreak.UnitPrice.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}

public sealed record QuoteLadderComparisonRequest(
    string ManufacturerPartNumber,
    int RequestedBuildQuantity,
    DateOnly EvaluationDate,
    IReadOnlyList<QuoteLadderOffer> Offers);

public sealed record QuoteLadderComparison(
    IReadOnlyList<QuoteLadderComparisonResult> Results,
    IReadOnlyList<QuoteComparisonDiagnostic> Diagnostics);

public sealed record QuoteLadderComparisonResult(
    QuoteLadderOffer Offer,
    int RequestedBuildQuantity,
    int PurchaseQuantity,
    int SelectedPriceBreakQuantity,
    Money UnitPrice,
    Money ExtendedCost)
{
    public bool HasSufficientStock => Offer.QuantityAvailable >= PurchaseQuantity;

    public int AvailabilityRank => HasSufficientStock ? 0 : Offer.QuantityAvailable > 0 ? 1 : 2;
}

public sealed record QuoteLadderOffer(
    string VendorName,
    string VendorPartNumber,
    string ManufacturerPartNumber,
    int QuantityAvailable,
    int MinimumOrderQuantity,
    IReadOnlyList<QuantityPriceBreak> PriceBreaks,
    DateOnly LastUpdated,
    QuoteLifecycleRisk LifecycleRisk,
    bool IsPreferredVendor);

public enum QuoteLifecycleRisk
{
    Active = 0,
    NotRecommendedForNewDesigns = 1,
    Obsolete = 2,
}

public enum QuoteComparisonDiagnosticCode
{
    MissingPriceBreak,
    InsufficientStock,
    MoqMismatch,
    StaleOffer,
    LifecycleWarning,
}

public sealed record QuoteComparisonDiagnostic(
    QuoteComparisonDiagnosticCode Code,
    string VendorName,
    string VendorPartNumber,
    string Message)
{
    public static QuoteComparisonDiagnostic ForOffer(
        QuoteComparisonDiagnosticCode code,
        QuoteLadderOffer offer,
        string message)
    {
        return new QuoteComparisonDiagnostic(code, offer.VendorName, offer.VendorPartNumber, message);
    }
}
