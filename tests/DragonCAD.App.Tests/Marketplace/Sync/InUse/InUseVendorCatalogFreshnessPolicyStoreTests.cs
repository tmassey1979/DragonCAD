using DragonCAD.App.Marketplace.Sync.InUse;

namespace DragonCAD.App.Tests.Marketplace.Sync.InUse;

public sealed class InUseVendorCatalogFreshnessPolicyStoreTests
{
    [Fact]
    public void LoadReturnsDefaultPolicyWhenFileDoesNotExist()
    {
        string path = Path.Combine(CreateTempDirectory(), "missing.json");
        InUseVendorCatalogFreshnessPolicyStore store = new(path);

        InUseVendorCatalogFreshnessPolicy policy = store.Load();

        Assert.Equal(TimeSpan.FromHours(12), policy.FreshnessWindowFor("Digi-Key"));
        Assert.Equal(TimeSpan.FromHours(24), policy.FreshnessWindowFor("Mouser"));
    }

    [Fact]
    public void SaveAndLoadRoundTripsPolicyDeterministically()
    {
        string path = Path.Combine(CreateTempDirectory(), "freshness-policy.json");
        InUseVendorCatalogFreshnessPolicyStore store = new(path);
        InUseVendorCatalogFreshnessPolicy policy = new(
            TimeSpan.FromHours(48),
            new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mouser"] = TimeSpan.FromHours(18),
                ["Digi-Key"] = TimeSpan.FromHours(6)
            });

        store.Save(policy);

        string saved = File.ReadAllText(path);
        Assert.True(saved.IndexOf("\"Digi-Key\"", StringComparison.Ordinal) < saved.IndexOf("\"Mouser\"", StringComparison.Ordinal));
        InUseVendorCatalogFreshnessPolicy loaded = store.Load();
        Assert.Equal(TimeSpan.FromHours(48), loaded.DefaultFreshnessWindow);
        Assert.Equal(TimeSpan.FromHours(6), loaded.FreshnessWindowFor("Digi-Key"));
        Assert.Equal(TimeSpan.FromHours(18), loaded.FreshnessWindowFor("Mouser"));
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dragoncad-freshness-policy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
