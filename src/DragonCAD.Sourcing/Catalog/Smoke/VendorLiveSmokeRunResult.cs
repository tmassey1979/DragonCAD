namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed record VendorLiveSmokeRunResult(
    string ProviderName,
    VendorLiveSmokeRunStatus Status,
    int ListingCount,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics,
    string RequestId,
    TimeSpan Elapsed,
    IReadOnlyList<string> SanitizedDiagnostics)
{
    public VendorLiveSmokeRunResult(
        string providerName,
        VendorLiveSmokeRunStatus status,
        int listingCount,
        IReadOnlyList<CatalogImportDiagnostic> diagnostics)
        : this(providerName, status, listingCount, diagnostics, string.Empty, TimeSpan.Zero, [])
    {
    }

    public static VendorLiveSmokeRunResult Disabled(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        return new VendorLiveSmokeRunResult(
            providerName,
            VendorLiveSmokeRunStatus.Disabled,
            0,
            [],
            string.Empty,
            TimeSpan.Zero,
            []);
    }

    public static VendorLiveSmokeRunResult Failed(
        string providerName,
        IReadOnlyList<CatalogImportDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new VendorLiveSmokeRunResult(
            providerName,
            VendorLiveSmokeRunStatus.Failed,
            0,
            diagnostics,
            string.Empty,
            TimeSpan.Zero,
            VendorLiveSmokeReportSanitizer.Sanitize(diagnostics));
    }

    public static VendorLiveSmokeRunResult FromCatalogResult(
        string providerName,
        CatalogImportResult catalogResult,
        string requestId = "",
        TimeSpan elapsed = default,
        IReadOnlyList<string>? redactionTerms = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(catalogResult);

        var sanitizedDiagnostics = VendorLiveSmokeReportSanitizer.Sanitize(catalogResult.Diagnostics, redactionTerms);
        var status = ResolveStatus(catalogResult.Diagnostics);

        return new VendorLiveSmokeRunResult(
            providerName,
            status,
            catalogResult.Listings.Count,
            catalogResult.Diagnostics,
            requestId,
            elapsed,
            sanitizedDiagnostics);
    }

    public string ToDeterministicReport()
    {
        var diagnostics = SanitizedDiagnostics.Count == 0
            ? "none"
            : string.Join(" | ", SanitizedDiagnostics);

        return string.Join(
            Environment.NewLine,
            $"provider={ProviderName}",
            $"request_id={RequestId}",
            $"status={Status}",
            $"elapsed_ms={Elapsed.TotalMilliseconds:0}",
            $"listing_count={ListingCount}",
            $"diagnostics={diagnostics}");
    }

    private static VendorLiveSmokeRunStatus ResolveStatus(IReadOnlyList<CatalogImportDiagnostic> diagnostics)
    {
        if (diagnostics.Any(IsRateLimitDiagnostic))
        {
            return VendorLiveSmokeRunStatus.RateLimited;
        }

        return diagnostics.Any(diagnostic => diagnostic.Severity == CatalogDiagnosticSeverity.Error)
            ? VendorLiveSmokeRunStatus.Failed
            : VendorLiveSmokeRunStatus.Succeeded;
    }

    private static bool IsRateLimitDiagnostic(CatalogImportDiagnostic diagnostic) =>
        diagnostic.Code.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
        diagnostic.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
        diagnostic.Message.Contains("rate-limit", StringComparison.OrdinalIgnoreCase) ||
        diagnostic.Message.Contains("retry-after", StringComparison.OrdinalIgnoreCase) ||
        diagnostic.Message.Contains("429", StringComparison.Ordinal);
}
