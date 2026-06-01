namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomOrderVendorOffer
{
    public BomOrderVendorOffer(
        string vendorName,
        string vendorPartNumber,
        string manufacturerPartNumber,
        int quantityAvailable,
        int minimumOrderQuantity,
        int orderMultiple,
        PriceLadder priceLadder)
    {
        if (string.IsNullOrWhiteSpace(vendorName))
        {
            throw new ArgumentException("Vendor name is required.", nameof(vendorName));
        }

        if (string.IsNullOrWhiteSpace(vendorPartNumber))
        {
            throw new ArgumentException("Vendor part number is required.", nameof(vendorPartNumber));
        }

        if (string.IsNullOrWhiteSpace(manufacturerPartNumber))
        {
            throw new ArgumentException("Manufacturer part number is required.", nameof(manufacturerPartNumber));
        }

        if (quantityAvailable < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityAvailable), quantityAvailable, "Available quantity cannot be negative.");
        }

        if (minimumOrderQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrderQuantity), minimumOrderQuantity, "Minimum order quantity must be greater than zero.");
        }

        if (orderMultiple <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderMultiple), orderMultiple, "Order multiple must be greater than zero.");
        }

        VendorName = vendorName.Trim();
        VendorPartNumber = vendorPartNumber.Trim();
        ManufacturerPartNumber = manufacturerPartNumber.Trim();
        QuantityAvailable = quantityAvailable;
        MinimumOrderQuantity = minimumOrderQuantity;
        OrderMultiple = orderMultiple;
        PriceLadder = priceLadder ?? throw new ArgumentNullException(nameof(priceLadder));
    }

    public string VendorName { get; }

    public string VendorPartNumber { get; }

    public string ManufacturerPartNumber { get; }

    public int QuantityAvailable { get; }

    public int MinimumOrderQuantity { get; }

    public int OrderMultiple { get; }

    public PriceLadder PriceLadder { get; }
}
