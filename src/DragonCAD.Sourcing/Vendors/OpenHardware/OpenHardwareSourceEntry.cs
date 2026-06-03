namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public sealed record OpenHardwareSourceEntry(
    string ProviderName,
    string SourceId,
    OpenHardwareSourceMode Mode,
    Uri? RepositoryUrl,
    string? LocalPath,
    string? CacheKey,
    IReadOnlyList<string> LibraryPaths,
    string? ManualFeedName,
    DateTimeOffset RetrievedAtUtc,
    DateTimeOffset RefreshAfterUtc,
    bool AllowsScraping);
