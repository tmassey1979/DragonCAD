using DragonCAD.Sourcing.Vendors.OpenHardware;

namespace DragonCAD.Sourcing.Tests.Vendors.OpenHardware;

public sealed class OpenHardwareSourceManifestTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RefreshAfter = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ParseRepresentsSparkFunAndAdafruitRepositoryAndEagleLibrarySources()
    {
        var json = """
            {
              "sources": [
                {
                  "providerName": "SparkFun",
                  "sourceId": "sparkfun-eagle-libraries",
                  "mode": "EagleLibrary",
                  "repositoryUrl": "https://github.com/sparkfun/SparkFun-Eagle-Libraries",
                  "libraryPaths": [ "SparkFun-Boards.lbr", "SparkFun-Connectors.lbr" ],
                  "cacheKey": "sparkfun/eagle-libraries",
                  "retrievedAtUtc": "2026-06-01T12:00:00Z",
                  "refreshAfterUtc": "2026-06-08T12:00:00Z"
                },
                {
                  "providerName": "Adafruit",
                  "sourceId": "adafruit-feather-m0",
                  "mode": "OpenHardwareRepository",
                  "repositoryUrl": "https://github.com/adafruit/Adafruit-Feather-M0-Basic-Proto-PCB",
                  "libraryPaths": [ "Adafruit_Feather_M0_Basic_Proto.sch", "Adafruit_Feather_M0_Basic_Proto.brd" ],
                  "localPath": "vendors/adafruit/feather-m0",
                  "retrievedAtUtc": "2026-06-01T12:00:00Z",
                  "refreshAfterUtc": "2026-06-08T12:00:00Z"
                }
              ]
            }
            """;

        var manifest = OpenHardwareSourceManifestParser.Parse(json);

        Assert.Collection(
            manifest.Sources,
            sparkFun =>
            {
                Assert.Equal("SparkFun", sparkFun.ProviderName);
                Assert.Equal("sparkfun-eagle-libraries", sparkFun.SourceId);
                Assert.Equal(OpenHardwareSourceMode.EagleLibrary, sparkFun.Mode);
                Assert.Equal("https://github.com/sparkfun/SparkFun-Eagle-Libraries", sparkFun.RepositoryUrl?.ToString().TrimEnd('/'));
                Assert.Equal(["SparkFun-Boards.lbr", "SparkFun-Connectors.lbr"], sparkFun.LibraryPaths);
                Assert.Equal("sparkfun/eagle-libraries", sparkFun.CacheKey);
                Assert.Equal(RetrievedAt, sparkFun.RetrievedAtUtc);
                Assert.Equal(RefreshAfter, sparkFun.RefreshAfterUtc);
            },
            adafruit =>
            {
                Assert.Equal("Adafruit", adafruit.ProviderName);
                Assert.Equal("adafruit-feather-m0", adafruit.SourceId);
                Assert.Equal(OpenHardwareSourceMode.OpenHardwareRepository, adafruit.Mode);
                Assert.Equal("vendors/adafruit/feather-m0", adafruit.LocalPath);
                Assert.Equal(["Adafruit_Feather_M0_Basic_Proto.sch", "Adafruit_Feather_M0_Basic_Proto.brd"], adafruit.LibraryPaths);
            });
    }

    [Fact]
    public void ValidateAcceptsSupportedOpenHardwareAndManualFeedModes()
    {
        var manifest = new OpenHardwareSourceManifest(
        [
            Source("SparkFun", "sparkfun-qwiic", OpenHardwareSourceMode.OpenHardwareRepository, repositoryUrl: "https://github.com/sparkfun/Qwiic_Breakout"),
            Source("Adafruit", "adafruit-eagle", OpenHardwareSourceMode.EagleLibrary, repositoryUrl: "https://github.com/adafruit/Adafruit-Eagle-Library"),
            Source("Jameco", "jameco-curated-csv", OpenHardwareSourceMode.ManualCsvFeed, manualFeedName: "Curated MKT-006 CSV"),
        ]);

        var diagnostics = OpenHardwareSourceManifestValidator.Validate(manifest, RetrievedAt.AddDays(-1));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ValidateReportsDuplicateSourceRows()
    {
        var manifest = new OpenHardwareSourceManifest(
        [
            Source("SparkFun", "sparkfun-qwiic", OpenHardwareSourceMode.OpenHardwareRepository, repositoryUrl: "https://github.com/sparkfun/Qwiic_Breakout"),
            Source("SparkFun", "sparkfun-qwiic", OpenHardwareSourceMode.EagleLibrary, repositoryUrl: "https://github.com/sparkfun/SparkFun-Eagle-Libraries"),
        ]);

        var diagnostics = OpenHardwareSourceManifestValidator.Validate(manifest, RetrievedAt.AddDays(-1));

        var duplicate = Assert.Single(diagnostics);
        Assert.Equal(OpenHardwareSourceManifestDiagnosticCodes.DuplicateSourceRow, duplicate.Code);
        Assert.Equal("SparkFun", duplicate.ProviderName);
        Assert.Equal("sparkfun-qwiic", duplicate.SourceId);
    }

    [Fact]
    public void ValidateBlocksUnsupportedScrapingUnlessProviderAllowsIt()
    {
        var manifest = new OpenHardwareSourceManifest(
        [
            Source("Jameco", "jameco-web", OpenHardwareSourceMode.Scrape, repositoryUrl: "https://www.jameco.com"),
            Source("LabVendor", "allowed-scrape", OpenHardwareSourceMode.Scrape, repositoryUrl: "https://example.test/catalog", allowsScraping: true),
        ]);

        var diagnostics = OpenHardwareSourceManifestValidator.Validate(manifest, RetrievedAt.AddDays(-1));

        var blocked = Assert.Single(diagnostics);
        Assert.Equal(OpenHardwareSourceManifestDiagnosticCodes.UnsupportedSourceMode, blocked.Code);
        Assert.Equal("Jameco", blocked.ProviderName);
        Assert.Contains("scraping is not allowed", blocked.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OpenHardwareSourceEntry Source(
        string providerName,
        string sourceId,
        OpenHardwareSourceMode mode,
        string? repositoryUrl = null,
        string? manualFeedName = null,
        bool allowsScraping = false)
    {
        return new OpenHardwareSourceEntry(
            ProviderName: providerName,
            SourceId: sourceId,
            Mode: mode,
            RepositoryUrl: repositoryUrl is null ? null : new Uri(repositoryUrl),
            LocalPath: null,
            CacheKey: "cache/" + sourceId,
            LibraryPaths: [],
            ManualFeedName: manualFeedName,
            RetrievedAtUtc: RetrievedAt,
            RefreshAfterUtc: RefreshAfter,
            AllowsScraping: allowsScraping);
    }
}
