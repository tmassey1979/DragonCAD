namespace DragonCAD.Sourcing;

public sealed record VendorQuoteOffer(
    NormalizedVendorQuote Quote,
    PriceLadder PriceLadder);
