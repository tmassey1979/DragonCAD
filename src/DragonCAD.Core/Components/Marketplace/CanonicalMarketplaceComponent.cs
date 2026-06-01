namespace DragonCAD.Core.Components.Marketplace;

public readonly record struct CanonicalComponentKey : IComparable<CanonicalComponentKey>
{
    public CanonicalComponentKey(string value)
    {
        Value = NormalizeRequired(value, nameof(value));
    }

    public string Value { get; }

    public static CanonicalComponentKey FromPartNumber(string partNumber) =>
        new($"PART:{NormalizeToken(partNumber, nameof(partNumber))}");

    public static CanonicalComponentKey FromPassive(
        string kind,
        string value,
        string package,
        string tolerance) =>
        new(
            string.Join(
                ':',
                "PASSIVE",
                NormalizeToken(kind, nameof(kind)),
                NormalizePassiveValue(value, nameof(value)),
                NormalizeToken(package, nameof(package)),
                NormalizeToken(tolerance, nameof(tolerance))));

    public int CompareTo(CanonicalComponentKey other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;

    internal static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Marketplace component values cannot be empty.", parameterName);
        }

        if (trimmed.Any(char.IsControl))
        {
            throw new ArgumentException("Marketplace component values cannot contain control characters.", parameterName);
        }

        return trimmed;
    }

    internal static string NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        if (trimmed.Any(char.IsControl))
        {
            throw new ArgumentException("Marketplace component values cannot contain control characters.", nameof(value));
        }

        return trimmed;
    }

    private static string NormalizePassiveValue(string value, string parameterName) =>
        NormalizeToken(value, parameterName)
            .Replace("KΩ", "KOHM", StringComparison.Ordinal)
            .Replace("Ω", "OHM", StringComparison.Ordinal);

    private static string NormalizeToken(string value, string parameterName)
    {
        string normalized = NormalizeRequired(value, parameterName)
            .ToUpperInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal);

        return normalized.Length == 0
            ? throw new ArgumentException("Marketplace component tokens cannot normalize to empty.", parameterName)
            : normalized;
    }
}

public sealed record MarketplaceVendorOffer
{
    public MarketplaceVendorOffer(
        string VendorName,
        string VendorSku,
        string Manufacturer,
        string ManufacturerPartNumber,
        string DisplayName,
        string ProductUrl,
        string ValueOverride,
        string PackageOverride)
    {
        this.VendorName = CanonicalComponentKey.NormalizeRequired(VendorName, nameof(VendorName));
        this.VendorSku = CanonicalComponentKey.NormalizeRequired(VendorSku, nameof(VendorSku));
        this.Manufacturer = CanonicalComponentKey.NormalizeOptional(Manufacturer);
        this.ManufacturerPartNumber = CanonicalComponentKey.NormalizeOptional(ManufacturerPartNumber);
        this.DisplayName = CanonicalComponentKey.NormalizeOptional(DisplayName);
        this.ProductUrl = CanonicalComponentKey.NormalizeOptional(ProductUrl);
        this.ValueOverride = CanonicalComponentKey.NormalizeOptional(ValueOverride);
        this.PackageOverride = CanonicalComponentKey.NormalizeOptional(PackageOverride);
    }

    public string VendorName { get; }

    public string VendorSku { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string DisplayName { get; }

    public string ProductUrl { get; }

    public string ValueOverride { get; }

    public string PackageOverride { get; }

    public string LinkKey => $"{VendorName}:{VendorSku}";
}

public sealed record CanonicalMarketplaceComponent
{
    private CanonicalMarketplaceComponent(
        CanonicalComponentKey key,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string defaultValue,
        string defaultPackage,
        IReadOnlyList<MarketplaceVendorOffer> offers)
    {
        Key = key;
        DisplayName = CanonicalComponentKey.NormalizeRequired(displayName, nameof(displayName));
        Manufacturer = CanonicalComponentKey.NormalizeOptional(manufacturer);
        ManufacturerPartNumber = CanonicalComponentKey.NormalizeOptional(manufacturerPartNumber);
        DefaultValue = CanonicalComponentKey.NormalizeOptional(defaultValue);
        DefaultPackage = CanonicalComponentKey.NormalizeOptional(defaultPackage);
        Offers = offers
            .OrderBy(offer => offer.LinkKey, StringComparer.Ordinal)
            .ToArray();
    }

