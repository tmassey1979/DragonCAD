using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering.Review;

public sealed record FabricationOrderReviewPacket
{
    private FabricationOrderReviewPacket(
        FabricationProviderDescriptor provider,
        FabricationOrderMode orderMode,
        FabricationProviderReviewKind providerKind,
        FabricationReviewArtifact[] requiredArtifacts,
        FabricationReviewWarning[] reviewWarnings)
    {
        Provider = provider;
        OrderMode = orderMode;
        ProviderKind = providerKind;
        RequiredArtifacts = requiredArtifacts;
        ReviewWarnings = reviewWarnings;
    }

    public FabricationProviderDescriptor Provider { get; }

    public string ProviderDisplayName => Provider.DisplayName;

    public FabricationOrderMode OrderMode { get; }

    public FabricationProviderReviewKind ProviderKind { get; }

    public IReadOnlyList<FabricationReviewArtifact> RequiredArtifacts { get; }

    public IReadOnlyList<FabricationReviewWarning> ReviewWarnings { get; }

    public FabricationProviderSubmissionPolicy SubmissionPolicy => FabricationProviderSubmissionPolicy.ManualReviewOnly;

    public static FabricationOrderReviewPacket Create(
        FabricationProviderDescriptor provider,
        FabricationOrderMode orderMode,
        ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(manifest);

        FabricationReviewArtifact[] requiredArtifacts = provider.RequiredFileRoles
            .Select(role => CreateArtifact(role, manifest))
            .ToArray();
        FabricationReviewWarning[] warnings = requiredArtifacts
            .Where(artifact => !artifact.IsPresent)
            .Select(artifact => new FabricationReviewWarning(
                "missing-required-artifact",
                $"Review package is missing required {artifact.Role} artifact for {provider.DisplayName}.",
                artifact.Role))
            .ToArray();

        return new FabricationOrderReviewPacket(
            provider,
            orderMode,
            DetermineProviderKind(provider),
            requiredArtifacts,
            warnings);
    }

    private static FabricationReviewArtifact CreateArtifact(
        ManufacturingFileRole role,
        ManufacturingOutputManifest manifest)
    {
        ManufacturingRelativePath[] paths = manifest.Entries
            .Where(entry => entry.Role == role)
            .Select(entry => entry.RelativePath)
            .ToArray();

        return new FabricationReviewArtifact(role, paths.Length > 0, paths);
    }

    private static FabricationProviderReviewKind DetermineProviderKind(FabricationProviderDescriptor provider)
    {
        return provider.SupportedOrderModes.Contains(FabricationOrderMode.PrototypeBoard)
            && !provider.SupportedOrderModes.Contains(FabricationOrderMode.ProductionBoard)
            && !provider.SupportedOrderModes.Contains(FabricationOrderMode.AssembledBoard)
                ? FabricationProviderReviewKind.Prototype
                : FabricationProviderReviewKind.Production;
    }
}
