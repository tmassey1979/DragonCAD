namespace DragonCAD.Sourcing.Providers;

public sealed record VendorCredentialBag(IReadOnlyDictionary<string, string> SecretPlaceholders)
{
    public static VendorCredentialBag Empty { get; } = new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public static VendorCredentialBag FromSecretValues(IReadOnlyDictionary<string, string> secretValues)
    {
        ArgumentNullException.ThrowIfNull(secretValues);

        return new VendorCredentialBag(
            secretValues.Keys
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(key => key, key => $"<secret:{key}>", StringComparer.OrdinalIgnoreCase));
    }

    public string GetPlaceholder(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return SecretPlaceholders.TryGetValue(key, out var placeholder)
            ? placeholder
            : $"<missing-secret:{key}>";
    }
}
