using DragonCAD.Sourcing.Compliance;
using DragonCAD.Sourcing.Marketplace;

namespace DragonCAD.App.Marketplace.Compliance;

public sealed class ProviderTermsReviewViewModel
{
    private ProviderTermsReviewViewModel(
        ProviderTermsReviewState state,
        IReadOnlyList<string> catalogCapabilityLabels,
        IReadOnlyList<string> manufacturingCapabilityLabels,
        IReadOnlyList<string> sourceModeLabels,
        IReadOnlyList<string> blockedAutomationModeLabels,
        IReadOnlyList<string> providerTermLabels,
        IReadOnlyList<string> requiredManufacturingArtifactLabels,
        IReadOnlyList<string> missingManufacturingArtifactLabels,
        ProviderTermsReviewActionViewModel primaryAction)
    {
        ProviderId = state.Capabilities.ProviderId;
        ProviderName = state.Capabilities.DisplayName;
        CatalogCapabilityLabels = catalogCapabilityLabels;
        ManufacturingCapabilityLabels = manufacturingCapabilityLabels;
        SourceModeLabels = sourceModeLabels;
        BlockedAutomationModeLabels = blockedAutomationModeLabels;
        ProviderTermLabels = providerTermLabels;
        RequiredManufacturingArtifactLabels = requiredManufacturingArtifactLabels;
        MissingManufacturingArtifactLabels = missingManufacturingArtifactLabels;
        PrimaryAction = primaryAction;
        AttributionSummary = FormatAttribution(state.Compliance.Attribution);
        RedistributionSummary = FormatRedistribution(state.Compliance.Redistribution);
        CacheLimitSummary = FormatCacheLimit(state.Compliance.CacheLimit);
    }

    public string ProviderId { get; }

    public string ProviderName { get; }

    public IReadOnlyList<string> CatalogCapabilityLabels { get; }

    public IReadOnlyList<string> ManufacturingCapabilityLabels { get; }

    public IReadOnlyList<string> SourceModeLabels { get; }

    public string AttributionSummary { get; }

    public string RedistributionSummary { get; }

    public string CacheLimitSummary { get; }

    public IReadOnlyList<string> BlockedAutomationModeLabels { get; }

    public IReadOnlyList<string> ProviderTermLabels { get; }

    public string ProviderTermsSummary => ProviderTermLabels.Count == 0
        ? "No special provider terms"
        : string.Join(", ", ProviderTermLabels);

    public IReadOnlyList<string> RequiredManufacturingArtifactLabels { get; }

    public IReadOnlyList<string> MissingManufacturingArtifactLabels { get; }

    public ProviderTermsReviewActionViewModel PrimaryAction { get; }

    public static ProviderTermsReviewViewModel FromState(ProviderTermsReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        IReadOnlyList<MarketplaceManufacturingArtifact> missingArtifacts = state.Capabilities.RequiredArtifacts
            .Where(artifact => !state.AvailableArtifacts.Contains(artifact))
            .ToArray();

        return new ProviderTermsReviewViewModel(
            state,
            catalogCapabilityLabels: FlagLabels(state.Capabilities.Catalog, FormatCatalogCapability),
            manufacturingCapabilityLabels: FlagLabels(state.Capabilities.Manufacturing, FormatManufacturingCapability),
            sourceModeLabels: state.Compliance.AllowedSourceModes.Select(FormatSourceMode).ToArray(),
            blockedAutomationModeLabels: state.Compliance.BlockedAutomationModes.Select(FormatAutomationMode).ToArray(),
            providerTermLabels: FlagLabels(state.Capabilities.Terms, FormatProviderTerm),
            requiredManufacturingArtifactLabels: state.Capabilities.RequiredArtifacts.Select(FormatManufacturingArtifact).ToArray(),
            missingManufacturingArtifactLabels: missingArtifacts.Select(FormatManufacturingArtifact).ToArray(),
            primaryAction: CreatePrimaryAction(state, missingArtifacts));
    }

