namespace DragonCAD.App.Marketplace.Sync;

public sealed class VendorCatalogSyncDashboardViewModel
{
    private static readonly string[] ProviderOrder =
    [
        "Digi-Key",
        "Mouser",
        "Adafruit",
        "SparkFun",
        "Jameco"
    ];

    private VendorCatalogSyncDashboardViewModel(
        IReadOnlyList<VendorCatalogSyncProviderRow> providers,
        string runReadinessSummary,
        string nextActionSummary)
    {
        Providers = providers;
        RunReadinessSummary = runReadinessSummary;
        NextActionSummary = nextActionSummary;
    }

    public IReadOnlyList<VendorCatalogSyncProviderRow> Providers { get; }

    public string RunReadinessSummary { get; }

    public string NextActionSummary { get; }

    public static VendorCatalogSyncDashboardViewModel FromStatuses(
        DateTimeOffset now,
        IEnumerable<VendorCatalogSyncStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        VendorCatalogSyncProviderRow[] providers = statuses
            .OrderBy(status => ProviderSortIndex(status.ProviderName))
            .ThenBy(status => status.ProviderName, StringComparer.OrdinalIgnoreCase)
            .Select(status => VendorCatalogSyncProviderRow.FromStatus(now, status))
            .ToArray();

        return new VendorCatalogSyncDashboardViewModel(
            providers,
            FormatRunReadinessSummary(providers),
            FormatNextActionSummary(providers));
    }

