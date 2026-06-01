using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.App.Marketplace;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Deduplication;

namespace DragonCAD.App.Marketplace.Deduplication;

public sealed class ComponentDeduplicationReviewViewModel
{
    private ComponentDeduplicationReviewViewModel(IReadOnlyList<ComponentDeduplicationReviewRow> rows)
    {
        Rows = new ObservableCollection<ComponentDeduplicationReviewRow>(rows);
    }

    public ObservableCollection<ComponentDeduplicationReviewRow> Rows { get; }

    public static ComponentDeduplicationReviewViewModel FromCandidates(IEnumerable<ComponentCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return new ComponentDeduplicationReviewViewModel(
            candidates.Select(ComponentDeduplicationReviewRow.FromCandidate).ToArray());
    }

    public static ComponentDeduplicationReviewViewModel FromMarketplaceRows(IEnumerable<MarketplaceComponentRow> rows) =>
        ComponentDeduplicationReviewFactory.FromMarketplaceRows(rows);
}

public static class ComponentDeduplicationReviewFactory
{
    public static ComponentDeduplicationReviewViewModel FromMarketplaceRows(IEnumerable<MarketplaceComponentRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        ComponentCandidate[] candidates = ComponentDeduplicator
            .GroupCandidates(rows.Select(ToCatalogListing))
            .Where(candidate => CandidateHasMultipleProviders(candidate))
            .ToArray();

        return ComponentDeduplicationReviewViewModel.FromCandidates(candidates);
    }

    private static NormalizedCatalogListing ToCatalogListing(MarketplaceComponentRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new NormalizedCatalogListing(
            RequireText(row.Provider, nameof(row.Provider)),
            RequireText(row.ManufacturerPartNumber, nameof(row.ManufacturerPartNumber)),
            RequireText(row.ManufacturerPartNumber, nameof(row.ManufacturerPartNumber)),
            RequireText(row.Manufacturer, nameof(row.Manufacturer)),
            row.DisplayName,
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(row.MinimumUnitPriceUsd ?? 0m))]),
            row.StockQuantity,
            CreateUri(row.DatasheetUrl),
            null,
            BuildFields(row),
            CatalogProviderCapabilities.Feed);
    }

    private static IReadOnlyDictionary<string, string> BuildFields(MarketplaceComponentRow row)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        AddField(fields, "Package", row.Category);
        AddField(fields, "Aliases", string.Join(", ", GetAliases(row)));
        return fields;
    }

    private static IEnumerable<string> GetAliases(MarketplaceComponentRow row)
    {
        yield return row.CanonicalComponentId;

        if (!string.IsNullOrWhiteSpace(row.DuplicateOfComponentId))
        {
            yield return row.DuplicateOfComponentId;
        }
    }

    private static bool CandidateHasMultipleProviders(ComponentCandidate candidate) =>
        candidate.SourceKeys
            .Select(GetProviderName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Skip(1)
            .Any();

    private static string GetProviderName(string sourceKey)
    {
        int separatorIndex = sourceKey.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex <= 0 ? sourceKey : sourceKey[..separatorIndex];
    }

    private static void AddField(IDictionary<string, string> fields, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[key] = value;
        }
    }

    private static Uri? CreateUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ? uri : null;

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value;
    }
}

public sealed class ComponentDeduplicationReviewRow : INotifyPropertyChanged
{
    private readonly IReadOnlyList<ComponentMergeWarning> warnings;
    private ComponentDeduplicationReviewState reviewState = ComponentDeduplicationReviewState.Pending;

    private ComponentDeduplicationReviewRow(
        string canonicalName,
        string manufacturer,
        string manufacturerPartNumber,
        string packageValueSummary,
        string aliasSummary,
        IReadOnlyList<ComponentDeduplicationVendorListingRow> vendorListings,
        IReadOnlyList<ComponentMergeWarning> warnings)
    {
        CanonicalName = canonicalName;
        Manufacturer = manufacturer;
        ManufacturerPartNumber = manufacturerPartNumber;
        PackageValueSummary = packageValueSummary;
        AliasSummary = aliasSummary;
        VendorListings = vendorListings;
        this.warnings = warnings;

        ApproveCommand = new DelegateCommand(Approve, () => CanApprove);
        RejectCommand = new DelegateCommand(Reject, () => CanReject);
        ResetCommand = new DelegateCommand(Reset, () => ReviewState != ComponentDeduplicationReviewState.Pending);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CanonicalName { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string PackageValueSummary { get; }

    public string AliasSummary { get; }

    public IReadOnlyList<ComponentDeduplicationVendorListingRow> VendorListings { get; }

    public IReadOnlyList<ComponentMergeWarning> Warnings => warnings;

    public DelegateCommand ApproveCommand { get; }

    public DelegateCommand RejectCommand { get; }

    public DelegateCommand ResetCommand { get; }

    public ComponentDeduplicationReviewState ReviewState
    {
        get => reviewState;
        private set
        {
            if (reviewState == value)
            {
                return;
            }

            reviewState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReviewStateDisplay));
            OnPropertyChanged(nameof(ReviewNote));
            OnPropertyChanged(nameof(CanApprove));
            OnPropertyChanged(nameof(CanReject));
            ApproveCommand.RaiseCanExecuteChanged();
            RejectCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
        }
    }

