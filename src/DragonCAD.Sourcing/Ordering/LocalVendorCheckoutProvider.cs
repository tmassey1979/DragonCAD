namespace DragonCAD.Sourcing.Ordering;

public sealed record LocalVendorCheckoutProvider(
    string ProviderId,
    string DisplayName,
    bool HasCredentials,
    bool HasShippingAddress,
    bool HasPaymentMethod);
