namespace DragonCAD.Sourcing.Providers;

public static class VendorCatalogRequestPlanner
{
    public static IReadOnlyDictionary<string, VendorCatalogProviderProfile> DefaultProfiles { get; } =
        new Dictionary<string, VendorCatalogProviderProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Digi-Key"] = new(
                ProviderName: "Digi-Key",
                AccessMode: VendorCatalogAccessMode.CredentialedApi,
                QueryMethod: HttpRequestMethod.Post,
                QueryEndpoint: new Uri("https://api.digikey.com/products/v4/search/keyword"),
                RequiredCredentialKeys: ["client_id", "client_secret"],
                RateLimit: new VendorRateLimitMetadata(60, null, RequiresManualRefresh: false, "Use configured Digi-Key API throttling and backoff policy."),
                ExpectedResponseFields: ["manufacturerPartNumber", "productUrl", "datasheetUrl", "priceBreaks", "stockQuantity"],
                ManufacturerPartNumberField: "manufacturerPartNumber"),
            ["Mouser"] = new(
                ProviderName: "Mouser",
                AccessMode: VendorCatalogAccessMode.CredentialedApi,
                QueryMethod: HttpRequestMethod.Post,
                QueryEndpoint: new Uri("https://api.mouser.com/api/v2/search/partnumber"),
                RequiredCredentialKeys: ["api_key"],
                RateLimit: new VendorRateLimitMetadata(30, null, RequiresManualRefresh: false, "Use configured Mouser API throttling and backoff policy."),
                ExpectedResponseFields: ["manufacturerPartNumber", "productUrl", "datasheet", "priceBreaks", "stockQuantity"],
                ManufacturerPartNumberField: "manufacturerPartNumber"),
            ["Adafruit"] = new(
                ProviderName: "Adafruit",
                AccessMode: VendorCatalogAccessMode.PublicProductApi,
                QueryMethod: HttpRequestMethod.Get,
                QueryEndpoint: new Uri("https://www.adafruit.com/api/products"),
                RequiredCredentialKeys: [],
                RateLimit: new VendorRateLimitMetadata(20, null, RequiresManualRefresh: false, "Public product API request planning only."),
                ExpectedResponseFields: ["manufacturerPartNumber", "productUrl", "datasheetUrl", "stockQuantity"],
                ManufacturerPartNumberField: "manufacturerPartNumber"),
            ["SparkFun"] = new(
                ProviderName: "SparkFun",
                AccessMode: VendorCatalogAccessMode.OpenHardwareRepositorySync,
                QueryMethod: HttpRequestMethod.Get,
                QueryEndpoint: new Uri("https://github.com/sparkfun"),
                RequiredCredentialKeys: [],
                RateLimit: new VendorRateLimitMetadata(10, null, RequiresManualRefresh: false, "Plan repository syncs through cached open-hardware metadata."),
                ExpectedResponseFields: ["manufacturerPartNumber", "productUrl", "datasheetUrl", "repositoryUrl", "designFilesUrl"],
                ManufacturerPartNumberField: "manufacturerPartNumber"),
            ["Jameco"] = new(
                ProviderName: "Jameco",
                AccessMode: VendorCatalogAccessMode.ManualCatalogFeedFallback,
                QueryMethod: HttpRequestMethod.Get,
                QueryEndpoint: new Uri("https://www.jameco.com/"),
                RequiredCredentialKeys: [],
                RateLimit: new VendorRateLimitMetadata(null, null, RequiresManualRefresh: true, "Use approved manual catalog feed imports unless a vendor API agreement is configured."),
                ExpectedResponseFields: ["manufacturerPartNumber", "productUrl", "datasheetUrl", "priceBreaks", "stockQuantity"],
                ManufacturerPartNumberField: "manufacturerPartNumber",
                FallbackWarning: "Jameco uses a manual catalog feed fallback; do not plan live scraping without explicit permission."),
        };

    public static VendorCatalogRequestPlan BuildQueryByManufacturerPartNumber(
        string providerName,
        string manufacturerPartNumber,
        VendorCredentialBag credentials)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturerPartNumber);
        ArgumentNullException.ThrowIfNull(credentials);

        if (!DefaultProfiles.TryGetValue(providerName, out var profile))
        {
            throw new ArgumentException($"Unknown sourcing provider '{providerName}'.", nameof(providerName));
        }

        var headers = BuildHeaders(profile, credentials);
        var query = BuildQuery(profile, manufacturerPartNumber.Trim(), credentials);
        var warnings = profile.FallbackWarning is null ? [] : new[] { profile.FallbackWarning };

        return new VendorCatalogRequestPlan(
            profile.ProviderName,
            profile.AccessMode,
            profile.QueryMethod,
            profile.QueryEndpoint,
            headers,
            query,
            profile.RateLimit,
            profile.ExpectedResponseFields,
            warnings);
    }

    private static IReadOnlyDictionary<string, string> BuildHeaders(
        VendorCatalogProviderProfile profile,
        VendorCredentialBag credentials)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (profile.ProviderName.Equals("Digi-Key", StringComparison.OrdinalIgnoreCase))
        {
            headers["X-DIGIKEY-Client-Id"] = credentials.GetPlaceholder("client_id");
            headers["Authorization"] = "<runtime-oauth-token>";
        }

        return headers;
    }

    private static IReadOnlyDictionary<string, string> BuildQuery(
        VendorCatalogProviderProfile profile,
        string manufacturerPartNumber,
        VendorCredentialBag credentials)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [profile.ManufacturerPartNumberField] = manufacturerPartNumber,
        };

        if (profile.ProviderName.Equals("Mouser", StringComparison.OrdinalIgnoreCase))
        {
            query["apiKey"] = credentials.GetPlaceholder("api_key");
        }

        return query;
    }
}
