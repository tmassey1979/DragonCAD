namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartOffer
{
    public VendorCartOffer(
        string offerId,
        string providerId,
        string? vendorPartNumber,
        Money unitPrice,
        DateTimeOffset quotedAt)
    {
        if (string.IsNullOrWhiteSpace(offerId))
        {
            throw new ArgumentException("Offer id is required.", nameof(offerId));
        }

        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider id is required.", nameof(providerId));
        }

        OfferId = offerId.Trim();
        ProviderId = providerId.Trim();
        VendorPartNumber = string.IsNullOrWhiteSpace(vendorPartNumber) ? null : vendorPartNumber.Trim();
        UnitPrice = unitPrice;
        QuotedAt = quotedAt;
    }

    public string OfferId { get; }

    public string ProviderId { get; }

    public string? VendorPartNumber { get; }

    public Money UnitPrice { get; }

    public DateTimeOffset QuotedAt { get; }
}
