using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public static class OpenHardwareSourceManifestParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static OpenHardwareSourceManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document = JsonSerializer.Deserialize<OpenHardwareSourceManifestDocument>(json, Options)
            ?? new OpenHardwareSourceManifestDocument();

        return new OpenHardwareSourceManifest(
            document.Sources
                .Select(MapSource)
                .ToArray());
    }

    private static OpenHardwareSourceEntry MapSource(OpenHardwareSourceEntryDocument source)
    {
        return new OpenHardwareSourceEntry(
            ProviderName: NormalizeText(source.ProviderName),
            SourceId: NormalizeText(source.SourceId),
            Mode: source.Mode,
            RepositoryUrl: TryCreateUri(source.RepositoryUrl),
            LocalPath: NormalizeOptionalText(source.LocalPath),
            CacheKey: NormalizeOptionalText(source.CacheKey),
            LibraryPaths: source.LibraryPaths ?? [],
            ManualFeedName: NormalizeOptionalText(source.ManualFeedName),
            RetrievedAtUtc: source.RetrievedAtUtc,
            RefreshAfterUtc: source.RefreshAfterUtc,
            AllowsScraping: source.AllowsScraping);
    }

    private static Uri? TryCreateUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private sealed record OpenHardwareSourceManifestDocument
    {
        public IReadOnlyList<OpenHardwareSourceEntryDocument> Sources { get; init; } = [];
    }

    private sealed record OpenHardwareSourceEntryDocument
    {
        public string? ProviderName { get; init; }

        public string? SourceId { get; init; }

        public OpenHardwareSourceMode Mode { get; init; }

        public string? RepositoryUrl { get; init; }

        public string? LocalPath { get; init; }

        public string? CacheKey { get; init; }

        public IReadOnlyList<string>? LibraryPaths { get; init; }

        public string? ManualFeedName { get; init; }

        public DateTimeOffset RetrievedAtUtc { get; init; }

        public DateTimeOffset RefreshAfterUtc { get; init; }

        public bool AllowsScraping { get; init; }
    }
}
