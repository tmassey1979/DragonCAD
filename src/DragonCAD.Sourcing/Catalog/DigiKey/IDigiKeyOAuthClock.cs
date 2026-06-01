namespace DragonCAD.Sourcing.Catalog.DigiKey;

public interface IDigiKeyOAuthClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemDigiKeyOAuthClock : IDigiKeyOAuthClock
{
    public static SystemDigiKeyOAuthClock Instance { get; } = new();

    private SystemDigiKeyOAuthClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
