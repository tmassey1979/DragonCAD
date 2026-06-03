namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public static class OpenHardwareSourceManifestDiagnosticCodes
{
    public const string MissingProviderName = "OPENHW_MISSING_PROVIDER";
    public const string MissingSourceId = "OPENHW_MISSING_SOURCE_ID";
    public const string MissingRepositoryUrl = "OPENHW_MISSING_REPOSITORY_URL";
    public const string MissingLocalPathOrCacheKey = "OPENHW_MISSING_LOCAL_PATH_OR_CACHE_KEY";
    public const string MissingManualFeedName = "OPENHW_MISSING_MANUAL_FEED_NAME";
    public const string DuplicateSourceRow = "OPENHW_DUPLICATE_SOURCE_ROW";
    public const string StaleRetrievedTimestamp = "OPENHW_STALE_RETRIEVED_TIMESTAMP";
    public const string UnsupportedSourceMode = "OPENHW_UNSUPPORTED_SOURCE_MODE";
}
