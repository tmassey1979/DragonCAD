namespace DragonCAD.Sourcing.Marketplace;

public sealed record MarketplaceResponseMetadata
{
    public MarketplaceResponseMetadata(
        string SourceVendor,
        string? SourceId,
        Uri? SourceUrl,
        DateTimeOffset RetrievedAt,
        MarketplaceProviderTerms Terms,
        MarketplaceCatalogCapabilities CapabilityFlags)
    {
        this.SourceVendor = MarketplaceContractText.Require(SourceVendor, nameof(SourceVendor));
        this.SourceId = MarketplaceContractText.Normalize(SourceId);
        this.SourceUrl = SourceUrl;
        this.RetrievedAt = RetrievedAt;
        this.Terms = Terms;
        this.CapabilityFlags = CapabilityFlags;
    }

    public string SourceVendor { get; }

    public string SourceId { get; }

    public Uri? SourceUrl { get; }

    public DateTimeOffset RetrievedAt { get; }

    public MarketplaceProviderTerms Terms { get; }

    public MarketplaceCatalogCapabilities CapabilityFlags { get; }

    public bool HasCatalogCapability(MarketplaceCatalogCapabilities capability)
    {
        return CapabilityFlags.HasFlag(capability);
    }
}
