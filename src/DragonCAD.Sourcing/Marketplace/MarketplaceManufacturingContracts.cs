using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Marketplace;

public sealed record MarketplaceBoardHandoffRequest(
    string ProjectId,
    string Revision,
    MarketplaceManufacturingCapabilities RequestedCapability,
    IReadOnlyList<MarketplaceManufacturingArtifactLink> Artifacts)
{
    public IReadOnlyList<MarketplaceManufacturingArtifactLink> Artifacts { get; } =
        new ReadOnlyCollection<MarketplaceManufacturingArtifactLink>([.. Artifacts ?? []]);
}

public sealed record MarketplacePrototypeBoardHandoffResponse(
    MarketplaceResponseMetadata Metadata,
    string HandoffId,
    Uri? HandoffUrl,
    IReadOnlyList<MarketplaceUnsupportedCapabilityDiagnostic> Diagnostics);

public sealed record MarketplaceProductionQuoteHandoffResponse(
    MarketplaceResponseMetadata Metadata,
    string QuoteRequestId,
    Uri? QuoteUrl,
    IReadOnlyList<MarketplaceUnsupportedCapabilityDiagnostic> Diagnostics);

public sealed record MarketplaceManufacturingArtifactLink(
    MarketplaceManufacturingArtifact Artifact,
    Uri Location);

public sealed record MarketplaceUnsupportedCapabilityDiagnostic(
    MarketplaceUnsupportedCapabilityKind Kind,
    string ProviderId,
    string Message,
    MarketplaceManufacturingCapabilities? Capability = null,
    MarketplaceManufacturingArtifact? Artifact = null);

public enum MarketplaceUnsupportedCapabilityKind
{
    UnsupportedManufacturingCapability,
    MissingRequiredArtifact,
}
