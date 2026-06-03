namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartProvider
{
    public VendorCartProvider(
        string providerId,
        string displayName,
        VendorCartSupportMode supportMode,
        bool hasCredentials)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider id is required.", nameof(providerId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        ProviderId = providerId.Trim();
        DisplayName = displayName.Trim();
        SupportMode = supportMode;
        HasCredentials = hasCredentials;
    }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public VendorCartSupportMode SupportMode { get; }

    public bool HasCredentials { get; }
}