    public string WarningBadge => warnings.Count == 0
        ? "No warnings"
        : warnings.Count == 1 ? "1 warning" : $"{warnings.Count} warnings";

    public string ConflictSummary => warnings.Count == 0
        ? "No conflicts detected"
        : string.Join("; ", warnings.Select(FormatWarning));

    public string ReviewStateDisplay =>
        ReviewState switch
        {
            ComponentDeduplicationReviewState.Pending => "Pending Review",
            ComponentDeduplicationReviewState.Approved => "Approved",
            ComponentDeduplicationReviewState.Rejected => "Rejected",
            _ => throw new InvalidOperationException($"Unsupported deduplication review state {ReviewState}.")
        };

    public string ReviewNote =>
        ReviewState switch
        {
            ComponentDeduplicationReviewState.Pending => "Waiting for local review before merge.",
            ComponentDeduplicationReviewState.Approved => "Approved locally; merge write is still pending.",
            ComponentDeduplicationReviewState.Rejected => "Rejected locally; candidate remains unchanged.",
            _ => throw new InvalidOperationException($"Unsupported deduplication review state {ReviewState}.")
        };

    public bool CanApprove => ReviewState == ComponentDeduplicationReviewState.Pending;

    public bool CanReject => ReviewState == ComponentDeduplicationReviewState.Pending;

    public static ComponentDeduplicationReviewRow FromCandidate(ComponentCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new ComponentDeduplicationReviewRow(
            $"{candidate.Manufacturer} {candidate.ManufacturerPartNumber}",
            candidate.Manufacturer,
            candidate.ManufacturerPartNumber,
            FormatPackageValue(candidate.Package, candidate.Value),
            candidate.Aliases.Count == 0 ? "No aliases" : string.Join(", ", candidate.Aliases),
            ComponentDeduplicationVendorListingRow.FromSourceKeys(candidate.SourceKeys),
            candidate.Warnings.ToArray());
    }

    private void Approve() => ReviewState = ComponentDeduplicationReviewState.Approved;

    private void Reject() => ReviewState = ComponentDeduplicationReviewState.Rejected;

    private void Reset() => ReviewState = ComponentDeduplicationReviewState.Pending;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatPackageValue(string? package, string? value)
    {
        var parts = new[] { package, value }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!)
            .ToArray();

        return parts.Length == 0 ? "Package/value unknown" : string.Join(" / ", parts);
    }

    private static string FormatWarning(ComponentMergeWarning warning)
    {
        string label = warning.Kind switch
        {
            ComponentMergeWarningKind.ManufacturerDisagreement => "Manufacturer disagreement",
            ComponentMergeWarningKind.PackageDisagreement => "Package disagreement",
            ComponentMergeWarningKind.ValueDisagreement => "Value disagreement",
            _ => warning.Kind.ToString()
        };

        return warning.Values.Count == 0
            ? label
            : $"{label}: {string.Join(", ", warning.Values)}";
    }
}

public sealed record ComponentDeduplicationVendorListingRow(
    string ProviderName,
    IReadOnlyList<string> VendorSkus)
{
    public string DisplayText => $"{ProviderName}: {string.Join(", ", VendorSkus)}";

    public static IReadOnlyList<ComponentDeduplicationVendorListingRow> FromSourceKeys(IEnumerable<string> sourceKeys)
    {
        ArgumentNullException.ThrowIfNull(sourceKeys);

        return sourceKeys
            .Select(ParsedComponentSourceKey.From)
            .GroupBy(sourceKey => sourceKey.ProviderName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ComponentDeduplicationVendorListingRow(
                group.First().ProviderName,
                group.Select(sourceKey => sourceKey.VendorSku).OrderBy(sku => sku, StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
    }

    private sealed record ParsedComponentSourceKey(string ProviderName, string VendorSku)
    {
        public static ParsedComponentSourceKey From(string sourceKey)
        {
            string normalized = string.IsNullOrWhiteSpace(sourceKey) ? "Unknown" : sourceKey.Trim();
            int separatorIndex = normalized.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == normalized.Length - 1)
            {
                return new ParsedComponentSourceKey("Unknown", normalized);
            }

            return new ParsedComponentSourceKey(
                normalized[..separatorIndex],
                normalized[(separatorIndex + 1)..]);
        }
    }
}

public enum ComponentDeduplicationReviewState
{
    Pending,
    Approved,
    Rejected,
}