    private static int ProviderSortIndex(string providerName)
    {
        int index = Array.FindIndex(ProviderOrder, provider => string.Equals(provider, providerName, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }

    private static string FormatRunReadinessSummary(IReadOnlyCollection<VendorCatalogSyncProviderRow> providers)
    {
        int readyCount = providers.Count(provider => provider.CanSync);
        int disabledCount = providers.Count(provider => !provider.IsEnabled);
        int setupCount = providers.Count(provider => provider.IsEnabled && !provider.CanSync);

        return $"{readyCount} ready, {setupCount} need setup, {disabledCount} disabled";
    }

    private static string FormatNextActionSummary(IReadOnlyCollection<VendorCatalogSyncProviderRow> providers)
    {
        VendorCatalogSyncProviderRow[] credentialBlockedProviders = providers
            .Where(provider => provider.IsEnabled
                && !provider.CanSync
                && string.Equals(provider.NextActionLabel, "Add API credentials", StringComparison.Ordinal))
            .ToArray();

        if (credentialBlockedProviders.Length > 0)
        {
            return $"Add API credentials for {JoinProviderNames(credentialBlockedProviders)}";
        }

        int readyCount = providers.Count(provider => provider.CanSync);
        if (readyCount > 0)
        {
            return $"Run sync for {readyCount} ready {Pluralize(readyCount, "provider")}";
        }

        if (providers.Any(provider => !provider.IsEnabled))
        {
            return "Enable a provider to run catalog sync";
        }

        return "Set up a vendor provider before syncing";
    }

    private static string JoinProviderNames(IReadOnlyList<VendorCatalogSyncProviderRow> providers)
    {
        if (providers.Count == 1)
        {
            return providers[0].ProviderName;
        }

        if (providers.Count == 2)
        {
            return $"{providers[0].ProviderName} and {providers[1].ProviderName}";
        }

        return $"{string.Join(", ", providers.Take(providers.Count - 1).Select(provider => provider.ProviderName))}, and {providers[^1].ProviderName}";
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record VendorCatalogSyncStatus(
    string ProviderName,
    bool IsEnabled,
    CatalogCredentialState CredentialState,
    DateTimeOffset? LastSync,
    int ImportedCount,
    int LinkedCount,
    int WarningCount);

public enum CatalogCredentialState
{
    Missing,
    Configured,
    NotRequired,
    NotSupported
}

public sealed record VendorCatalogSyncProviderRow(
    string ProviderName,
    bool IsEnabled,
    string CredentialStatus,
    string LastSyncStatus,
    string NextActionLabel,
    string Warning,
    string ResultSummary,
    bool CanSync)
{
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;

    public static VendorCatalogSyncProviderRow FromStatus(DateTimeOffset now, VendorCatalogSyncStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        string credentialStatus = FormatCredentialStatus(status);
        string warning = FormatWarning(status);
        bool canSync = status.IsEnabled && status.CredentialState is CatalogCredentialState.Configured or CatalogCredentialState.NotRequired;

        return new VendorCatalogSyncProviderRow(
            ProviderName: status.ProviderName,
            IsEnabled: status.IsEnabled,
            CredentialStatus: credentialStatus,
            LastSyncStatus: FormatLastSync(now, status.LastSync),
            NextActionLabel: FormatNextAction(status),
            Warning: warning,
            ResultSummary: FormatResultSummary(status),
            CanSync: canSync);
    }

    private static string FormatCredentialStatus(VendorCatalogSyncStatus status)
    {
        if (ProviderComparer.Equals(status.ProviderName, "Jameco"))
        {
            return "Manual feed";
        }

        return status.CredentialState switch
        {
            CatalogCredentialState.Missing => "Credential missing",
            CatalogCredentialState.Configured => "Credential configured",
            CatalogCredentialState.NotRequired when ProviderComparer.Equals(status.ProviderName, "SparkFun") => "Public source",
            CatalogCredentialState.NotRequired => "Public catalog",
            CatalogCredentialState.NotSupported => "Manual feed",
            _ => "Unknown"
        };
    }

    private static string FormatWarning(VendorCatalogSyncStatus status)
    {
        if ((ProviderComparer.Equals(status.ProviderName, "Digi-Key") || ProviderComparer.Equals(status.ProviderName, "Mouser"))
            && status.CredentialState == CatalogCredentialState.Missing)
        {
            return $"{status.ProviderName} catalog sync requires API credentials before product, price, and stock data can be refreshed.";
        }

        if (ProviderComparer.Equals(status.ProviderName, "Jameco"))
        {
            return "Jameco sync is limited to manual/feed imports until an official catalog API is configured.";
        }

        return string.Empty;
    }

    private static string FormatNextAction(VendorCatalogSyncStatus status)
    {
        if (!status.IsEnabled)
        {
            return "Disabled";
        }

        if ((ProviderComparer.Equals(status.ProviderName, "Digi-Key") || ProviderComparer.Equals(status.ProviderName, "Mouser"))
            && status.CredentialState == CatalogCredentialState.Missing)
        {
            return "Add API credentials";
        }

        if (ProviderComparer.Equals(status.ProviderName, "Jameco"))
        {
            return "Import vendor feed";
        }

        if (ProviderComparer.Equals(status.ProviderName, "SparkFun"))
        {
            return "Refresh source libraries";
        }

        if (ProviderComparer.Equals(status.ProviderName, "Adafruit"))
        {
            return "Sync public catalog";
        }

        return "Sync now";
    }

    private static string FormatLastSync(DateTimeOffset now, DateTimeOffset? lastSync)
    {
        if (lastSync is null)
        {
            return "Never synced";
        }

        TimeSpan age = now - lastSync.Value;
        if (age.TotalDays >= 7)
        {
            return $"Stale: {(int)age.TotalDays} days ago";
        }

        if (age.TotalDays >= 1)
        {
            int days = (int)age.TotalDays;
            return $"Last synced {days} {Pluralize(days, "day")} ago";
        }

        if (age.TotalHours >= 1)
        {
            int hours = (int)age.TotalHours;
            return $"Last synced {hours} {Pluralize(hours, "hour")} ago";
        }

        int minutes = Math.Max(0, (int)age.TotalMinutes);
        return $"Last synced {minutes} {Pluralize(minutes, "minute")} ago";
    }

    private static string FormatResultSummary(VendorCatalogSyncStatus status)
    {
        string warningLabel = status.WarningCount == 1 ? "warning" : "warnings";
        return $"{status.ImportedCount:N0} imported, {status.LinkedCount:N0} linked, {status.WarningCount:N0} {warningLabel}";
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}
