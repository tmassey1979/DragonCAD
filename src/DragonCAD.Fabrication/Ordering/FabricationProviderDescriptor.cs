using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationProviderDescriptor
{
    public FabricationProviderDescriptor(
        string id,
        string displayName,
        IEnumerable<FabricationOrderMode> supportedOrderModes,
        IEnumerable<FabricationHandoffType> supportedHandoffTypes,
        IEnumerable<ManufacturingFileRole> requiredFileRoles,
        FabricationProviderProfile? profile = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Provider id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Provider display name must not be empty.", nameof(displayName));
        }

        ArgumentNullException.ThrowIfNull(supportedOrderModes);
        ArgumentNullException.ThrowIfNull(supportedHandoffTypes);
        ArgumentNullException.ThrowIfNull(requiredFileRoles);

        Id = id.Trim();
        DisplayName = displayName.Trim();
        SupportedOrderModes = supportedOrderModes.Distinct().Order().ToArray();
        SupportedHandoffTypes = supportedHandoffTypes.Distinct().Order().ToArray();
        RequiredFileRoles = requiredFileRoles.Distinct().Order().ToArray();
        Profile = profile ?? FabricationProviderProfile.Unrestricted(Id, RequiredFileRoles);

        if (!string.Equals(Profile.ProviderId, Id, StringComparison.Ordinal))
        {
            throw new ArgumentException("Provider profile id must match descriptor id.", nameof(profile));
        }
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<FabricationOrderMode> SupportedOrderModes { get; }

    public IReadOnlyList<FabricationHandoffType> SupportedHandoffTypes { get; }

    public IReadOnlyList<ManufacturingFileRole> RequiredFileRoles { get; }

    public FabricationProviderProfile Profile { get; }
}
