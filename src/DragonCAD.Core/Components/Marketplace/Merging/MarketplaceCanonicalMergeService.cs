namespace DragonCAD.Core.Components.Marketplace.Merging;

public sealed class MarketplaceCanonicalMergeService
{
    public MarketplaceCanonicalMergeResult Merge(IReadOnlyList<MarketplaceComponentFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        MarketplaceCanonicalMergeDecision[] decisions = facts
            .Select(MarketplaceMergeCandidate.FromFact)
            .GroupBy(candidate => candidate.GroupKey, StringComparer.Ordinal)
            .Select(CreateDecision)
            .OrderBy(decision => decision.Component.Key)
            .ToArray();

        MarketplaceCanonicalMergeDiagnostic[] diagnostics = decisions
            .SelectMany(CreateDiagnostics)
            .OrderBy(diagnostic => diagnostic.ComponentKey)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new MarketplaceCanonicalMergeResult(decisions, diagnostics);
    }

    private static MarketplaceCanonicalMergeDecision CreateDecision(IGrouping<string, MarketplaceMergeCandidate> group)
    {
        MarketplaceMergeCandidate[] candidates = group
            .OrderBy(candidate => candidate.Fact.VendorName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Fact.VendorSku, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Fact.ManufacturerPartNumber, StringComparer.Ordinal)
            .ToArray();

        MarketplaceMergeCandidate winner = candidates
            .OrderByDescending(candidate => ScoreCanonicalCandidate(candidate.Fact))
            .ThenBy(candidate => candidate.Fact.ManufacturerPartNumber, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Fact.VendorName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Fact.VendorSku, StringComparer.Ordinal)
            .First();

        bool exactManufacturerPartMatch = candidates.All(candidate => candidate.MatchReason != "PASSIVE") && candidates
            .Select(candidate => CreateExactSignature(candidate.Fact))
            .Where(signature => signature.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count() == 1;

        CanonicalComponentKey componentKey = exactManufacturerPartMatch
            ? CanonicalComponentKey.FromPartNumber(winner.Fact.ManufacturerPartNumber)
            : winner.ComponentKey;

        CanonicalMarketplaceComponent component = CanonicalMarketplaceComponent.FromOffers(
            componentKey,
            candidates.Select(candidate => candidate.ToOffer()).ToArray(),
            winner.Fact.Value,
            winner.Fact.Package);

        return new MarketplaceCanonicalMergeDecision(
            component,
            candidates.Select(candidate => candidate.Fact).ToArray(),
            exactManufacturerPartMatch ? "MPN" : winner.MatchReason);
    }

    private static IEnumerable<MarketplaceCanonicalMergeDiagnostic> CreateDiagnostics(MarketplaceCanonicalMergeDecision decision)
    {
        string[] distinctValues = decision.SourceFacts
            .Select(fact => MarketplaceTextNormalizer.NormalizeComparableValue(fact.Value))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (distinctValues.Length > 1)
        {
            yield return new MarketplaceCanonicalMergeDiagnostic(
                MarketplaceCanonicalMergeDiagnosticSeverity.Warning,
                MarketplaceCanonicalMergeDiagnosticCodes.ConflictingValues,
                decision.Component.Key,
                $"Merged listings disagree on value: {string.Join(", ", distinctValues)}.");
        }
    }

    private static int ScoreCanonicalCandidate(MarketplaceComponentFact fact)
    {
        int score = 0;

        if (!string.IsNullOrWhiteSpace(fact.Manufacturer))
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace(fact.ManufacturerPartNumber))
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace(fact.Value))
        {
            score += 2;
        }

        if (!string.IsNullOrWhiteSpace(fact.Package))
        {
            score += 2;
        }

        return score;
    }

    private static string CreateExactSignature(MarketplaceComponentFact fact)
    {
        string manufacturer = MarketplaceTextNormalizer.NormalizeToken(fact.Manufacturer);
        string part = MarketplaceTextNormalizer.NormalizeToken(fact.ManufacturerPartNumber);

        return manufacturer.Length == 0 || part.Length == 0
            ? string.Empty
            : $"{manufacturer}:{part}";
    }
}