    public CanonicalComponentKey Key { get; }

    public string DisplayName { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string DefaultValue { get; }

    public string DefaultPackage { get; }

    public IReadOnlyList<MarketplaceVendorOffer> Offers { get; }

    public static CanonicalMarketplaceComponent Create(
        CanonicalComponentKey key,
        string displayName,
        string DefaultValue,
        string DefaultPackage) =>
        new(
            key,
            displayName,
            manufacturer: string.Empty,
            manufacturerPartNumber: string.Empty,
            defaultValue: DefaultValue,
            defaultPackage: DefaultPackage,
            offers: []);

    public static CanonicalMarketplaceComponent FromOffers(
        CanonicalComponentKey key,
        IReadOnlyList<MarketplaceVendorOffer> offers,
        string DefaultValue,
        string DefaultPackage)
    {
        ArgumentNullException.ThrowIfNull(offers);

        MarketplaceVendorOffer? metadataOffer = offers
            .OrderByDescending(offer => offer.ManufacturerPartNumber.Length)
            .ThenBy(offer => offer.ManufacturerPartNumber, StringComparer.Ordinal)
            .ThenBy(offer => offer.LinkKey, StringComparer.Ordinal)
            .FirstOrDefault();

        string displayName = FirstNonEmpty(
            metadataOffer?.DisplayName,
            metadataOffer?.ManufacturerPartNumber,
            key.Value);

        return new CanonicalMarketplaceComponent(
            key,
            displayName,
            metadataOffer?.Manufacturer ?? string.Empty,
            metadataOffer?.ManufacturerPartNumber ?? string.Empty,
            DefaultValue,
            DefaultPackage,
            DeduplicateOffers(offers));
    }

    public CanonicalMarketplaceComponent AttachOffer(MarketplaceVendorOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        Dictionary<string, MarketplaceVendorOffer> offers = Offers.ToDictionary(
            existing => existing.LinkKey,
            StringComparer.Ordinal);
        offers[offer.LinkKey] = offer;

        return new CanonicalMarketplaceComponent(
            Key,
            DisplayName,
            Manufacturer,
            ManufacturerPartNumber,
            DefaultValue,
            DefaultPackage,
            offers.Values.ToArray());
    }

    public CanonicalMarketplaceComponent WithDefaults(string DefaultValue, string DefaultPackage) =>
        new(Key, DisplayName, Manufacturer, ManufacturerPartNumber, DefaultValue, DefaultPackage, Offers);

    public string GetOrderingValue(string vendorName, string vendorSku) =>
        FindOffer(vendorName, vendorSku)?.ValueOverride is { Length: > 0 } valueOverride
            ? valueOverride
            : DefaultValue;

    public string GetOrderingPackage(string vendorName, string vendorSku) =>
        FindOffer(vendorName, vendorSku)?.PackageOverride is { Length: > 0 } packageOverride
            ? packageOverride
            : DefaultPackage;

    private MarketplaceVendorOffer? FindOffer(string vendorName, string vendorSku)
    {
        string linkKey = string.Join(
            ':',
            CanonicalComponentKey.NormalizeRequired(vendorName, nameof(vendorName)),
            CanonicalComponentKey.NormalizeRequired(vendorSku, nameof(vendorSku)));

        return Offers.FirstOrDefault(offer => string.Equals(offer.LinkKey, linkKey, StringComparison.Ordinal));
    }

    private static IReadOnlyList<MarketplaceVendorOffer> DeduplicateOffers(IReadOnlyList<MarketplaceVendorOffer> offers) =>
        offers
            .GroupBy(offer => offer.LinkKey, StringComparer.Ordinal)
            .Select(group => group.OrderBy(offer => offer.ManufacturerPartNumber, StringComparer.Ordinal).First())
            .OrderBy(offer => offer.LinkKey, StringComparer.Ordinal)
            .ToArray();

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "Component";
    }
}
