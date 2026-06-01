namespace DragonCAD.Sourcing.Catalog.SparkFun;

public sealed record SparkFunSourceEntry(
    string Id,
    Uri? RepositoryUrl,
    string? LocalPath,
    string? CacheKey,
    IReadOnlyList<string> LibraryNames,
    Uri? ProductUrl,
    Uri? DatasheetUrl,
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<string> Warnings)
{
    public const string Provider = "SparkFun";

    public string ProviderName => Provider;
}