internal sealed record MarketplaceMergeCandidate(
    MarketplaceComponentFact Fact,
    CanonicalComponentKey ComponentKey,
    string GroupKey,
    string MatchReason)
{
    public static MarketplaceMergeCandidate FromFact(MarketplaceComponentFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        if (MarketplaceTextNormalizer.IsPassiveKind(fact.Kind))
        {
            CanonicalComponentKey key = CanonicalComponentKey.FromPassive(
                fact.Kind,
                fact.Value,
                fact.Package,
                string.IsNullOrWhiteSpace(fact.Tolerance) ? "unspecified" : fact.Tolerance);

            return new MarketplaceMergeCandidate(fact, key, key.Value, "PASSIVE");
        }

        string normalizedPart = MarketplaceTextNormalizer.NormalizeToken(fact.ManufacturerPartNumber);
        string normalizedDisplay = MarketplaceTextNormalizer.NormalizeToken(fact.DisplayName);
        string aliasKey = MarketplaceTextNormalizer.ResolveAliasKey(normalizedPart, normalizedDisplay);

        if (aliasKey.Length > 0)
        {
            return new MarketplaceMergeCandidate(
                fact,
                new CanonicalComponentKey(aliasKey),
                aliasKey,
                aliasKey);
        }

        string manufacturer = MarketplaceTextNormalizer.NormalizeToken(fact.Manufacturer);
        if (normalizedPart.Length > 0 && manufacturer.Length > 0)
        {
            string exactKey = $"MPN:{manufacturer}:{normalizedPart}";
            return new MarketplaceMergeCandidate(
                fact,
                CanonicalComponentKey.FromPartNumber(fact.ManufacturerPartNumber),
                exactKey,
                "MPN");
        }

        string fallback = normalizedPart.Length > 0
            ? normalizedPart
            : MarketplaceTextNormalizer.NormalizeToken($"{fact.VendorName}:{fact.VendorSku}");

        return new MarketplaceMergeCandidate(
            fact,
            CanonicalComponentKey.FromPartNumber(fallback),
            $"PART:{fallback}",
            "PART");
    }

    public MarketplaceVendorOffer ToOffer() =>
        new(
            VendorName: Fact.VendorName,
            VendorSku: Fact.VendorSku,
            Manufacturer: Fact.Manufacturer,
            ManufacturerPartNumber: Fact.ManufacturerPartNumber,
            DisplayName: Fact.DisplayName,
            ProductUrl: Fact.ProductUrl,
            ValueOverride: Fact.Value,
            PackageOverride: Fact.Package);
}

internal static class MarketplaceTextNormalizer
{
    public static bool IsPassiveKind(string kind)
    {
        string token = NormalizeToken(kind);
        return token is "RESISTOR" or "CAPACITOR" or "INDUCTOR";
    }

    public static string ResolveAliasKey(string normalizedPart, string normalizedDisplay)
    {
        string merged = normalizedPart + normalizedDisplay;

        if (merged.Contains("ESP32", StringComparison.Ordinal) &&
            (merged.Contains("DEVKIT", StringComparison.Ordinal) || merged.Contains("DEVBOARD", StringComparison.Ordinal)))
        {
            return "ALIAS:ESP32-DEVKIT";
        }

        if (Is7805Alias(normalizedPart))
        {
            return "ALIAS:7805";
        }

        if (Is555Alias(normalizedPart))
        {
            return "ALIAS:555";
        }

        return string.Empty;
    }

    public static string NormalizeComparableValue(string value) =>
        NormalizeToken(value)
            .Replace("KOHM", "K", StringComparison.Ordinal)
            .Replace("OHM", "R", StringComparison.Ordinal);

    public static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static bool Is7805Alias(string normalizedPart) =>
        normalizedPart is "7805" or "L7805" or "LM7805" or "L7805CV" or "LM7805CT" ||
        normalizedPart.StartsWith("L7805", StringComparison.Ordinal) ||
        normalizedPart.StartsWith("LM7805", StringComparison.Ordinal);

    private static bool Is555Alias(string normalizedPart) =>
        normalizedPart is "555" or "NE555" or "SE555" or "LM555" ||
        normalizedPart.StartsWith("NE555", StringComparison.Ordinal) ||
        normalizedPart.StartsWith("SE555", StringComparison.Ordinal) ||
        normalizedPart.StartsWith("LM555", StringComparison.Ordinal) ||
        normalizedPart.StartsWith("TLC555", StringComparison.Ordinal);
}
