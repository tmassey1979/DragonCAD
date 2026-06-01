using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Sourcing.TrustedLibrary;

public enum TrustedLibraryMatchReviewState
{
    PendingReview,
    Approved,
    Rejected,
}

public sealed record ReviewedVendorCatalogMatch
{
    public ReviewedVendorCatalogMatch(
        TrustedLibraryMatchReviewState reviewState,
        string sourceProvider,
        string vendorSku,
        string manufacturerPartNumber,
        ComponentId targetComponentId,
        IReadOnlyList<TrustedLibraryArtifactPath> artifactPaths,
        IReadOnlyList<string> warnings)
    {
        ReviewState = reviewState;
        SourceProvider = TrustedLibraryPromotionText.Require(sourceProvider, nameof(sourceProvider));
        VendorSku = TrustedLibraryPromotionText.Require(vendorSku, nameof(vendorSku));
        ManufacturerPartNumber = TrustedLibraryPromotionText.Require(manufacturerPartNumber, nameof(manufacturerPartNumber));
        TargetComponentId = targetComponentId;
        ArtifactPaths = TrustedLibraryPromotionText.SortArtifacts(artifactPaths);
        Warnings = TrustedLibraryPromotionText.SortWarnings(warnings);
    }

    public TrustedLibraryMatchReviewState ReviewState { get; }

    public string SourceProvider { get; }

    public string VendorSku { get; }

    public string ManufacturerPartNumber { get; }

    public ComponentId TargetComponentId { get; }

    public IReadOnlyList<TrustedLibraryArtifactPath> ArtifactPaths { get; }

    public IReadOnlyList<string> Warnings { get; }
}
