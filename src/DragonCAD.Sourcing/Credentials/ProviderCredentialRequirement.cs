using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialRequirement(
    string ProviderName,
    IReadOnlyList<string> RequiredKeyNames)
{
    public static IReadOnlyDictionary<string, ProviderCredentialRequirement> KnownProviders { get; } =
        VendorCatalogRequestPlanner.DefaultProfiles.ToDictionary(
            profile => profile.Key,
            profile => new ProviderCredentialRequirement(
                profile.Value.ProviderName,
                profile.Value.RequiredCredentialKeys.ToArray()),
            StringComparer.OrdinalIgnoreCase);
}
