using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.App.Marketplace.Smoke;

public interface IVendorLiveSmokeHarness
{
    bool IsEnabled();

    Task<VendorLiveSmokeRunResult> RunDigiKeyKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken);

    Task<VendorLiveSmokeRunResult> RunMouserKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken);
}
