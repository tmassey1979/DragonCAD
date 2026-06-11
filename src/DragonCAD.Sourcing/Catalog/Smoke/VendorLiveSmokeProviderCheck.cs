namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed record VendorLiveSmokeProviderCheck(
    string ProviderName,
    string Query,
    int Limit,
    VendorLiveSmokeMode Mode,
    VendorLiveSmokeProviderStatus Status,
    string CredentialSummary,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> RedactionTerms);
