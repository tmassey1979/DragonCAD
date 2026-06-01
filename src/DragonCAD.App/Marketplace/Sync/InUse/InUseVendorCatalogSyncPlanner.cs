using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;

namespace DragonCAD.App.Marketplace.Sync.InUse;

public static class InUseVendorCatalogSyncPlanner
{
    private static readonly string[] ApiProviders = ["Digi-Key", "Mouser"];

    public static IReadOnlyList<InUseVendorCatalogSyncRequest> Plan(
        IEnumerable<SchematicComponentInstance> placedParts,
        IEnumerable<ComponentManagerRow> libraryRows,
        IEnumerable<VendorCatalogSyncProviderRow> providers) =>
        Plan(placedParts, libraryRows, providers, [], DateTimeOffset.UtcNow, InUseVendorCatalogFreshnessPolicy.Default);

    public static IReadOnlyList<InUseVendorCatalogSyncRequest> Plan(
        IEnumerable<SchematicComponentInstance> placedParts,
        IEnumerable<ComponentManagerRow> libraryRows,
        IEnumerable<VendorCatalogSyncProviderRow> providers,
        IEnumerable<InUseVendorCatalogSyncState> syncStates,
        DateTimeOffset now) =>
        Plan(placedParts, libraryRows, providers, syncStates, now, InUseVendorCatalogFreshnessPolicy.Default);

