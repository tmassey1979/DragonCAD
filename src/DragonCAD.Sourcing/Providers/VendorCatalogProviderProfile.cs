namespace DragonCAD.Sourcing.Providers;

public sealed record VendorCatalogProviderProfile(
    string ProviderName,
    VendorCatalogAccessMode AccessMode,
    HttpRequestMethod QueryMethod,
    Uri QueryEndpoint,
    IReadOnlyList<string> RequiredCredentialKeys,
    VendorRateLimitMetadata RateLimit,
    IReadOnlyList<string> ExpectedResponseFields,
    string ManufacturerPartNumberField,
    string? FallbackWarning = null);
