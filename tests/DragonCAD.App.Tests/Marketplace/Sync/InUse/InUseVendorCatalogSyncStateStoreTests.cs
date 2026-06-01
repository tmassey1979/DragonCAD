using DragonCAD.App.Marketplace.Sync.InUse;

namespace DragonCAD.App.Tests.Marketplace.Sync.InUse;

public sealed class InUseVendorCatalogSyncStateStoreTests
{
    [Fact]
    public void SaveAndLoadRoundTripsStatesDeterministically()
    {
        string directory = CreateTempDirectory();
        string path = Path.Combine(directory, "in-use-sync-state.json");
        InUseVendorCatalogSyncStateStore store = new(path);
        InUseVendorCatalogSyncState[] states =
        [
            new("dragon:ne555", "Mouser", "NE555P", new DateTimeOffset(2026, 6, 1, 12, 15, 0, TimeSpan.Zero), 2, 1),
            new("dragon:lm7805", "Digi-Key", "LM7805CT", new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), 3, 0)
        ];

        store.Save(states);

        string saved = File.ReadAllText(path);
        Assert.Contains("\"componentId\": \"dragon:lm7805\"", saved, StringComparison.Ordinal);
        Assert.True(saved.IndexOf("dragon:lm7805", StringComparison.Ordinal) < saved.IndexOf("dragon:ne555", StringComparison.Ordinal));
        Assert.Equal(states.OrderBy(state => state.ComponentId, StringComparer.Ordinal).ToArray(), store.Load());
    }

    [Fact]
    public void LoadReturnsEmptyWhenStateFileDoesNotExist()
    {
        string path = Path.Combine(CreateTempDirectory(), "missing.json");
        InUseVendorCatalogSyncStateStore store = new(path);

        Assert.Empty(store.Load());
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dragoncad-in-use-sync-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
