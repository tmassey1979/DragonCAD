using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialRequirement(
    string ProviderName,
    IReadOnlyList<string> RequiredKeyNames)
{
    public static IReadOnlyDictionary<string, ProviderCredentialRequirement> KnownProviders { get; } =
        BuildKnownProviders();

    private static IReadOnlyDictionary<string, ProviderCredentialRequirement> BuildKnownProviders()
    {
        var providers = new Dictionary<string, ProviderCredentialRequirement>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in VendorCatalogRequestPlanner.DefaultProfiles.Values)
        {
            providers[profile.ProviderName] = new ProviderCredentialRequirement(
                profile.ProviderName,
                profile.RequiredCredentialKeys.ToArray());
        }

        providers["OSH Park"] = new ProviderCredentialRequirement("OSH Park", []);
        providers["PCBCart"] = new ProviderCredentialRequirement("PCBCart", []);

        return providers;
    }
}
