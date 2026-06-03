using System.Collections.ObjectModel;
using DragonCAD.Sourcing.Compliance;
using DragonCAD.Sourcing.Credentials;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Providers.Descriptors;

public sealed record LocalSourcingProviderDescriptor
{
    public LocalSourcingProviderDescriptor(
        string providerId,
        string displayName,
        IReadOnlyList<MarketplaceSourceMode> sourceModes,
        VendorCatalogAccessMode accessMode,
        ProviderCredentialRequirement credentialRequirement,
        ProviderCacheLimit cachePolicy,
        IReadOnlyList<SourcingCatalogData> supportedCatalogData,
        IReadOnlyList<MarketplaceAutomationMode> blockedAutomationModes,
        IReadOnlyList<string> diagnostics)
    {
        ProviderId = RequireText(providerId, nameof(providerId));
        DisplayName = RequireText(displayName, nameof(displayName));
        SourceModes = ToReadOnly(sourceModes, nameof(sourceModes));
        AccessMode = accessMode;
        CredentialRequirement = credentialRequirement ?? throw new ArgumentNullException(nameof(credentialRequirement));
        CachePolicy = cachePolicy ?? throw new ArgumentNullException(nameof(cachePolicy));
        SupportedCatalogData = ToReadOnly(supportedCatalogData, nameof(supportedCatalogData));
        BlockedAutomationModes = ToReadOnly(blockedAutomationModes, nameof(blockedAutomationModes));
        Diagnostics = ToReadOnlyText(diagnostics, nameof(diagnostics));
    }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public IReadOnlyList<MarketplaceSourceMode> SourceModes { get; }

    public VendorCatalogAccessMode AccessMode { get; }

    public ProviderCredentialRequirement CredentialRequirement { get; }

    public ProviderCacheLimit CachePolicy { get; }

    public IReadOnlyList<SourcingCatalogData> SupportedCatalogData { get; }

    public IReadOnlyList<MarketplaceAutomationMode> BlockedAutomationModes { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public bool IsOfflineDescriptor => true;

    private static IReadOnlyList<T> ToReadOnly<T>(IReadOnlyList<T>? values, string parameterName)
        where T : struct, Enum
    {
        var normalized = values?.Distinct().OrderBy(static value => value).ToArray() ?? [];
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return new ReadOnlyCollection<T>(normalized);
    }

    private static IReadOnlyList<string> ToReadOnlyText(IReadOnlyList<string>? values, string parameterName)
    {
        var normalized = values?
            .Select(value => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return new ReadOnlyCollection<string>(normalized);
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
