using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.App.Marketplace.Smoke;

public sealed class VendorLiveSmokeHarnessAdapter : IVendorLiveSmokeHarness
{
    private readonly VendorLiveSmokeHarness harness;
    private readonly Func<string, string?> readEnvironment;

    public VendorLiveSmokeHarnessAdapter(
        VendorLiveSmokeHarness harness,
        Func<string, string?>? readEnvironment = null)
    {
        this.harness = harness ?? throw new ArgumentNullException(nameof(harness));
        this.readEnvironment = readEnvironment ?? Environment.GetEnvironmentVariable;
    }

    public static VendorLiveSmokeHarnessAdapter CreateDefault() =>
        new(VendorLiveSmokeHarness.CreateDefault());

    public bool IsEnabled() => VendorLiveSmokeHarness.IsEnabled(readEnvironment);

    public Task<VendorLiveSmokeRunResult> RunDigiKeyKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken) =>
        harness.RunDigiKeyKeywordSearchAsync(keyword, limit, cancellationToken);

    public Task<VendorLiveSmokeRunResult> RunMouserKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken) =>
        harness.RunMouserKeywordSearchAsync(keyword, limit, cancellationToken);
}
