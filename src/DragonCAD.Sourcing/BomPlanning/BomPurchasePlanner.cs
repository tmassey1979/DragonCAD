namespace DragonCAD.Sourcing.BomPlanning;

public static class BomPurchasePlanner
{
    public static BomPlanningResult Plan(
        IEnumerable<BomPlanningComponent> components,
        IEnumerable<BomPlanningVendorQuote> quotes,
        IEnumerable<BomBuildScenario> scenarios,
        string currencyCode,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(scenarios);

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalizedCurrencyCode = currencyCode.Trim().ToUpperInvariant();
        var groups = GroupComponents(components).ToArray();
        var quoteList = quotes.ToArray();
        var scenarioList = scenarios.ToArray();

        if (scenarioList.Length == 0)
        {
            throw new ArgumentException("At least one build scenario is required.", nameof(scenarios));
        }

        var diagnostics = new List<BomPlanningDiagnostic>();
        var scenarioResults = scenarioList
            .Select(scenario => PlanScenario(scenario, groups, quoteList, normalizedCurrencyCode, diagnostics))
            .ToArray();

        return new BomPlanningResult(
            createdAt,
            normalizedCurrencyCode,
            groups,
            scenarioResults,
            diagnostics
                .OrderBy(diagnostic => diagnostic.ScenarioName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(diagnostic => diagnostic.GroupKey, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IEnumerable<BomPlanningGroup> GroupComponents(IEnumerable<BomPlanningComponent> components)
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

                return new BomPlanningGroup(
                    group.Key,
                    NormalizeDisplay(first.CanonicalIdentity),
                    NormalizeDisplay(first.SelectedValue),
                    NormalizeDisplay(first.Package),
                    orderedComponents.Sum(component => component.QuantityPerBuild),
                    first.SelectedManufacturerPartNumber,
                    orderedComponents.Any(component => component.DoNotSubstitute),
                    orderedComponents
                        .SelectMany(component => component.Alternates)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(alternate => alternate, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    orderedComponents
                        .Select(component => component.Designator)
                        .ToArray());
            });
    }

    private static BomPlanningScenarioResult PlanScenario(
        BomBuildScenario scenario,
        IReadOnlyList<BomPlanningGroup> groups,
        IReadOnlyList<BomPlanningVendorQuote> quotes,
        string currencyCode,
        List<BomPlanningDiagnostic> diagnostics)
    {
        var purchaseLines = new List<BomPlanningPurchaseLine>();
        Money? total = null;

        foreach (var group in groups)
        {
            var remainingQuantity = checked(group.QuantityPerBuild * scenario.BuildQuantity);
            foreach (var allocation in Allocate(group, quotes, remainingQuantity, currencyCode))
            {
                remainingQuantity -= allocation.RequiredQuantity;
                purchaseLines.Add(allocation);
                total = Add(total, allocation.ExtendedCost);

                if (remainingQuantity == 0)
                {
                    break;
                }
            }

            if (remainingQuantity > 0)
            {
                diagnostics.Add(BomPlanningDiagnostic.MissingStock(scenario.Name, group, remainingQuantity));
            }
        }

        return new BomPlanningScenarioResult(
            scenario.Name,
            scenario.BuildQuantity,
            total ?? new Money(0, currencyCode),
            purchaseLines
                .OrderBy(line => line.GroupKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(line => line.VendorName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(line => line.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IEnumerable<BomPlanningPurchaseLine> Allocate(
        BomPlanningGroup group,
        IReadOnlyList<BomPlanningVendorQuote> quotes,
        int requiredQuantity,
        string currencyCode)
    {
        var remainingQuantity = requiredQuantity;
        var allowedPartNumbers = AllowedPartNumbers(group);
        var candidates = quotes
            .Where(quote =>
                string.Equals(CreateGroupKey(quote.CanonicalIdentity, quote.SelectedValue, quote.Package), group.GroupKey, StringComparison.OrdinalIgnoreCase)
                && allowedPartNumbers.Contains(NormalizeKey(quote.ManufacturerPartNumber)))
            .Select(quote => TryCreateCandidate(group, quote, remainingQuantity, currencyCode))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(candidate => candidate.UnitPrice.Amount)
            .ThenBy(candidate => candidate.IsSubstitution)
            .ThenByDescending(candidate => candidate.Quote.IsPreferredVendor)
            .ThenBy(candidate => candidate.Quote.Lifecycle)
            .ThenBy(candidate => candidate.PurchaseQuantity)
            .ThenBy(candidate => candidate.Quote.LeadTimeDays)
            .ThenBy(candidate => candidate.Quote.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Quote.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in candidates)
        {
            if (remainingQuantity == 0)
            {
                yield break;
            }

            var allocatedQuantity = Math.Min(candidate.RequiredQuantity, remainingQuantity);
            var purchaseQuantity = RoundPurchaseQuantity(
                allocatedQuantity,
                candidate.Quote.MinimumOrderQuantity,
                candidate.Quote.OrderMultiple);
            var priceBreak = candidate.Quote.PriceLadder.FindBestBreakFor(purchaseQuantity);
            if (!string.Equals(priceBreak.UnitPrice.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("BOM purchase plan cannot compare mixed currencies.");
            }

            remainingQuantity -= allocatedQuantity;
            yield return new BomPlanningPurchaseLine(
                group.GroupKey,
                candidate.Quote.VendorName,
                candidate.Quote.VendorPartNumber,
                candidate.Quote.ManufacturerPartNumber,
                candidate.Quote.IsPreferredVendor,
                candidate.IsSubstitution,
                allocatedQuantity,
                purchaseQuantity,
                priceBreak.UnitPrice,
                new Money(priceBreak.UnitPrice.Amount * purchaseQuantity, currencyCode),
                candidate.Quote.Stock,
                candidate.Quote.LeadTimeDays,
                candidate.Quote.Lifecycle);
        }
    }

    private static BomPlanningCandidate? TryCreateCandidate(
        BomPlanningGroup group,
        BomPlanningVendorQuote quote,
        int remainingQuantity,
        string currencyCode)
    {
        var allocatableQuantity = Math.Min(remainingQuantity, quote.Stock);
        while (allocatableQuantity > 0)
        {
            var purchaseQuantity = RoundPurchaseQuantity(allocatableQuantity, quote.MinimumOrderQuantity, quote.OrderMultiple);
            if (purchaseQuantity <= quote.Stock)
            {
                var priceBreak = quote.PriceLadder.FindBestBreakFor(purchaseQuantity);
                if (!string.Equals(priceBreak.UnitPrice.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("BOM purchase plan cannot compare mixed currencies.");
                }

                return new BomPlanningCandidate(
                    quote,
                    IsSubstitution(group, quote),
                    allocatableQuantity,
                    purchaseQuantity,
                    priceBreak.UnitPrice);
            }

            allocatableQuantity--;
        }

        return null;
    }

    private static HashSet<string> AllowedPartNumbers(BomPlanningGroup group)
    {
        var partNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeKey(group.SelectedManufacturerPartNumber),
        };

        if (!group.DoNotSubstitute)
        {
            foreach (var alternate in group.Alternates)
            {
                partNumbers.Add(NormalizeKey(alternate));
            }
        }

        return partNumbers;
    }

    private static bool IsSubstitution(BomPlanningGroup group, BomPlanningVendorQuote quote)
    {
        return !string.Equals(
            NormalizeKey(group.SelectedManufacturerPartNumber),
            NormalizeKey(quote.ManufacturerPartNumber),
            StringComparison.OrdinalIgnoreCase);
    }

    private static int RoundPurchaseQuantity(int requiredQuantity, int minimumOrderQuantity, int orderMultiple)
    {
        var purchaseQuantity = Math.Max(requiredQuantity, minimumOrderQuantity);
        var remainder = purchaseQuantity % orderMultiple;
        return remainder == 0
            ? purchaseQuantity
            : checked(purchaseQuantity + orderMultiple - remainder);
    }

    private static Money Add(Money? total, Money addend)
    {
        if (total is null)
        {
            return addend;
        }

        if (!string.Equals(total.Value.CurrencyCode, addend.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BOM purchase plan cannot total mixed currencies.");
        }

        return new Money(total.Value.Amount + addend.Amount, total.Value.CurrencyCode);
    }

    private static string CreateGroupKey(string canonicalIdentity, string selectedValue, string package)
    {
        return $"{NormalizeKey(canonicalIdentity)}|{NormalizeKey(selectedValue)}|{NormalizeKey(package)}";
    }

    private static string NormalizeDisplay(string value)
    {
        return value.Trim();
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed record BomPlanningCandidate(
        BomPlanningVendorQuote Quote,
        bool IsSubstitution,
        int RequiredQuantity,
        int PurchaseQuantity,
        Money UnitPrice);
}
