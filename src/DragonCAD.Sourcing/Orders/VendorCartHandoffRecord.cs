namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartHandoffRecord(
    string BomLineId,
    string ProviderId,
    VendorCartSupportMode SupportMode,
    string VendorPartNumber,
    int Quantity,
    string SourceOfferId,
    Money UnitPriceSnapshot,
    Money ExtendedPriceSnapshot,
    DateTimeOffset PriceSnapshotAt,
    DateTimeOffset CreatedAt,
    VendorCartReviewStatus ReviewStatus);
