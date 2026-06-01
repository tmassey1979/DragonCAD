namespace DragonCAD.Sourcing;

public sealed record NormalizedVendorQuote(
    string VendorName,
    string VendorPartNumber,
    string ManufacturerPartNumber,
    Money UnitPrice,
    int QuantityAvailable,
    int MinimumOrderQuantity,
    int? LeadTimeDays)
{
    public bool IsInStock => QuantityAvailable > 0;

    public VendorAvailability Availability
    {
        get
        {
            if (QuantityAvailable > 0)
            {
                return VendorAvailability.InStock;
            }

            return LeadTimeDays is > 0
                ? VendorAvailability.Backorder
                : VendorAvailability.Unavailable;
        }
    }
}
