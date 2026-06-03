using DragonCAD.Core.Projects.Marketplace;
using Xunit;

namespace DragonCAD.Core.Tests.Projects.Marketplace;

public sealed class MarketplaceProjectStateStoreTests
{
    [Fact]
    public void SaveAndLoadProjectMarketplaceStateRoundTripsDeterministically()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        MarketplaceProjectStateStore store = new();
        MarketplaceProjectState state = CreateStateWithUnsortedEntries();

        store.Save(temp.Path, state);
        string firstWrite = File.ReadAllText(Path.Combine(temp.Path, MarketplaceProjectStateStore.RelativeStatePath));
        store.Save(temp.Path, state);
        string secondWrite = File.ReadAllText(Path.Combine(temp.Path, MarketplaceProjectStateStore.RelativeStatePath));
        MarketplaceProjectStateLoadResult result = store.Load(temp.Path);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(state, result.State);
        Assert.Equal(firstWrite, secondWrite);
        Assert.Contains("\"schemaVersion\": \"1.0\"", firstWrite, StringComparison.Ordinal);
        Assert.True(firstWrite.IndexOf("digikey", StringComparison.Ordinal) < firstWrite.IndexOf("mouser", StringComparison.Ordinal));
        Assert.True(firstWrite.IndexOf("alt-1", StringComparison.Ordinal) < firstWrite.IndexOf("alt-2", StringComparison.Ordinal));
        Assert.True(firstWrite.IndexOf("local-1", StringComparison.Ordinal) < firstWrite.IndexOf("local-2", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadReturnsEmptyStateWhenMarketplaceFileIsMissing()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        MarketplaceProjectStateStore store = new();

        MarketplaceProjectStateLoadResult result = store.Load(temp.Path);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(MarketplaceProjectState.Empty, result.State);
    }

    [Fact]
    public void LoadReturnsDiagnosticWhenMarketplaceFileIsMalformed()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        MarketplaceProjectStateStore store = new();
        string path = Path.Combine(temp.Path, MarketplaceProjectStateStore.RelativeStatePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ invalid json");

        MarketplaceProjectStateLoadResult result = store.Load(temp.Path);

        Assert.Null(result.State);
        MarketplaceProjectStateDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MarketplaceProjectStateDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(MarketplaceProjectStateDiagnosticCodes.StateFileCorrupt, diagnostic.Code);
        Assert.Contains(MarketplaceProjectStateStore.RelativeStatePath, diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveDoesNotSerializeCredentialsPaymentOrShippingSecrets()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        MarketplaceProjectStateStore store = new();
        MarketplaceProjectState state = new(
            new Version(1, 0),
            [new MarketplaceProviderFreshness("digikey", new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), "etag-safe")],
            [
                new MarketplaceSelectedAlternate(
                    "R1",
                    "resistor-10k",
                    "digikey",
                    "sku-safe",
                    "alternate selected with oauth-token-should-not-persist")
            ],
            [new MarketplaceOrderDraft("digikey", "cart-safe", "draft-safe")],
            [
                new MarketplaceLocalOrderRecord(
                    "local-1",
                    "digikey",
                    "order-safe",
                    new DateTimeOffset(2026, 6, 2, 8, 30, 0, TimeSpan.Zero),
                    MarketplaceLocalOrderStatus.Draft,
                    ["R1"],
                    "shipping-address-should-not-persist payment-card-should-not-persist")
            ]);

        store.Save(temp.Path, state);

        string json = File.ReadAllText(Path.Combine(temp.Path, MarketplaceProjectStateStore.RelativeStatePath));
        Assert.DoesNotContain("oauth-token-should-not-persist", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment-card-should-not-persist", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shipping-address-should-not-persist", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFromAppArtifactScopedStateProducesProjectStateWithoutMutatingSource()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        MarketplaceProjectStateStore store = new();
        string artifactPath = Path.Combine(temp.Path, "artifact-marketplace-state.json");
        const string artifactJson = """
            {
              "schemaVersion": "1.0",
              "artifactId": "app-artifact-42",
              "providerFreshness": [
                {
                  "providerId": "mouser",
                  "lastRefreshedAt": "2026-06-02T12:00:00+00:00",
                  "freshnessToken": "mouser-etag"
                }
              ],
              "selectedAlternates": [
                {
                  "designator": "U1",
                  "canonicalComponentId": "timer-ne555",
                  "providerId": "mouser",
                  "vendorSku": "595-NE555P",
                  "selectionReason": "in stock"
                }
              ],
              "orderDrafts": [
                {
                  "providerId": "mouser",
                  "cartId": "cart-42",
                  "orderDraftId": "draft-42"
                }
              ],
              "localOrders": [
                {
                  "localOrderId": "local-42",
                  "providerId": "mouser",
                  "vendorOrderId": "order-42",
                  "createdAt": "2026-06-02T13:00:00+00:00",
                  "status": "submitted",
                  "designators": [ "U1" ],
                  "notes": "safe local note"
                }
              ]
            }
            """;
        File.WriteAllText(artifactPath, artifactJson);

        MarketplaceProjectStateImportResult result = store.ImportAppArtifactState(artifactPath);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(artifactJson, File.ReadAllText(artifactPath));
        MarketplaceProjectState state = Assert.IsType<MarketplaceProjectState>(result.State);
        Assert.Equal("mouser", Assert.Single(state.ProviderFreshness).ProviderId);
        Assert.Equal("cart-42", Assert.Single(state.OrderDrafts).CartId);
        Assert.Equal("U1", Assert.Single(state.SelectedAlternates).Designator);
        Assert.Equal("local-42", Assert.Single(state.LocalOrders).LocalOrderId);
    }

    private static MarketplaceProjectState CreateStateWithUnsortedEntries() =>
        new(
            new Version(1, 0),
            [
                new MarketplaceProviderFreshness("mouser", new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero), "etag-mouser"),
                new MarketplaceProviderFreshness("digikey", new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), "etag-digikey")
            ],
            [
                new MarketplaceSelectedAlternate("R2", "resistor-1k", "mouser", "alt-2", "cheapest"),
                new MarketplaceSelectedAlternate("R1", "resistor-10k", "digikey", "alt-1", "preferred")
            ],
            [
                new MarketplaceOrderDraft("mouser", "cart-2", "draft-2"),
                new MarketplaceOrderDraft("digikey", "cart-1", "draft-1")
            ],
            [
                new MarketplaceLocalOrderRecord(
                    "local-2",
                    "mouser",
                    "order-2",
                    new DateTimeOffset(2026, 6, 2, 8, 30, 0, TimeSpan.Zero),
                    MarketplaceLocalOrderStatus.Submitted,
                    ["R2"],
                    "submitted order"),
                new MarketplaceLocalOrderRecord(
                    "local-1",
                    "digikey",
                    "order-1",
                    new DateTimeOffset(2026, 6, 1, 8, 30, 0, TimeSpan.Zero),
                    MarketplaceLocalOrderStatus.Draft,
                    ["R1"],
                    "draft order")
            ]);

    private sealed class TempProjectDirectory : IDisposable
    {
        private TempProjectDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempProjectDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DragonCAD.MarketplaceProjectState.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempProjectDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
