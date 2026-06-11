namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomBuildCostVendorQuote
{
    public BomBuildCostVendorQuote(
        string canonicalIdentity,
        string selectedValue,
        string package,
        string manufacturerPartNumber,
        string vendorName,
        string vendorPartNumber,
        bool isPreferredVendor,
        int stock,
        int minimumOrderQuantity,
        int orderMultiple,
        int leadTimeDays,
        BomPartLifecycle lifecycle,
        DateTimeOffset capturedAt,
        PriceLadder priceLadder)
    {
        if (string.IsNullOrWhiteSpace(canonicalIdentity))
        {
            throw new ArgumentException("Canonical identity is required.", nameof(canonicalIdentity));
        }

        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            throw new ArgumentException("Selected value is required.", nameof(selectedValue));
        }

        if (string.IsNullOrWhiteSpace(package))
        {
            throw new ArgumentException("Package is required.", nameof(package));
        }

        if (string.IsNullOrWhiteSpace(manufacturerPartNumber))
        {
            throw new ArgumentException("Manufacturer part number is required.", nameof(manufacturerPartNumber));
        }

        if (string.IsNullOrWhiteSpace(vendorName))
        {
            throw new ArgumentException("Vendor name is required.", nameof(vendorName));
        }

        if (string.IsNullOrWhiteSpace(vendorPartNumber))
        {
            throw new ArgumentException("Vendor part number is required.", nameof(vendorPartNumber));
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), stock, "Stock cannot be negative.");
        }

        if (minimumOrderQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrderQuantity), minimumOrderQuantity, "Minimum order quantity must be greater than zero.");
        }

        if (orderMultiple <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderMultiple), orderMultiple, "Order multiple must be greater than zero.");
        }

        if (leadTimeDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leadTimeDays), leadTimeDays, "Lead time cannot be negative.");
        }

        CanonicalIdentity = canonicalIdentity.Trim();
        SelectedValue = selectedValue.Trim();
        Package = package.Trim();
        ManufacturerPartNumber = manufacturerPartNumber.Trim();
        VendorName = vendorName.Trim();
        VendorPartNumber = vendorPartNumber.Trim();
        IsPreferredVendor = isPreferredVendor;
        Stock = stock;
        MinimumOrderQuantity = minimumOrderQuantity;
        OrderMultiple = orderMultiple;
        LeadTimeDays = leadTimeDays;
        Lifecycle = lifecycle;
        CapturedAt = capturedAt;
        PriceLadder = priceLadder ?? throw new ArgumentNullException(nameof(priceLadder));
    }

    public string CanonicalIdentity { get; }

    public string SelectedValue { get; }

    public string Package { get; }

    public string ManufacturerPartNumber { get; }

    public string VendorName { get; }

    public string VendorPartNumber { get; }

    public bool IsPreferredVendor { get; }

    public int Stock { get; }

    public int MinimumOrderQuantity { get; }

    public int OrderMultiple { get; }

    public int LeadTimeDays { get; }

    public BomPartLifecycle Lifecycle { get; }

    public DateTimeOffset CapturedAt { get; }

    public PriceLadder PriceLadder { get; }
}
