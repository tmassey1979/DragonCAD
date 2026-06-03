using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Compliance;

public sealed record ProviderComplianceMetadata
{
    public ProviderComplianceMetadata(
        string providerId,
        IReadOnlyList<MarketplaceSourceMode> allowedSourceModes,
        AttributionRequirement attribution,
        RedistributionPolicy redistribution,
        ProviderCacheLimit cacheLimit,
        IReadOnlyList<MarketplaceAutomationMode> blockedAutomationModes)
    {
        ProviderId = RequireText(providerId, nameof(providerId));
        AllowedSourceModes = new ReadOnlyCollection<MarketplaceSourceMode>(
            NormalizeModes(allowedSourceModes).ToArray());
        Attribution = attribution ?? throw new ArgumentNullException(nameof(attribution));
        Redistribution = redistribution;
        CacheLimit = cacheLimit ?? throw new ArgumentNullException(nameof(cacheLimit));
        BlockedAutomationModes = new ReadOnlyCollection<MarketplaceAutomationMode>(
            NormalizeModes(blockedAutomationModes)
                .Where(mode => mode != MarketplaceAutomationMode.None)
                .ToArray());
    }

    public string ProviderId { get; }

    public IReadOnlyList<MarketplaceSourceMode> AllowedSourceModes { get; }

    public AttributionRequirement Attribution { get; }

    public RedistributionPolicy Redistribution { get; }

    public ProviderCacheLimit CacheLimit { get; }

    public IReadOnlyList<MarketplaceAutomationMode> BlockedAutomationModes { get; }

    public bool ProhibitsRedistribution => Redistribution == RedistributionPolicy.NotAllowed;

    public bool AllowsSourceMode(MarketplaceSourceMode mode)
    {
        return AllowedSourceModes.Contains(mode);
    }

    public bool BlocksAutomationMode(MarketplaceAutomationMode mode)
    {
        return BlockedAutomationModes.Contains(mode);
    }

    private static IEnumerable<T> NormalizeModes<T>(IReadOnlyList<T>? modes)
        where T : struct, Enum
    {
        return modes?.Distinct().OrderBy(static mode => mode).ToArray() ?? [];
    }

    private static string RequireText(string value, string parameterName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }
}
