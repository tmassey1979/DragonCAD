using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public static class OpenHardwareSourceManifestValidator
{
    public static IReadOnlyList<OpenHardwareSourceManifestDiagnostic> Validate(
        OpenHardwareSourceManifest manifest,
        DateTimeOffset staleBeforeUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var diagnostics = new List<OpenHardwareSourceManifestDiagnostic>();
        foreach (var source in manifest.Sources)
        {
            AddRequiredFieldDiagnostics(source, diagnostics);
            AddSourceModeDiagnostics(source, diagnostics);
            AddStaleTimestampDiagnostic(source, staleBeforeUtc, diagnostics);
        }

        AddDuplicateSourceRowDiagnostics(manifest.Sources, diagnostics);

        return diagnostics;
    }

    private static void AddRequiredFieldDiagnostics(
        OpenHardwareSourceEntry source,
        List<OpenHardwareSourceManifestDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(source.ProviderName))
        {
            diagnostics.Add(Error(OpenHardwareSourceManifestDiagnosticCodes.MissingProviderName, "Provider name is required.", source));
        }

        if (string.IsNullOrWhiteSpace(source.SourceId))
        {
            diagnostics.Add(Error(OpenHardwareSourceManifestDiagnosticCodes.MissingSourceId, "Source id is required.", source));
        }

        if (RequiresRepository(source.Mode) && source.RepositoryUrl is null)
        {
            diagnostics.Add(Error(
                OpenHardwareSourceManifestDiagnosticCodes.MissingRepositoryUrl,
                $"Open hardware source '{source.SourceId}' is missing a repository URL.",
                source));
        }

        if (RequiresCachedSource(source.Mode) &&
            string.IsNullOrWhiteSpace(source.LocalPath) &&
            string.IsNullOrWhiteSpace(source.CacheKey))
        {
            diagnostics.Add(Error(
                OpenHardwareSourceManifestDiagnosticCodes.MissingLocalPathOrCacheKey,
                $"Open hardware source '{source.SourceId}' is missing a local path or cache key.",
                source));
        }

        if (source.Mode == OpenHardwareSourceMode.ManualCsvFeed && string.IsNullOrWhiteSpace(source.ManualFeedName))
        {
            diagnostics.Add(Error(
                OpenHardwareSourceManifestDiagnosticCodes.MissingManualFeedName,
                $"Manual CSV source '{source.SourceId}' is missing a feed name.",
                source));
        }
    }

    private static void AddSourceModeDiagnostics(
        OpenHardwareSourceEntry source,
        List<OpenHardwareSourceManifestDiagnostic> diagnostics)
    {
        if (source.Mode == OpenHardwareSourceMode.Scrape && !source.AllowsScraping)
        {
            diagnostics.Add(Error(
                OpenHardwareSourceManifestDiagnosticCodes.UnsupportedSourceMode,
                $"Provider '{source.ProviderName}' source '{source.SourceId}' uses scraping, but scraping is not allowed unless the provider explicitly allows it.",
                source));
            return;
        }

        if (ProviderIs(source, "Jameco") && source.Mode != OpenHardwareSourceMode.ManualCsvFeed)
        {
            diagnostics.Add(Error(
                OpenHardwareSourceManifestDiagnosticCodes.UnsupportedSourceMode,
                "Jameco sources must use a curated manual CSV feed unless an approved provider mode is configured.",
                source));
        }
    }

    private static void AddStaleTimestampDiagnostic(
        OpenHardwareSourceEntry source,
        DateTimeOffset staleBeforeUtc,
        List<OpenHardwareSourceManifestDiagnostic> diagnostics)
    {
        if (source.RetrievedAtUtc < staleBeforeUtc)
        {
            diagnostics.Add(new OpenHardwareSourceManifestDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                OpenHardwareSourceManifestDiagnosticCodes.StaleRetrievedTimestamp,
                $"Open hardware source '{source.SourceId}' was retrieved at {source.RetrievedAtUtc:O}, before stale threshold {staleBeforeUtc:O}.",
                source.ProviderName,
                source.SourceId));
        }
    }

    private static void AddDuplicateSourceRowDiagnostics(
        IReadOnlyList<OpenHardwareSourceEntry> sources,
        List<OpenHardwareSourceManifestDiagnostic> diagnostics)
    {
        var duplicates = sources
            .GroupBy(source => (Provider: source.ProviderName, SourceId: source.SourceId), SourceRowKeyComparer.Instance)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(key => key.SourceId, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicate in duplicates)
        {
            diagnostics.Add(new OpenHardwareSourceManifestDiagnostic(
                CatalogDiagnosticSeverity.Error,
                OpenHardwareSourceManifestDiagnosticCodes.DuplicateSourceRow,
                $"Duplicate source row '{duplicate.SourceId}' for provider '{duplicate.Provider}'.",
                duplicate.Provider,
                duplicate.SourceId));
        }
    }

    private static bool RequiresRepository(OpenHardwareSourceMode mode)
    {
        return mode is OpenHardwareSourceMode.OpenHardwareRepository or OpenHardwareSourceMode.EagleLibrary;
    }

    private static bool RequiresCachedSource(OpenHardwareSourceMode mode)
    {
        return mode is OpenHardwareSourceMode.OpenHardwareRepository or OpenHardwareSourceMode.EagleLibrary or OpenHardwareSourceMode.ManualCsvFeed;
    }

    private static bool ProviderIs(OpenHardwareSourceEntry source, string providerName)
    {
        return string.Equals(source.ProviderName, providerName, StringComparison.OrdinalIgnoreCase);
    }

    private static OpenHardwareSourceManifestDiagnostic Error(
        string code,
        string message,
        OpenHardwareSourceEntry source)
    {
        return new OpenHardwareSourceManifestDiagnostic(
            CatalogDiagnosticSeverity.Error,
            code,
            message,
            source.ProviderName,
            source.SourceId);
    }

    private sealed class SourceRowKeyComparer : IEqualityComparer<(string Provider, string SourceId)>
    {
        public static readonly SourceRowKeyComparer Instance = new();

        public bool Equals((string Provider, string SourceId) x, (string Provider, string SourceId) y)
        {
            return string.Equals(x.Provider, y.Provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.SourceId, y.SourceId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Provider, string SourceId) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Provider),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SourceId));
        }
    }
}