    private static ProviderTermsReviewActionViewModel CreatePrimaryAction(
        ProviderTermsReviewState state,
        IReadOnlyList<MarketplaceManufacturingArtifact> missingArtifacts)
    {
        if (!state.HasCredentials)
        {
            return ProviderTermsReviewActionViewModel.Blocked(
                "Credentials",
                $"Add {state.Capabilities.DisplayName} credentials before API-backed catalog review can run.");
        }

        if (state.CatalogDataStale)
        {
            return ProviderTermsReviewActionViewModel.Blocked(
                "Stale data",
                $"Refresh {state.Capabilities.DisplayName} catalog data before reviewing manual-feed terms.");
        }

        if (missingArtifacts.Count > 0)
        {
            return ProviderTermsReviewActionViewModel.Blocked(
                "Missing manufacturing artifacts",
                $"Export {JoinLabels(missingArtifacts.Select(FormatManufacturingArtifact).ToArray())} before {state.Capabilities.DisplayName} upload or quote handoff.");
        }

        if (state.Compliance.BlocksAutomationMode(state.RequestedAutomationMode))
        {
            return ProviderTermsReviewActionViewModel.Blocked(
                "Provider terms",
                $"{state.Capabilities.DisplayName} terms block {FormatAutomationMode(state.RequestedAutomationMode)} automation for this provider.");
        }

        return ProviderTermsReviewActionViewModel.Ready;
    }

    private static IReadOnlyList<string> FlagLabels<TEnum>(TEnum value, Func<TEnum, string> label)
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Where(flag => Convert.ToInt64(flag) != 0 && value.HasFlag(flag))
            .Select(label)
            .ToArray();
    }

    private static string FormatAttribution(AttributionRequirement attribution)
    {
        if (!attribution.IsRequired)
        {
            return "Attribution not required";
        }

        return string.IsNullOrWhiteSpace(attribution.Notice)
            ? "Attribution required"
            : $"Required: {attribution.Notice}";
    }

    private static string FormatRedistribution(RedistributionPolicy redistribution) =>
        redistribution switch
        {
            RedistributionPolicy.Allowed => "Allowed",
            RedistributionPolicy.AllowedWithAttribution => "Allowed with attribution",
            RedistributionPolicy.NotAllowed => "Not allowed",
            _ => "Unknown"
        };

    private static string FormatCacheLimit(ProviderCacheLimit cacheLimit)
    {
        string summary = cacheLimit.Mode switch
        {
            ProviderCacheLimitMode.Unlimited => "Unlimited",
            ProviderCacheLimitMode.NoPersistentCache => "No persistent cache",
            ProviderCacheLimitMode.TimeToLive => FormatTimeToLiveCacheLimit(cacheLimit),
            _ => cacheLimit.Mode.ToString()
        };

        return string.IsNullOrWhiteSpace(cacheLimit.Note)
            ? summary
            : $"{summary}. {cacheLimit.Note}";
    }

    private static string FormatTimeToLiveCacheLimit(ProviderCacheLimit cacheLimit)
    {
        string age = cacheLimit.MaxAge is null
            ? "bounded freshness"
            : FormatDuration(cacheLimit.MaxAge.Value);
        string entries = cacheLimit.MaxEntries is null
            ? "unbounded entries"
            : $"{cacheLimit.MaxEntries.Value:N0} entries";

        return $"Time to live: {age}, {entries}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1 && duration.TotalDays % 1 == 0)
        {
            return $"{duration.TotalDays:N0} {Pluralize((int)duration.TotalDays, "day")}";
        }

        if (duration.TotalHours >= 1 && duration.TotalHours % 1 == 0)
        {
            return $"{duration.TotalHours:N0} {Pluralize((int)duration.TotalHours, "hour")}";
        }

        return $"{duration.TotalMinutes:N0} {Pluralize((int)duration.TotalMinutes, "minute")}";
    }

    private static string JoinLabels(IReadOnlyList<string> labels)
    {
        if (labels.Count <= 1)
        {
            return labels.Count == 0 ? "" : labels[0];
        }

        if (labels.Count == 2)
        {
            return $"{labels[0]} and {labels[1]}";
        }

        return $"{string.Join(", ", labels.Take(labels.Count - 1))}, and {labels[^1]}";
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";

    private static string FormatCatalogCapability(MarketplaceCatalogCapabilities capability) =>
        capability switch
        {
            MarketplaceCatalogCapabilities.Search => "Search",
            MarketplaceCatalogCapabilities.ProductDetails => "Product details",
            MarketplaceCatalogCapabilities.Pricing => "Pricing",
            MarketplaceCatalogCapabilities.Stock => "Stock",
            MarketplaceCatalogCapabilities.Lifecycle => "Lifecycle",
            MarketplaceCatalogCapabilities.DatasheetLinks => "Datasheet links",
            MarketplaceCatalogCapabilities.ImageLinks => "Image links",
            _ => capability.ToString()
        };

    private static string FormatManufacturingCapability(MarketplaceManufacturingCapabilities capability) =>
        capability switch
        {
            MarketplaceManufacturingCapabilities.PrototypeBoardHandoff => "Prototype board handoff",
            MarketplaceManufacturingCapabilities.ProductionQuoteHandoff => "Production quote handoff",
            _ => capability.ToString()
        };

    private static string FormatProviderTerm(MarketplaceProviderTerms term) =>
        term switch
        {
            MarketplaceProviderTerms.AllowsCatalogCache => "Catalog cache",
            MarketplaceProviderTerms.AllowsPriceCache => "Price cache",
            MarketplaceProviderTerms.AllowsStockCache => "Stock cache",
            MarketplaceProviderTerms.RequiresAttribution => "Requires attribution",
            MarketplaceProviderTerms.RequiresSourceUrl => "Requires source URL",
            MarketplaceProviderTerms.RequiresSourceId => "Requires source ID",
            _ => term.ToString()
        };

    private static string FormatSourceMode(MarketplaceSourceMode mode) =>
        mode switch
        {
            MarketplaceSourceMode.Api => "API",
            MarketplaceSourceMode.ManualDownload => "Manual download",
            MarketplaceSourceMode.RepositoryClone => "Repository clone",
            MarketplaceSourceMode.DatasheetExtraction => "Datasheet extraction",
            MarketplaceSourceMode.CachedFixture => "Cached fixture",
            _ => mode.ToString()
        };

    private static string FormatAutomationMode(MarketplaceAutomationMode mode) =>
        mode switch
        {
            MarketplaceAutomationMode.Api => "API",
            MarketplaceAutomationMode.ManualReview => "Manual review",
            MarketplaceAutomationMode.Scraping => "Scraping",
            MarketplaceAutomationMode.BulkDownload => "Bulk download",
            MarketplaceAutomationMode.RepositoryMirror => "Repository mirror",
            _ => mode.ToString()
        };

    private static string FormatManufacturingArtifact(MarketplaceManufacturingArtifact artifact) =>
        artifact switch
        {
            MarketplaceManufacturingArtifact.Gerbers => "Gerbers",
            MarketplaceManufacturingArtifact.DrillFiles => "Drill files",
            MarketplaceManufacturingArtifact.BillOfMaterials => "Bill of materials",
            MarketplaceManufacturingArtifact.PickAndPlace => "Pick and place",
            MarketplaceManufacturingArtifact.BoardStackup => "Board stackup",
            MarketplaceManufacturingArtifact.AssemblyDrawing => "Assembly drawing",
            MarketplaceManufacturingArtifact.FabricationDrawing => "Fabrication drawing",
            _ => artifact.ToString()
        };
}

