using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationProviderProfile
{
    public FabricationProviderProfile(
        string providerId,
        FabricationProviderKind providerKind,
        int minimumQuantity,
        int maximumQuantity,
        IEnumerable<int> supportedLayerCounts,
        IEnumerable<ManufacturingFileRole> boardPackageRequiredRoles,
        IEnumerable<ManufacturingFileRole> assemblyPackageRequiredRoles)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider id must not be empty.", nameof(providerId));
        }

        if (minimumQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumQuantity), minimumQuantity, "Minimum quantity must be at least 1.");
        }

        if (maximumQuantity < minimumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumQuantity), maximumQuantity, "Maximum quantity must be greater than or equal to the minimum quantity.");
        }

        ArgumentNullException.ThrowIfNull(supportedLayerCounts);
        ArgumentNullException.ThrowIfNull(boardPackageRequiredRoles);
        ArgumentNullException.ThrowIfNull(assemblyPackageRequiredRoles);

        int[] layers = supportedLayerCounts.Distinct().Order().ToArray();
        if (layers.Any(layer => layer < 1))
        {
            throw new ArgumentOutOfRangeException(nameof(supportedLayerCounts), "Supported layer counts must be at least 1.");
        }

        ProviderId = providerId.Trim();
        ProviderKind = providerKind;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        SupportedLayerCounts = layers;
        BoardPackageRequiredRoles = boardPackageRequiredRoles.Distinct().Order().ToArray();
        AssemblyPackageRequiredRoles = assemblyPackageRequiredRoles.Distinct().Order().ToArray();
    }

    public string ProviderId { get; }

    public FabricationProviderKind ProviderKind { get; }

    public int MinimumQuantity { get; }

    public int MaximumQuantity { get; }

    public IReadOnlyList<int> SupportedLayerCounts { get; }

    public IReadOnlyList<ManufacturingFileRole> BoardPackageRequiredRoles { get; }

    public IReadOnlyList<ManufacturingFileRole> AssemblyPackageRequiredRoles { get; }

    public static FabricationProviderProfile Unrestricted(
        string providerId,
        IEnumerable<ManufacturingFileRole> boardPackageRequiredRoles)
    {
        return new FabricationProviderProfile(
            providerId,
            FabricationProviderKind.Production,
            minimumQuantity: 1,
            maximumQuantity: int.MaxValue,
            supportedLayerCounts: [],
            boardPackageRequiredRoles,
            assemblyPackageRequiredRoles: []);
    }
}
