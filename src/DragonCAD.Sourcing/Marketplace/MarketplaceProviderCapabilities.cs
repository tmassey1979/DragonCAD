using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Marketplace;

public sealed record MarketplaceProviderCapabilities
{
    public MarketplaceProviderCapabilities(
        string ProviderId,
        string DisplayName,
        MarketplaceCatalogCapabilities Catalog,
        MarketplaceManufacturingCapabilities Manufacturing,
        MarketplaceProviderTerms Terms,
        IReadOnlyList<MarketplaceManufacturingArtifact> RequiredArtifacts)
    {
        this.ProviderId = MarketplaceContractText.Require(ProviderId, nameof(ProviderId));
        this.DisplayName = MarketplaceContractText.Require(DisplayName, nameof(DisplayName));
        this.Catalog = Catalog;
        this.Manufacturing = Manufacturing;
        this.Terms = Terms;
        this.RequiredArtifacts = new ReadOnlyCollection<MarketplaceManufacturingArtifact>(
            [.. RequiredArtifacts ?? []]);
    }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public MarketplaceCatalogCapabilities Catalog { get; }

    public MarketplaceManufacturingCapabilities Manufacturing { get; }

    public MarketplaceProviderTerms Terms { get; }

    public IReadOnlyList<MarketplaceManufacturingArtifact> RequiredArtifacts { get; }

    public bool SupportsCatalog(MarketplaceCatalogCapabilities capability)
    {
        return Catalog.HasFlag(capability);
    }

    public bool SupportsManufacturing(MarketplaceManufacturingCapabilities capability)
    {
        return Manufacturing.HasFlag(capability);
    }
}
