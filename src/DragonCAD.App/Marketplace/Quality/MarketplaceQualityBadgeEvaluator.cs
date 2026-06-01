using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.App.Marketplace.Quality;

public enum MarketplaceQualityBadgeCode
{
    GeneratedNeedsReview,
    DatasheetMissing,
    StockRisk,
    PriceUnavailable,
    DuplicateCanonicalLink,
    TrustedVendorImport,
}

public enum MarketplaceQualitySeverity
{
    Critical,
    Warning,
    Info,
}

public sealed record MarketplaceQualityBadge(
    MarketplaceQualityBadgeCode Code,
    MarketplaceQualitySeverity Severity,
    string Label,
    string Detail,
    int DisplayOrder);

public static class MarketplaceQualityBadgeEvaluator
{
    private const int LowStockThreshold = 5;

    public static IReadOnlyList<MarketplaceQualityBadge> Evaluate(
        MarketplaceComponentRow row,
        IEnumerable<MarketplaceComponentProvenance> provenanceRecords)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(provenanceRecords);

        MarketplaceComponentProvenance[] matchingProvenance = provenanceRecords
            .Where(record => string.Equals(record.ComponentKey.Value, row.CanonicalComponentId, StringComparison.Ordinal))
            .ToArray();

        List<MarketplaceQualityBadge> badges = [];

        if (matchingProvenance.Any(IsGeneratedAndNeedsReview))
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.GeneratedNeedsReview,
                    MarketplaceQualitySeverity.Critical,
                    "Generated component needs review",
                    "Datasheet-generated symbol, footprint, or model output must be reviewed before promotion.",
                    10));
        }

        if (!row.HasDatasheet)
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.DatasheetMissing,
                    MarketplaceQualitySeverity.Warning,
                    "Datasheet missing",
                    "This marketplace row does not include a linked datasheet.",
                    20));
        }

        if (row.StockQuantity <= 0)
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.StockRisk,
                    MarketplaceQualitySeverity.Warning,
                    "Out of stock",
                    "No available stock was reported for this vendor listing.",
                    30));
        }
        else if (row.StockQuantity < LowStockThreshold)
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.StockRisk,
                    MarketplaceQualitySeverity.Warning,
                    "Low stock",
                    $"Only {row.StockQuantity} units are currently reported in stock.",
                    30));
        }

        if (row.MinimumUnitPriceUsd is null)
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.PriceUnavailable,
                    MarketplaceQualitySeverity.Warning,
                    "Price unavailable",
                    "No unit price is available for BOM cost planning.",
                    40));
        }

        if (!row.IsCanonical)
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.DuplicateCanonicalLink,
                    MarketplaceQualitySeverity.Info,
                    $"Duplicate linked to {row.DuplicateOfComponentId}",
                    "This vendor listing resolves to an existing canonical DragonCAD component.",
                    50));
        }

        if (matchingProvenance.Any(IsTrustedVendorImport))
        {
            badges.Add(
                new MarketplaceQualityBadge(
                    MarketplaceQualityBadgeCode.TrustedVendorImport,
                    MarketplaceQualitySeverity.Info,
                    "Trusted vendor import",
                    "This component has an audited vendor-import provenance record.",
                    60));
        }

        return badges
            .OrderBy(badge => badge.Severity)
            .ThenBy(badge => badge.DisplayOrder)
            .ThenBy(badge => badge.Label, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsGeneratedAndNeedsReview(MarketplaceComponentProvenance provenance) =>
        provenance.Kind == MarketplaceProvenanceKind.DatasheetGenerated &&
        provenance.ReviewState == MarketplaceReviewState.PendingReview;

    private static bool IsTrustedVendorImport(MarketplaceComponentProvenance provenance) =>
        provenance.Kind == MarketplaceProvenanceKind.VendorImport &&
        (provenance.ReviewState == MarketplaceReviewState.Imported ||
         provenance.ReviewState == MarketplaceReviewState.Approved);
}
