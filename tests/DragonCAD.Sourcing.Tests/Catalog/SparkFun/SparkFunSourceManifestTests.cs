using DragonCAD.Sourcing.Catalog.SparkFun;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Tests.Catalog.SparkFun;

public sealed class SparkFunSourceManifestTests
{
    [Fact]
    public void ParseMapsConfiguredSourceEntries()
    {
        const string json = """
        {
          "sources": [
            {
              "id": "sparkfun-esp32-thing",
              "repositoryUrl": "https://github.com/sparkfun/ESP32_Thing",
              "localPath": "C:/cache/sparkfun/ESP32_Thing",
              "libraryNames": [ "ESP32 Thing", "SparkFun ESP32" ],
              "productUrl": "https://www.sparkfun.com/products/13975",
              "datasheetUrl": "https://cdn.sparkfun.com/assets/learn_tutorials/5/0/7/esp32-thing.pdf",
              "retrievedAtUtc": "2026-05-31T12:30:00Z",
              "warnings": [ "missing bom.csv" ]
            }
          ]
        }
        """;

        var manifest = SparkFunSourceManifestParser.Parse(json);

        var source = Assert.Single(manifest.Sources);
        Assert.Equal("sparkfun-esp32-thing", source.Id);
        Assert.Equal("SparkFun", source.ProviderName);
        Assert.Equal(new Uri("https://github.com/sparkfun/ESP32_Thing"), source.RepositoryUrl);
        Assert.Equal("C:/cache/sparkfun/ESP32_Thing", source.LocalPath);
        Assert.Null(source.CacheKey);
        Assert.Equal(["ESP32 Thing", "SparkFun ESP32"], source.LibraryNames);
        Assert.Equal(new Uri("https://www.sparkfun.com/products/13975"), source.ProductUrl);
        Assert.Equal(new Uri("https://cdn.sparkfun.com/assets/learn_tutorials/5/0/7/esp32-thing.pdf"), source.DatasheetUrl);
        Assert.Equal(DateTimeOffset.Parse("2026-05-31T12:30:00Z"), source.RetrievedAtUtc);
        Assert.Equal(["missing bom.csv"], source.Warnings);
    }

    [Fact]
    public void ValidateReportsDuplicateSourceIdsDeterministically()
    {
        var manifest = new SparkFunSourceManifest(
        [
            CreateSource(id: "sparkfun-thing"),
            CreateSource(id: "sparkfun-thing"),
            CreateSource(id: "sparkfun-other"),
            CreateSource(id: "sparkfun-thing"),
        ]);

        var diagnostics = SparkFunSourceManifestValidator.Validate(
            manifest,
            staleBeforeUtc: DateTimeOffset.Parse("2026-05-01T00:00:00Z"));

        var duplicate = Assert.Single(diagnostics);
        Assert.Equal(SparkFunSourceManifestDiagnosticCodes.DuplicateSourceId, duplicate.Code);
        Assert.Equal("sparkfun-thing", duplicate.SourceId);
        Assert.Equal("Duplicate SparkFun source id 'sparkfun-thing'.", duplicate.Message);
    }

    [Fact]
    public void ValidateReportsMissingRequiredSourceFields()
    {
        var manifest = new SparkFunSourceManifest(
        [
            new SparkFunSourceEntry(
                Id: "missing-required-fields",
                RepositoryUrl: null,
                LocalPath: null,
                CacheKey: null,
                LibraryNames: ["SparkFun Example"],
                ProductUrl: new Uri("https://www.sparkfun.com/products/1"),
                DatasheetUrl: new Uri("https://cdn.sparkfun.com/example.pdf"),
                RetrievedAtUtc: DateTimeOffset.Parse("2026-05-31T12:30:00Z"),
                Warnings: []),
        ]);

        var diagnostics = SparkFunSourceManifestValidator.Validate(
            manifest,
            staleBeforeUtc: DateTimeOffset.Parse("2026-05-01T00:00:00Z"));

        Assert.Equal(
            [
                SparkFunSourceManifestDiagnosticCodes.MissingRepositoryUrl,
                SparkFunSourceManifestDiagnosticCodes.MissingLocalPathOrCacheKey,
            ],
            diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(diagnostics, diagnostic => Assert.Equal("missing-required-fields", diagnostic.SourceId));
        Assert.All(diagnostics, diagnostic => Assert.Equal(CatalogDiagnosticSeverity.Error, diagnostic.Severity));
    }

    [Fact]
    public void ValidateWarnsWhenSourceTimestampIsOlderThanThreshold()
    {
        var manifest = new SparkFunSourceManifest(
        [
            CreateSource(
                id: "stale-source",
                retrievedAtUtc: DateTimeOffset.Parse("2026-04-30T23:59:59Z")),
        ]);

        var diagnostics = SparkFunSourceManifestValidator.Validate(
            manifest,
            staleBeforeUtc: DateTimeOffset.Parse("2026-05-01T00:00:00Z"));

        var stale = Assert.Single(diagnostics);
        Assert.Equal(SparkFunSourceManifestDiagnosticCodes.StaleRetrievedTimestamp, stale.Code);
        Assert.Equal(CatalogDiagnosticSeverity.Warning, stale.Severity);
        Assert.Equal("stale-source", stale.SourceId);
    }

    private static SparkFunSourceEntry CreateSource(
        string id,
        Uri? repositoryUrl = null,
        string? localPath = "C:/cache/sparkfun/source",
        string? cacheKey = null,
        DateTimeOffset? retrievedAtUtc = null)
    {
        return new SparkFunSourceEntry(
            Id: id,
            RepositoryUrl: repositoryUrl ?? new Uri("https://github.com/sparkfun/example"),
            LocalPath: localPath,
            CacheKey: cacheKey,
            LibraryNames: ["SparkFun Example"],
            ProductUrl: new Uri("https://www.sparkfun.com/products/1"),
            DatasheetUrl: new Uri("https://cdn.sparkfun.com/example.pdf"),
            RetrievedAtUtc: retrievedAtUtc ?? DateTimeOffset.Parse("2026-05-31T12:30:00Z"),
            Warnings: []);
    }
}
