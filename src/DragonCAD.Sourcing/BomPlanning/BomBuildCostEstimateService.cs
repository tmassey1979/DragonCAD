using System.Globalization;

namespace DragonCAD.Sourcing.BomPlanning;

public static class BomBuildCostEstimateService
{
    public static BomBuildCostEstimate Estimate(
        IEnumerable<BomPlanningComponent> components,
        IEnumerable<BomBuildCostVendorQuote> quotes,
        IEnumerable<int> buildQuantities,
        BomBuildCostEstimateOptions options)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(buildQuantities);
        ArgumentNullException.ThrowIfNull(options);

        var quoteList = quotes.ToArray();
        var groups = GroupComponents(components).ToArray();
        var quantities = buildQuantities
            .Select(quantity =>
            {
                if (quantity <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(buildQuantities), quantity, "Build quantity must be greater than zero.");
                }

                return quantity;
            })
            .Distinct()
            .Order()
            .ToArray();

        if (quantities.Length == 0)
        {
            throw new ArgumentException("At least one build quantity is required.", nameof(buildQuantities));
        }

        var culture = CultureInfo.GetCultureInfo(options.CultureName);
        var unavailableLines = new List<BomBuildCostUnavailableLine>();
        var diagnostics = new List<BomBuildCostDiagnostic>();
        var scenarios = quantities
            .Select(quantity => EstimateScenario(quantity, groups, quoteList, options, culture, unavailableLines, diagnostics))
            .ToArray();

        return new BomBuildCostEstimate(
            options.EstimateAt,
            options.CurrencyCode,
            scenarios,
            unavailableLines
                .OrderBy(line => line.BuildQuantity)
                .ThenBy(line => line.GroupKey, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            diagnostics
                .OrderBy(diagnostic => diagnostic.BuildQuantity)
                .ThenBy(diagnostic => diagnostic.GroupKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static BomBuildCostEstimateScenario EstimateScenario(
        int buildQuantity,
        IReadOnlyList<BomBuildCostGroup> groups,
        IReadOnlyList<BomBuildCostVendorQuote> quotes,
        BomBuildCostEstimateOptions options,
        CultureInfo culture,
        List<BomBuildCostUnavailableLine> unavailableLines,
        List<BomBuildCostDiagnostic> diagnostics)
    {
        var lines = new List<BomBuildCostEstimateLine>();
        Money total = new(0, options.CurrencyCode);

        foreach (var group in groups)
        {
            var requiredQuantity = checked(group.QuantityPerBuild * buildQuantity);
            var candidate = SelectOffer(group, quotes, requiredQuantity, options);
            if (candidate is null)
            {
                var availableQuantity = AvailableQuantity(group, quotes);
                var unavailableLine = new BomBuildCostUnavailableLine(
                    buildQuantity,
                    group.GroupKey,
                    requiredQuantity,
                    availableQuantity,
                    $"Only {availableQuantity} of {requiredQuantity} units are available for {group.GroupKey}.");
                unavailableLines.Add(unavailableLine);
                diagnostics.Add(new BomBuildCostDiagnostic(
                    "Shortage",
                    buildQuantity,
                    group.GroupKey,
                    unavailableLine.Message));
                continue;
            }

            lines.Add(candidate);
            total = Add(total, candidate.LineTotal);
            diagnostics.AddRange(candidate.Diagnostics);
        }

        var orderedLines = lines
            .OrderBy(line => line.GroupKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BomBuildCostEstimateScenario(
            buildQuantity,
            total,
            FormatMoney(total, culture),
            orderedLines);
    }

    private static BomBuildCostEstimateLine? SelectOffer(
        BomBuildCostGroup group,
        IReadOnlyList<BomBuildCostVendorQuote> quotes,
        int requiredQuantity,
        BomBuildCostEstimateOptions options)
    {
        return quotes
            .Where(quote =>
                string.Equals(CreateGroupKey(quote.CanonicalIdentity, quote.SelectedValue, quote.Package), group.GroupKey, StringComparison.OrdinalIgnoreCase)
                && group.AllowedPartNumbers.Contains(NormalizeKey(quote.ManufacturerPartNumber)))
            .Select(quote => CreateCandidate(group, quote, requiredQuantity, options))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(candidate => candidate.LineTotal.Amount)
            .ThenBy(candidate => candidate.IsAlternate)
            .ThenByDescending(candidate => candidate.IsPreferredVendor)
            .ThenBy(candidate => candidate.PurchaseQuantity)
            .ThenBy(candidate => candidate.LeadTimeDays)
            .ThenBy(candidate => candidate.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static BomBuildCostEstimateLine? CreateCandidate(
        BomBuildCostGroup group,
        BomBuildCostVendorQuote quote,
        int requiredQuantity,
        BomBuildCostEstimateOptions options)
    {
        var purchaseQuantity = RoundPurchaseQuantity(requiredQuantity, quote.MinimumOrderQuantity, quote.OrderMultiple);
        if (purchaseQuantity > quote.Stock)
        {
            return null;
        }

        var priceBreak = quote.PriceLadder.FindBestBreakFor(purchaseQuantity);
        if (!string.Equals(priceBreak.UnitPrice.CurrencyCode, options.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BOM build cost estimate cannot compare mixed currencies.");
        }

        var lineTotal = new Money(priceBreak.UnitPrice.Amount * purchaseQuantity, options.CurrencyCode);
        var diagnostics = CreateLineDiagnostics(group, quote, requiredQuantity, purchaseQuantity, priceBreak, options).ToArray();
        var isPreferredVendor = quote.IsPreferredVendor;

        return new BomBuildCostEstimateLine(
            requiredQuantity / group.QuantityPerBuild,
            group.GroupKey,
            quote.VendorName,
            quote.VendorPartNumber,
            quote.ManufacturerPartNumber,
            isPreferredVendor,
            !string.Equals(group.SelectedManufacturerPartNumber, NormalizeKey(quote.ManufacturerPartNumber), StringComparison.OrdinalIgnoreCase),
            requiredQuantity,
            purchaseQuantity,
            priceBreak.UnitPrice,
            lineTotal,
            FormatMoney(lineTotal, CultureInfo.GetCultureInfo(options.CultureName)),
            quote.Stock,
            quote.LeadTimeDays,
            quote.Lifecycle,
            isPreferredVendor ? $"Preferred vendor selected: {quote.VendorName}." : null,
            diagnostics);
    }

    private static IEnumerable<BomBuildCostDiagnostic> CreateLineDiagnostics(
        BomBuildCostGroup group,
        BomBuildCostVendorQuote quote,
        int requiredQuantity,
        int purchaseQuantity,
        QuantityPriceBreak priceBreak,
        BomBuildCostEstimateOptions options)
    {
        var buildQuantity = requiredQuantity / group.QuantityPerBuild;
        if (purchaseQuantity != requiredQuantity || priceBreak.Quantity > 1)
        {
            yield return new BomBuildCostDiagnostic(
                "PriceBreakApplied",
                buildQuantity,
                group.GroupKey,
                $"Purchased {purchaseQuantity} units at the {priceBreak.Quantity}+ price break for {group.GroupKey}.");
        }

        if (options.EstimateAt - quote.CapturedAt > options.MaxQuoteAge)
        {
            yield return new BomBuildCostDiagnostic(
                "StaleQuote",
                buildQuantity,
                group.GroupKey,
                $"Quote from {quote.VendorName} was captured at {quote.CapturedAt:u}.");
        }

        if (quote.Lifecycle != BomPartLifecycle.Active)
        {
            yield return new BomBuildCostDiagnostic(
                "LifecycleWarning",
                buildQuantity,
                group.GroupKey,
                $"{quote.ManufacturerPartNumber} lifecycle is {quote.Lifecycle}.");
        }
    }

    private static IEnumerable<BomBuildCostGroup> GroupComponents(IEnumerable<BomPlanningComponent> components)
    {
        return components
            .GroupBy(component => CreateGroupKey(component.CanonicalIdentity, component.SelectedValue, component.Package), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var orderedComponents = group
                    .OrderBy(component => component.Designator, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var first = orderedComponents[0];
                var allowedPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    NormalizeKey(first.SelectedManufacturerPartNumber),
                };

                if (!orderedComponents.Any(component => component.DoNotSubstitute))
                {
                    foreach (var alternate in orderedComponents.SelectMany(component => component.Alternates))
                    {
                        allowedPartNumbers.Add(NormalizeKey(alternate));
                    }
                }

                return new BomBuildCostGroup(
                    group.Key,
                    orderedComponents.Sum(component => component.QuantityPerBuild),
                    NormalizeKey(first.SelectedManufacturerPartNumber),
                    allowedPartNumbers);
            });
    }

    private static int AvailableQuantity(BomBuildCostGroup group, IEnumerable<BomBuildCostVendorQuote> quotes)
    {
        return quotes
            .Where(quote =>
                string.Equals(CreateGroupKey(quote.CanonicalIdentity, quote.SelectedValue, quote.Package), group.GroupKey, StringComparison.OrdinalIgnoreCase)
                && group.AllowedPartNumbers.Contains(NormalizeKey(quote.ManufacturerPartNumber)))
            .Sum(quote => quote.Stock);
    }

    private static int RoundPurchaseQuantity(int requiredQuantity, int minimumOrderQuantity, int orderMultiple)
    {
        var purchaseQuantity = Math.Max(requiredQuantity, minimumOrderQuantity);
        var remainder = purchaseQuantity % orderMultiple;
        return remainder == 0
            ? purchaseQuantity
            : checked(purchaseQuantity + orderMultiple - remainder);
    }

    private static Money Add(Money total, Money addend)
    {
        if (!string.Equals(total.CurrencyCode, addend.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BOM build cost estimate cannot total mixed currencies.");
        }

        return new Money(total.Amount + addend.Amount, total.CurrencyCode);
    }

    private static string FormatMoney(Money money, CultureInfo culture)
    {
        return money.Amount.ToString("C", culture);
    }

    private static string CreateGroupKey(string canonicalIdentity, string selectedValue, string package)
    {
        return $"{NormalizeKey(canonicalIdentity)}|{NormalizeKey(selectedValue)}|{NormalizeKey(package)}";
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed record BomBuildCostGroup(
        string GroupKey,
        int QuantityPerBuild,
        string SelectedManufacturerPartNumber,
        IReadOnlySet<string> AllowedPartNumbers);
}
