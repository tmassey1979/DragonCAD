namespace DragonCAD.Sourcing.Ordering;

public sealed record LocalVendorCheckoutBlocker(
    LocalVendorCheckoutBlockerKind Kind,
    string Message);
