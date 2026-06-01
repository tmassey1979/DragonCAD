namespace DragonCAD.Sourcing.Tests.Providers;

public sealed class VendorPartnershipDocumentTests
{
    private static readonly string[] RequiredVendors =
    [
        "Digi-Key",
        "Mouser",
        "SparkFun",
        "Adafruit",
        "Jameco",
        "OSH Park",
        "PCB Cart",
    ];

    [Fact]
    public void MarketplaceProviderPartnershipArtifactListsRequiredVendorsAndControls()
    {
        string document = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "docs", "vendor-partnerships", "marketplace-provider-partnerships.md"));

        foreach (string vendor in RequiredVendors)
        {
            Assert.Contains($"| {vendor} |", document, StringComparison.Ordinal);
        }

        Assert.Contains("## Provider Partnership Matrix", document, StringComparison.Ordinal);
        Assert.Contains("## Security Requirements", document, StringComparison.Ordinal);
        Assert.Contains("## OAuth And API Key Handling", document, StringComparison.Ordinal);
        Assert.Contains("## Scraping Policy Warning", document, StringComparison.Ordinal);
        Assert.Contains("https://developer.digikey.com", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://www.mouser.com/api-hub", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://docs.sparkfun.com", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://io.adafruit.com/api/docs", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://www.jameco.com/contact", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://oshpark.com/developer", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://www.pcbcart.com/support/faq", document, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonCAD.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the DragonCAD repository root.");
    }
}
