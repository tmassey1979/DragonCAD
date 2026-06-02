using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace DragonCAD.Sourcing.Catalog.Matching;

public static class VendorCatalogMatchReviewService
{
    private static readonly IReadOnlyDictionary<string, string> ManufacturerAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["STMICRO"] = "STMICROELECTRONICS",
        ["ST"] = "STMICROELECTRONICS",
        ["TEXASINSTRUMENTS"] = "TEXASINSTRUMENTS",
        ["TI"] = "TEXASINSTRUMENTS",
    };

    public static VendorCatalogMatchReviewResult BuildReview(
        IReadOnlyList<DragonCadComponentMatchProfile> components,
        IReadOnlyList<VendorCatalogMatchReviewRow> catalogRows,
        IReadOnlyList<VendorCatalogMatchReviewDecisionKey>? decisions = null)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(catalogRows);

        var decisionsByRow = (decisions ?? [])
            .ToDictionary(decision => RowKey(decision.ProviderName, decision.VendorSku), decision => decision.Decision, StringComparer.Ordinal);

        var items = catalogRows
            .Select(row => BuildItem(components, row, decisionsByRow))
            .OrderBy(item => item.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.VendorSku, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        MarkDuplicateOffers(items);

        return new VendorCatalogMatchReviewResult(items);
    }

    private static VendorCatalogMatchReviewItem BuildItem(
        IReadOnlyList<DragonCadComponentMatchProfile> components,
        VendorCatalogMatchReviewRow row,
        IReadOnlyDictionary<string, VendorCatalogMatchReviewDecision> decisionsByRow)
    {
        var best = components
            .Select(component => ScoreComponent(component, row))
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Component.ComponentKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var conflicts = new List<VendorCatalogMatchConflict>();
        VendorCatalogMatchClassification classification;
        string? componentKey = null;
        var confidence = 0;

        if (best is null || best.Confidence == 0)
        {
            classification = VendorCatalogMatchClassification.CatalogOnly;
        }
        else
        {
            classification = best.Classification;
            componentKey = best.Component.ComponentKey;
            confidence = best.Confidence;
            conflicts.AddRange(best.Conflicts);
        }

        if (IsObsolete(row.LifecycleStatus))
        {
            classification = VendorCatalogMatchClassification.Conflict;
            conflicts.Add(new VendorCatalogMatchConflict(
                VendorCatalogMatchConflictKind.ObsoleteLifecycle,
                $"Vendor row lifecycle is {row.LifecycleStatus}."));
        }

        decisionsByRow.TryGetValue(RowKey(row.ProviderName, row.VendorSku), out var decision);
        var canPlace = classification == VendorCatalogMatchClassification.ExactComponentMatch
            && decision?.Outcome != VendorCatalogMatchReviewOutcome.Rejected;

        return new VendorCatalogMatchReviewItem(
            row.ProviderName,
            row.VendorSku,
            row.ManufacturerPartNumber,
            componentKey,
            classification,
            confidence,
            conflicts,
            canPlace,
            decision);
    }

    private static ComponentScore ScoreComponent(DragonCadComponentMatchProfile component, VendorCatalogMatchReviewRow row)
    {
        var mpnMatches = NormalizeIdentity(component.ManufacturerPartNumber) == NormalizeIdentity(row.ManufacturerPartNumber);
        var manufacturerMatches = NormalizeManufacturer(component.Manufacturer) == NormalizeManufacturer(row.Manufacturer);
        var packageMatches = NormalizePackage(component.Package) == NormalizePackage(row.Package);
        var valueMatches = NormalizeValue(component.Value) == NormalizeValue(row.Value);
        var conflicts = new List<VendorCatalogMatchConflict>();

        if (mpnMatches && manufacturerMatches && packageMatches && valueMatches)
        {
            return new ComponentScore(component, VendorCatalogMatchClassification.ExactComponentMatch, 100, conflicts);
        }

        if (mpnMatches)
        {
            AddMismatch(conflicts, VendorCatalogMatchConflictKind.PackageMismatch, packageMatches, component.Package, row.Package);
            AddMismatch(conflicts, VendorCatalogMatchConflictKind.ValueMismatch, valueMatches, component.Value, row.Value);

            return new ComponentScore(
                component,
                VendorCatalogMatchClassification.Conflict,
                80 + (manufacturerMatches ? 10 : 0),
                conflicts);
        }

        if (valueMatches && packageMatches)
        {
            return new ComponentScore(component, VendorCatalogMatchClassification.LikelyAlternate, manufacturerMatches ? 74 : 70, conflicts);
        }

        if (manufacturerMatches && valueMatches)
        {
            conflicts.Add(new VendorCatalogMatchConflict(
                VendorCatalogMatchConflictKind.PackageMismatch,
                $"Component package '{component.Package}' differs from vendor package '{row.Package}'."));

            return new ComponentScore(component, VendorCatalogMatchClassification.PackageVariant, 68, conflicts);
        }

        if (manufacturerMatches && packageMatches)
        {
            conflicts.Add(new VendorCatalogMatchConflict(
                VendorCatalogMatchConflictKind.ValueMismatch,
                $"Component value '{component.Value}' differs from vendor value '{row.Value}'."));

            return new ComponentScore(component, VendorCatalogMatchClassification.ValueVariant, 62, conflicts);
        }

        return new ComponentScore(component, VendorCatalogMatchClassification.CatalogOnly, 0, conflicts);
    }

    private static void MarkDuplicateOffers(List<VendorCatalogMatchReviewItem> items)
    {
        foreach (var duplicateGroup in items
            .Where(item => item.ComponentKey is not null)
            .GroupBy(item => $"{item.ComponentKey}|{NormalizeIdentity(item.ManufacturerPartNumber)}", StringComparer.Ordinal))
        {
            var first = true;
            foreach (var item in duplicateGroup)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                item.AddConflict(new VendorCatalogMatchConflict(
                    VendorCatalogMatchConflictKind.DuplicateOffer,
                    "Another vendor row already ranks first for this component and manufacturer part number."));
            }
        }
    }

    private static void AddMismatch(
        List<VendorCatalogMatchConflict> conflicts,
        VendorCatalogMatchConflictKind kind,
        bool matches,
        string componentValue,
        string rowValue)
    {
        if (matches)
        {
            return;
        }

        var label = kind == VendorCatalogMatchConflictKind.PackageMismatch ? "package" : "value";
        conflicts.Add(new VendorCatalogMatchConflict(
            kind,
            $"Component {label} '{componentValue}' differs from vendor {label} '{rowValue}'."));
    }

    private static bool IsObsolete(string lifecycleStatus)
    {
        var normalized = NormalizeToken(lifecycleStatus);
        return normalized.Contains("OBSOLETE", StringComparison.Ordinal)
            || normalized.Contains("DISCONTINUED", StringComparison.Ordinal)
            || normalized.Contains("NRND", StringComparison.Ordinal);
    }

    private static string RowKey(string providerName, string vendorSku)
    {
        return $"{NormalizeToken(providerName)}|{NormalizeToken(vendorSku)}";
    }

    private static string NormalizeIdentity(string value)
    {
        return NormalizeToken(value).Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizePackage(string value)
    {
        return NormalizeToken(value);
    }

    private static string NormalizeManufacturer(string value)
    {
        var normalized = NormalizeToken(value);
        return ManufacturerAliases.TryGetValue(normalized, out var alias)
            ? alias
            : normalized;
    }

    private static string NormalizeValue(string value)
    {
        var normalized = NormalizeToken(value);
        return normalized
            .Replace("KOHM", "K", StringComparison.Ordinal)
            .Replace("OHM", string.Empty, StringComparison.Ordinal)
            .Replace("UF", "U", StringComparison.Ordinal)
            .Replace("NF", "N", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpper(character, CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private sealed record ComponentScore(
        DragonCadComponentMatchProfile Component,
        VendorCatalogMatchClassification Classification,
        int Confidence,
        IReadOnlyList<VendorCatalogMatchConflict> Conflicts);
}

public sealed record DragonCadComponentMatchProfile(
    string ComponentKey,
    string ManufacturerPartNumber,
    string Manufacturer,
    string Package,
    string Value);

public sealed record VendorCatalogMatchReviewRow(
    string ProviderName,
    string VendorSku,
    string ManufacturerPartNumber,
    string Manufacturer,
    string Package,
    string Value,
    string LifecycleStatus);

public sealed record VendorCatalogMatchReviewResult(IReadOnlyList<VendorCatalogMatchReviewItem> Items);

public sealed class VendorCatalogMatchReviewItem
{
    private readonly List<VendorCatalogMatchConflict> conflicts;

    public VendorCatalogMatchReviewItem(
        string providerName,
        string vendorSku,
        string manufacturerPartNumber,
        string? componentKey,
        VendorCatalogMatchClassification classification,
        int confidence,
        IReadOnlyList<VendorCatalogMatchConflict> conflicts,
        bool isAvailableForDirectPlacement,
        VendorCatalogMatchReviewDecision? decision)
    {
        ProviderName = providerName;
        VendorSku = vendorSku;
        ManufacturerPartNumber = manufacturerPartNumber;
        ComponentKey = componentKey;
        Classification = classification;
        Confidence = confidence;
        this.conflicts = conflicts.ToList();
        IsAvailableForDirectPlacement = isAvailableForDirectPlacement;
        Decision = decision;
    }

    public string ProviderName { get; }

    public string VendorSku { get; }

    public string ManufacturerPartNumber { get; }

    public string? ComponentKey { get; }

    public VendorCatalogMatchClassification Classification { get; }

    public int Confidence { get; }

    public IReadOnlyList<VendorCatalogMatchConflict> Conflicts => new ReadOnlyCollection<VendorCatalogMatchConflict>(conflicts);

    public bool IsAvailableForDirectPlacement { get; private set; }

    public VendorCatalogMatchReviewDecision? Decision { get; }

    internal void AddConflict(VendorCatalogMatchConflict conflict)
    {
        conflicts.Add(conflict);
        IsAvailableForDirectPlacement = false;
    }
}

public enum VendorCatalogMatchClassification
{
    CatalogOnly = 0,
    ValueVariant = 1,
    PackageVariant = 2,
    LikelyAlternate = 3,
    ExactComponentMatch = 4,
    Conflict = 5,
}

public enum VendorCatalogMatchConflictKind
{
    PackageMismatch,
    ValueMismatch,
    ObsoleteLifecycle,
    DuplicateOffer,
}

public sealed record VendorCatalogMatchConflict(VendorCatalogMatchConflictKind Kind, string Message);

public enum VendorCatalogMatchReviewOutcome
{
    Accepted,
    Rejected,
    Ignored,
}

public sealed record VendorCatalogMatchReviewDecision(
    VendorCatalogMatchReviewOutcome Outcome,
    string Reviewer,
    string Notes,
    DateTimeOffset Timestamp)
{
    public VendorCatalogMatchReviewDecisionKey For(string providerName, string vendorSku)
    {
        return new VendorCatalogMatchReviewDecisionKey(providerName, vendorSku, this);
    }
}

public sealed record VendorCatalogMatchReviewDecisionKey(
    string ProviderName,
    string VendorSku,
    VendorCatalogMatchReviewDecision Decision);
