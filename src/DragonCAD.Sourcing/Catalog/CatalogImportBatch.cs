namespace DragonCAD.Sourcing.Catalog;

public sealed record CatalogImportBatch
{
    public CatalogImportBatch(
        string providerName,
        CatalogProviderCapabilities sourceCapabilities,
        IReadOnlyList<VendorCatalogItem> items,
        IReadOnlyList<CatalogImportDiagnostic> diagnostics)
    {
        ProviderName = RequireText(providerName, nameof(providerName));
        SourceCapabilities = sourceCapabilities;
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public string ProviderName { get; }

    public CatalogProviderCapabilities SourceCapabilities { get; }

    public IReadOnlyList<VendorCatalogItem> Items { get; }

    public IReadOnlyList<CatalogImportDiagnostic> Diagnostics { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
