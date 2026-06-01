namespace DragonCAD.Sourcing.TrustedLibrary;

public static class TrustedLibraryVendorMatchPromotionPlanner
{
    public static TrustedLibraryVendorMatchPromotionPlan Plan(IEnumerable<ReviewedVendorCatalogMatch> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        TrustedLibraryVendorMatchPromotionRecord[] records = matches
            .Select(match => new TrustedLibraryVendorMatchPromotionRecord(
                match.ReviewState,
                match.SourceProvider,
                match.VendorSku,
                match.ManufacturerPartNumber,
                match.TargetComponentId,
                match.ArtifactPaths,
                match.Warnings))
            .OrderBy(record => record.SourceProvider, StringComparer.Ordinal)
            .ThenBy(record => record.VendorSku, StringComparer.Ordinal)
            .ThenBy(record => record.TargetComponentId)
            .ToArray();

        return new TrustedLibraryVendorMatchPromotionPlan(records);
    }
}
