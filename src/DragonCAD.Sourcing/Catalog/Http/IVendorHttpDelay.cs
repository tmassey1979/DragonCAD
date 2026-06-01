namespace DragonCAD.Sourcing.Catalog.Http;

public interface IVendorHttpDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class SystemVendorHttpDelay : IVendorHttpDelay
{
    public static SystemVendorHttpDelay Instance { get; } = new();

    private SystemVendorHttpDelay()
    {
    }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