    public static IReadOnlyList<InUseVendorCatalogSyncRequest> Plan(
        IEnumerable<SchematicComponentInstance> placedParts,
        IEnumerable<ComponentManagerRow> libraryRows,
        IEnumerable<VendorCatalogSyncProviderRow> providers,
        IEnumerable<InUseVendorCatalogSyncState> syncStates,
        DateTimeOffset now,
        InUseVendorCatalogFreshnessPolicy freshnessPolicy)
    {
        ArgumentNullException.ThrowIfNull(placedParts);
        ArgumentNullException.ThrowIfNull(libraryRows);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(syncStates);
        ArgumentNullException.ThrowIfNull(freshnessPolicy);

        Dictionary<string, ComponentManagerRow> componentsById = libraryRows
            .GroupBy(row => row.ComponentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        Dictionary<SyncStateKey, InUseVendorCatalogSyncState> statesByKey = syncStates
            .GroupBy(state => new SyncStateKey(state.ComponentId, state.ProviderName, state.Query), SyncStateKeyComparer.Instance)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(state => state.LastSyncedAt).First(), SyncStateKeyComparer.Instance);

        InUseComponent[] inUseComponents = placedParts
            .GroupBy(part => part.ComponentId, StringComparer.Ordinal)
            .Select(group => CreateInUseComponent(group, componentsById))
            .Where(component => component is not null)
            .Select(component => component!)
            .OrderBy(component => component.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(component => component.ComponentId, StringComparer.Ordinal)
            .ToArray();

        VendorCatalogSyncProviderRow[] apiProviders = providers
            .Where(provider => ApiProviders.Contains(provider.ProviderName, StringComparer.OrdinalIgnoreCase))
            .OrderBy(provider => Array.FindIndex(ApiProviders, name => string.Equals(name, provider.ProviderName, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        List<InUseVendorCatalogSyncRequest> requests = [];
        foreach (InUseComponent component in inUseComponents)
        {
            foreach (VendorCatalogSyncProviderRow provider in apiProviders)
            {
                SyncStateKey stateKey = new(component.ComponentId, provider.ProviderName, component.ManufacturerPartNumber);
                statesByKey.TryGetValue(stateKey, out InUseVendorCatalogSyncState? syncState);
                bool isFresh = syncState is not null && now - syncState.LastSyncedAt < freshnessPolicy.FreshnessWindowFor(provider.ProviderName);
                requests.Add(new InUseVendorCatalogSyncRequest(
                    component.ComponentId,
                    component.DisplayName,
                    component.Manufacturer,
                    component.ManufacturerPartNumber,
                    component.ReferenceDesignators,
                    provider.ProviderName,
                    component.ManufacturerPartNumber,
                    $"In use: {component.ReferenceDesignators}",
                    isFresh ? "Fresh" : provider.NextActionLabel,
                    provider.CanSync,
                    IsDue: provider.CanSync && !isFresh,
                    FormatSyncState(syncState, now)));
            }
        }

        return requests;
    }

    public static InUseVendorCatalogSyncActionSummary Summarize(IEnumerable<InUseVendorCatalogSyncRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        InUseVendorCatalogSyncRequest[] requestArray = requests.ToArray();
        int freshCount = requestArray.Count(request => IsFresh(request));
        int dueCount = requestArray.Count(request => request.IsDue);
        int blockedCount = requestArray.Count(request => !request.CanRun && !request.IsDue);

        return new InUseVendorCatalogSyncActionSummary(
            requestArray.Length,
            freshCount,
            dueCount,
            blockedCount,
            FormatFreshnessLabel(freshCount, dueCount, blockedCount),
            FormatPrimaryActionLabel(requestArray.Length, dueCount, blockedCount));
    }

    private static InUseComponent? CreateInUseComponent(
        IGrouping<string, SchematicComponentInstance> placedParts,
        IReadOnlyDictionary<string, ComponentManagerRow> componentsById)
    {
        if (!componentsById.TryGetValue(placedParts.Key, out ComponentManagerRow? component) ||
            string.IsNullOrWhiteSpace(component.ManufacturerPartNumber))
        {
            return null;
        }

        string referenceDesignators = string.Join(
            ", ",
            placedParts
                .Select(part => part.ReferenceDesignator)
                .Where(reference => !string.IsNullOrWhiteSpace(reference))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal));

        return new InUseComponent(
            component.ComponentId,
            component.DisplayName,
            component.Manufacturer,
            component.ManufacturerPartNumber,
            referenceDesignators);
    }

    private sealed record InUseComponent(
        string ComponentId,
        string DisplayName,
        string Manufacturer,
        string ManufacturerPartNumber,
        string ReferenceDesignators);

    private static string FormatSyncState(InUseVendorCatalogSyncState? syncState, DateTimeOffset now)
    {
        if (syncState is null)
        {
            return "Never synced";
        }

        TimeSpan age = now - syncState.LastSyncedAt;
        string ageText;
        if (age.TotalDays >= 1)
        {
            int days = (int)age.TotalDays;
            ageText = $"{days} {Pluralize(days, "day")} ago";
        }
        else if (age.TotalHours >= 1)
        {
            int hours = (int)age.TotalHours;
            ageText = $"{hours} {Pluralize(hours, "hour")} ago";
        }
        else
        {
            int minutes = Math.Max(0, (int)age.TotalMinutes);
            ageText = $"{minutes} {Pluralize(minutes, "minute")} ago";
        }

        return $"Synced {ageText}: {syncState.LastImportedCount} {Pluralize(syncState.LastImportedCount, "candidate")}, {syncState.LastWarningCount} {Pluralize(syncState.LastWarningCount, "warning")}";
    }

    private static bool IsFresh(InUseVendorCatalogSyncRequest request) =>
        request.CanRun &&
        !request.IsDue &&
        string.Equals(request.ActionLabel, "Fresh", StringComparison.OrdinalIgnoreCase);

    private static string FormatFreshnessLabel(int freshCount, int dueCount, int blockedCount)
    {
        if (freshCount == 0 && dueCount == 0 && blockedCount == 0)
        {
            return "No in-use catalog requests";
        }

        List<string> segments = [];
        if (freshCount > 0)
        {
            segments.Add($"{freshCount} fresh");
        }

        if (dueCount > 0)
        {
            segments.Add($"{dueCount} due");
        }

        if (blockedCount > 0)
        {
            segments.Add($"{blockedCount} blocked");
        }

        return string.Join(", ", segments);
    }

    private static string FormatPrimaryActionLabel(int totalCount, int dueCount, int blockedCount)
    {
        if (dueCount > 0)
        {
            return $"Sync {dueCount} in-use catalog {Pluralize(dueCount, "request")}";
        }

        if (blockedCount > 0)
        {
            return $"Resolve {blockedCount} blocked in-use catalog {Pluralize(blockedCount, "request")}";
        }

        if (totalCount > 0)
        {
            return "In-use catalogs fresh";
        }

        return "No in-use catalog requests";
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";

    private sealed record SyncStateKey(string ComponentId, string ProviderName, string Query);

    private sealed class SyncStateKeyComparer : IEqualityComparer<SyncStateKey>
    {
        public static SyncStateKeyComparer Instance { get; } = new();

        public bool Equals(SyncStateKey? x, SyncStateKey? y) =>
            x is not null &&
            y is not null &&
            string.Equals(x.ComponentId, y.ComponentId, StringComparison.Ordinal) &&
            string.Equals(x.ProviderName, y.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Query, y.Query, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(SyncStateKey obj) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.ComponentId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProviderName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Query));
    }
}

public sealed record InUseVendorCatalogFreshnessPolicy(
    TimeSpan DefaultFreshnessWindow,
    IReadOnlyDictionary<string, TimeSpan> ProviderFreshnessWindows)
{
    public static InUseVendorCatalogFreshnessPolicy Default { get; } = new(
        TimeSpan.FromHours(24),
        new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
        {
            ["Digi-Key"] = TimeSpan.FromHours(12),
            ["Mouser"] = TimeSpan.FromHours(24)
        });

    public TimeSpan FreshnessWindowFor(string providerName) =>
        ProviderFreshnessWindows.TryGetValue(providerName, out TimeSpan providerWindow)
            ? providerWindow
            : DefaultFreshnessWindow;
}

public sealed record InUseVendorCatalogSyncState(
    string ComponentId,
    string ProviderName,
    string Query,
    DateTimeOffset LastSyncedAt,
    int LastImportedCount,
    int LastWarningCount);

public sealed record InUseVendorCatalogSyncRequest(
    string ComponentId,
    string DisplayName,
    string Manufacturer,
    string ManufacturerPartNumber,
    string ReferenceDesignators,
    string ProviderName,
    string Query,
    string Reason,
    string ActionLabel,
    bool CanRun,
    bool IsDue,
    string SyncStateLabel);

public sealed record InUseVendorCatalogSyncActionSummary(
    int TotalCount,
    int FreshCount,
    int DueCount,
    int BlockedCount,
    string FreshnessLabel,
    string PrimaryActionLabel);
