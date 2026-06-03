using DragonCAD.Sourcing.Marketplace;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

public sealed record ApiBackedProviderDiagnostics(
    string ProviderName,
    VendorRateLimitMetadata RateLimit,
    IReadOnlyList<string> CredentialKeys,
    MarketplaceProviderTerms Terms,
    string TermsSummary);
