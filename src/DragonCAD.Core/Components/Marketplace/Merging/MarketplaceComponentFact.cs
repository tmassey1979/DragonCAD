namespace DragonCAD.Core.Components.Marketplace.Merging;

public sealed record MarketplaceComponentFact
{
    public MarketplaceComponentFact(
        string VendorName,
        string VendorSku,
        string Manufacturer,
        string ManufacturerPartNumber,
        string DisplayName,
        string ProductUrl,
        string Kind,
        string Value,
        string Package,
        string Tolerance)
    {
        this.VendorName = CanonicalComponentKey.NormalizeRequired(VendorName, nameof(VendorName));
        this.VendorSku = CanonicalComponentKey.NormalizeRequired(VendorSku, nameof(VendorSku));
        this.Manufacturer = CanonicalComponentKey.NormalizeOptional(Manufacturer);
        this.ManufacturerPartNumber = CanonicalComponentKey.NormalizeOptional(ManufacturerPartNumber);
        this.DisplayName = CanonicalComponentKey.NormalizeOptional(DisplayName);
        this.ProductUrl = CanonicalComponentKey.NormalizeOptional(ProductUrl);
        this.Kind = CanonicalComponentKey.NormalizeRequired(Kind, nameof(Kind));
        this.Value = CanonicalComponentKey.NormalizeOptional(Value);
        this.Package = CanonicalComponentKey.NormalizeOptional(Package);
        this.Tolerance = CanonicalComponentKey.NormalizeOptional(Tolerance);
    }

    public string VendorName { get; }

    public string VendorSku { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string DisplayName { get; }

    public string ProductUrl { get; }

    public string Kind { get; }

    public string Value { get; }

    public string Package { get; }

    public string Tolerance { get; }
}
