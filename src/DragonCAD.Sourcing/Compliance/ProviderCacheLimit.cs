namespace DragonCAD.Sourcing.Compliance;

public sealed record ProviderCacheLimit
{
    public ProviderCacheLimit(
        ProviderCacheLimitMode mode,
        TimeSpan? maxAge,
        int? maxEntries,
        string? note)
    {
        Mode = mode;
        MaxAge = maxAge;
        MaxEntries = maxEntries;
        Note = Normalize(note);
    }

    public static ProviderCacheLimit Unlimited { get; } = new(
        ProviderCacheLimitMode.Unlimited,
        maxAge: null,
        maxEntries: null,
        note: null);

    public static ProviderCacheLimit NoPersistentCache { get; } = new(
        ProviderCacheLimitMode.NoPersistentCache,
        maxAge: null,
        maxEntries: null,
        note: null);

    public ProviderCacheLimitMode Mode { get; }

    public TimeSpan? MaxAge { get; }

    public int? MaxEntries { get; }

    public string Note { get; }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
