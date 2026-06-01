namespace DragonCAD.Sourcing.Catalog.SparkFun;

public static class SparkFunSourceManifestValidator
{
    public static IReadOnlyList<SparkFunSourceManifestDiagnostic> Validate(
        SparkFunSourceManifest manifest,
        DateTimeOffset staleBeforeUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var diagnostics = new List<SparkFunSourceManifestDiagnostic>();

        foreach (var source in manifest.Sources)
        {
            AddRequiredFieldDiagnostics(source, diagnostics);
            AddStaleTimestampDiagnostic(source, staleBeforeUtc, diagnostics);
        }

        AddDuplicateSourceIdDiagnostics(manifest.Sources, diagnostics);

        return diagnostics;
    }

    private static void AddRequiredFieldDiagnostics(
        SparkFunSourceEntry source,
        List<SparkFunSourceManifestDiagnostic> diagnostics)
    {
        if (source.RepositoryUrl is null)
        {
            diagnostics.Add(Error(
                SparkFunSourceManifestDiagnosticCodes.MissingRepositoryUrl,
                $"SparkFun source '{source.Id}' is missing a repository URL.",
                source.Id));
        }

        if (string.IsNullOrWhiteSpace(source.LocalPath) && string.IsNullOrWhiteSpace(source.CacheKey))
        {
            diagnostics.Add(Error(
                SparkFunSourceManifestDiagnosticCodes.MissingLocalPathOrCacheKey,
                $"SparkFun source '{source.Id}' is missing a local path or cache key.",
                source.Id));
        }
    }

    private static void AddStaleTimestampDiagnostic(
        SparkFunSourceEntry source,
        DateTimeOffset staleBeforeUtc,
        List<SparkFunSourceManifestDiagnostic> diagnostics)
    {
        if (source.RetrievedAtUtc < staleBeforeUtc)
        {
            diagnostics.Add(new SparkFunSourceManifestDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                SparkFunSourceManifestDiagnosticCodes.StaleRetrievedTimestamp,
                $"SparkFun source '{source.Id}' was retrieved at {source.RetrievedAtUtc:O}, before stale threshold {staleBeforeUtc:O}.",
                SparkFunSourceEntry.Provider,
                source.Id));
        }
    }

    private static void AddDuplicateSourceIdDiagnostics(
        IReadOnlyList<SparkFunSourceEntry> sources,
        List<SparkFunSourceManifestDiagnostic> diagnostics)
    {
        var duplicateIds = sources
            .GroupBy(source => source.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal);

        foreach (var duplicateId in duplicateIds)
        {
            diagnostics.Add(Error(
                SparkFunSourceManifestDiagnosticCodes.DuplicateSourceId,
                $"Duplicate SparkFun source id '{duplicateId}'.",
                duplicateId));
        }
    }

    private static SparkFunSourceManifestDiagnostic Error(string code, string message, string sourceId)
    {
        return new SparkFunSourceManifestDiagnostic(
            CatalogDiagnosticSeverity.Error,
            code,
            message,
            SparkFunSourceEntry.Provider,
            sourceId);
    }
}