public sealed record ProviderTermsReviewState(
    MarketplaceProviderCapabilities Capabilities,
    ProviderComplianceMetadata Compliance,
    bool HasCredentials,
    bool CatalogDataStale,
    IReadOnlyList<MarketplaceManufacturingArtifact> AvailableArtifacts,
    MarketplaceAutomationMode RequestedAutomationMode)
{
    public MarketplaceProviderCapabilities Capabilities { get; } =
        Capabilities ?? throw new ArgumentNullException(nameof(Capabilities));

    public ProviderComplianceMetadata Compliance { get; } =
        Compliance ?? throw new ArgumentNullException(nameof(Compliance));

    public IReadOnlyList<MarketplaceManufacturingArtifact> AvailableArtifacts { get; } =
        AvailableArtifacts?.Distinct().ToArray() ?? [];
}

public sealed record ProviderTermsReviewActionViewModel(
    bool CanExecute,
    string BlockerReason,
    string DisabledExplanation)
{
    public static ProviderTermsReviewActionViewModel Ready { get; } = new(
        CanExecute: true,
        BlockerReason: "",
        DisabledExplanation: "");

    public static ProviderTermsReviewActionViewModel Blocked(string blockerReason, string disabledExplanation) =>
        new(
            CanExecute: false,
            BlockerReason: blockerReason,
            DisabledExplanation: disabledExplanation);
}
