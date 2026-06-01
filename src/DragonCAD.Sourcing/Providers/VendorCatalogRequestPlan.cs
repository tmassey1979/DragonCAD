namespace DragonCAD.Sourcing.Providers;

public sealed record VendorCatalogRequestPlan(
    string ProviderName,
    VendorCatalogAccessMode AccessMode,
    HttpRequestMethod Method,
    Uri Endpoint,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> Query,
    VendorRateLimitMetadata RateLimit,
    IReadOnlyList<string> ExpectedResponseFields,
    IReadOnlyList<string> Warnings)
{
    public string LogSafeSummary
    {
        get
        {
            var query = string.Join(", ", Query.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));
            var warnings = Warnings.Count == 0 ? "none" : string.Join("; ", Warnings);

            return $"{ProviderName} {Method} {Endpoint} query[{query}] warnings[{warnings}]";
        }
    }
}
