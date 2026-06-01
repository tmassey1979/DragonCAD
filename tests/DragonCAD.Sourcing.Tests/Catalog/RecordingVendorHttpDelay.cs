using DragonCAD.Sourcing.Catalog.Http;

namespace DragonCAD.Sourcing.Tests.Catalog;

public sealed class RecordingVendorHttpDelay : IVendorHttpDelay
{
    public List<TimeSpan> Delays { get; } = [];

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}
