namespace DragonCAD.Sourcing.TrustedLibrary;

public sealed record TrustedLibraryVendorMatchPromotionPlan
{
    public TrustedLibraryVendorMatchPromotionPlan(IReadOnlyList<TrustedLibraryVendorMatchPromotionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        Records = records.ToArray();
    }

    public IReadOnlyList<TrustedLibraryVendorMatchPromotionRecord> Records { get; }

    public bool MutatesCoreLibrary => false;
}
