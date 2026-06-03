namespace DragonCAD.Sourcing.Marketplace;

public static class MarketplaceManufacturingContractValidator
{
    public static IReadOnlyList<MarketplaceUnsupportedCapabilityDiagnostic> Validate(
        MarketplaceBoardHandoffRequest request,
        MarketplaceProviderCapabilities provider)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(provider);

        var diagnostics = new List<MarketplaceUnsupportedCapabilityDiagnostic>();
        if (!provider.SupportsManufacturing(request.RequestedCapability))
        {
            diagnostics.Add(new MarketplaceUnsupportedCapabilityDiagnostic(
                MarketplaceUnsupportedCapabilityKind.UnsupportedManufacturingCapability,
                provider.ProviderId,
                $"{provider.DisplayName} does not support {Describe(request.RequestedCapability)}.",
                request.RequestedCapability));
        }

        var submittedArtifacts = request.Artifacts
            .Select(artifact => artifact.Artifact)
            .ToHashSet();

        foreach (var requiredArtifact in provider.RequiredArtifacts)
        {
            if (!submittedArtifacts.Contains(requiredArtifact))
            {
                diagnostics.Add(new MarketplaceUnsupportedCapabilityDiagnostic(
                    MarketplaceUnsupportedCapabilityKind.MissingRequiredArtifact,
                    provider.ProviderId,
                    $"{provider.DisplayName} requires {Describe(requiredArtifact)} for board handoff.",
                    Artifact: requiredArtifact));
            }
        }

        return diagnostics;
    }

    private static string Describe(MarketplaceManufacturingCapabilities capability)
    {
        return capability switch
        {
            MarketplaceManufacturingCapabilities.PrototypeBoardHandoff => "prototype board handoff",
            MarketplaceManufacturingCapabilities.ProductionQuoteHandoff => "production quote handoff",
            _ => capability.ToString(),
        };
    }

    private static string Describe(MarketplaceManufacturingArtifact artifact)
    {
        return artifact switch
        {
            MarketplaceManufacturingArtifact.Gerbers => "Gerber files",
            MarketplaceManufacturingArtifact.DrillFiles => "drill files",
            MarketplaceManufacturingArtifact.BillOfMaterials => "bill of materials",
            MarketplaceManufacturingArtifact.PickAndPlace => "pick-and-place files",
            MarketplaceManufacturingArtifact.BoardStackup => "board stackup",
            MarketplaceManufacturingArtifact.AssemblyDrawing => "assembly drawing",
            MarketplaceManufacturingArtifact.FabricationDrawing => "fabrication drawing",
            _ => artifact.ToString(),
        };
    }
}
