using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Sourcing.Catalog.SparkFun;

public static class SparkFunSourceManifestParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static SparkFunSourceManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document = JsonSerializer.Deserialize<SparkFunSourceManifestDocument>(json, Options)
            ?? new SparkFunSourceManifestDocument();

        return new SparkFunSourceManifest(
            document.Sources
                .Select(MapSource)
                .ToArray());
    }

    private static SparkFunSourceEntry MapSource(SparkFunSourceEntryDocument source)
    {
        return new SparkFunSourceEntry(
            Id: source.Id ?? string.Empty,
            RepositoryUrl: TryCreateUri(source.RepositoryUrl),
            LocalPath: NormalizeOptionalText(source.LocalPath),
            CacheKey: NormalizeOptionalText(source.CacheKey),
            LibraryNames: source.LibraryNames ?? [],
            ProductUrl: TryCreateUri(source.ProductUrl),
            DatasheetUrl: TryCreateUri(source.DatasheetUrl),
            RetrievedAtUtc: source.RetrievedAtUtc,
            Warnings: source.Warnings ?? []);
    }

    private static Uri? TryCreateUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed record SparkFunSourceManifestDocument
    {
        public IReadOnlyList<SparkFunSourceEntryDocument> Sources { get; init; } = [];
    }

    private sealed record SparkFunSourceEntryDocument
    {
        public string? Id { get; init; }

        public string? RepositoryUrl { get; init; }

        public string? LocalPath { get; init; }

        public string? CacheKey { get; init; }

        public IReadOnlyList<string>? LibraryNames { get; init; }

        public string? ProductUrl { get; init; }

        public string? DatasheetUrl { get; init; }

        public DateTimeOffset RetrievedAtUtc { get; init; }

        public IReadOnlyList<string>? Warnings { get; init; }
    }
}
