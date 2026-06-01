using DragonCAD.App.Marketplace.Sync;

namespace DragonCAD.App.Marketplace.Sync.Planning;

public static class VendorCatalogSyncRunPlanner
{
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;

    public static VendorCatalogSyncRunPlan Plan(VendorCatalogSyncProviderRow provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!provider.IsEnabled)
        {
            return Blocked(
                provider,
                $"{provider.ProviderName} catalog sync is disabled.",
                requiresCredential: false);
        }

        if (ProviderComparer.Equals(provider.ProviderName, "Jameco"))
        {
            return new VendorCatalogSyncRunPlan(
                ProviderName: provider.ProviderName,
                Status: VendorCatalogSyncRunPlanStatus.Ready,
                ActionKind: VendorCatalogSyncRunActionKind.ManualFeedImport,
                ActionLabel: "Import Jameco vendor feed",
                Summary: "Prepare Jameco manual/feed import; user-selected catalog file is required before execution.",
                Diagnostic: string.Empty,
                RequiresCredential: false,
                RequiresUserFile: true);
        }

        if (RequiresApiCredential(provider.ProviderName))
        {
            if (!provider.CanSync || provider.CredentialStatus.Equals("Credential missing", StringComparison.OrdinalIgnoreCase))
            {
                return Blocked(
                    provider,
                    $"{provider.ProviderName} catalog sync requires API credentials.",
                    requiresCredential: true);
            }

            return new VendorCatalogSyncRunPlan(
                ProviderName: provider.ProviderName,
                Status: VendorCatalogSyncRunPlanStatus.Ready,
                ActionKind: VendorCatalogSyncRunActionKind.ApiCatalogSync,
                ActionLabel: $"Sync {provider.ProviderName} catalog",
                Summary: $"Prepare {provider.ProviderName} API catalog import request; no network call is executed by the planner.",
                Diagnostic: string.Empty,
                RequiresCredential: true,
                RequiresUserFile: false);
        }

        if (ProviderComparer.Equals(provider.ProviderName, "SparkFun"))
        {
            return new VendorCatalogSyncRunPlan(
                ProviderName: provider.ProviderName,
                Status: VendorCatalogSyncRunPlanStatus.Ready,
                ActionKind: VendorCatalogSyncRunActionKind.SourceLibrarySync,
                ActionLabel: "Refresh SparkFun source libraries",
                Summary: "Prepare SparkFun source-library import from configured local/source package cache.",
                Diagnostic: string.Empty,
                RequiresCredential: false,
                RequiresUserFile: false);
        }

        if (ProviderComparer.Equals(provider.ProviderName, "Adafruit"))
        {
            return new VendorCatalogSyncRunPlan(
                ProviderName: provider.ProviderName,
                Status: VendorCatalogSyncRunPlanStatus.Ready,
                ActionKind: VendorCatalogSyncRunActionKind.PublicCatalogSync,
                ActionLabel: "Sync Adafruit public catalog",
                Summary: "Prepare Adafruit public catalog import; no network call is executed by the planner.",
                Diagnostic: string.Empty,
                RequiresCredential: false,
                RequiresUserFile: false);
        }

        if (!provider.CanSync)
        {
            return Blocked(
                provider,
                $"{provider.ProviderName} catalog sync is not ready.",
                requiresCredential: false);
        }

        return new VendorCatalogSyncRunPlan(
            ProviderName: provider.ProviderName,
            Status: VendorCatalogSyncRunPlanStatus.Ready,
            ActionKind: VendorCatalogSyncRunActionKind.PublicCatalogSync,
            ActionLabel: $"Sync {provider.ProviderName} catalog",
            Summary: $"Prepare {provider.ProviderName} catalog import; no network call is executed by the planner.",
            Diagnostic: string.Empty,
            RequiresCredential: false,
            RequiresUserFile: false);
    }

    private static VendorCatalogSyncRunPlan Blocked(
        VendorCatalogSyncProviderRow provider,
        string diagnostic,
        bool requiresCredential) =>
        new(
            ProviderName: provider.ProviderName,
            Status: VendorCatalogSyncRunPlanStatus.Blocked,
            ActionKind: VendorCatalogSyncRunActionKind.None,
            ActionLabel: provider.NextActionLabel,
            Summary: string.Empty,
            Diagnostic: diagnostic,
            RequiresCredential: requiresCredential,
            RequiresUserFile: false);

    private static bool RequiresApiCredential(string providerName) =>
        ProviderComparer.Equals(providerName, "Digi-Key")
        || ProviderComparer.Equals(providerName, "Mouser");
}

public sealed record VendorCatalogSyncRunPlan(
    string ProviderName,
    VendorCatalogSyncRunPlanStatus Status,
    VendorCatalogSyncRunActionKind ActionKind,
    string ActionLabel,
    string Summary,
    string Diagnostic,
    bool RequiresCredential,
    bool RequiresUserFile);

public enum VendorCatalogSyncRunPlanStatus
{
    Ready,
    Blocked
}

public enum VendorCatalogSyncRunActionKind
{
    None,
    ApiCatalogSync,
    PublicCatalogSync,
    SourceLibrarySync,
    ManualFeedImport
}
