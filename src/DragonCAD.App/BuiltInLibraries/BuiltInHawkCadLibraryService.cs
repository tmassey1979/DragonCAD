using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Libraries;

namespace DragonCAD.App.BuiltInLibraries;

public sealed class BuiltInHawkCadLibraryService
{
    private readonly string json;
    private readonly int initialDeviceLimit;

    private BuiltInHawkCadLibraryService(
        string json,
        HawkCadComponentLibraryIndex index,
        int initialDeviceLimit)
    {
        this.json = json;
        Index = index;
        this.initialDeviceLimit = initialDeviceLimit;
    }

    public HawkCadComponentLibraryIndex Index { get; }

    public BuiltInHawkCadLibrarySearchResult InitialLoad =>
        LoadInitialWindow();

    public static BuiltInHawkCadLibraryService FromJson(string json, int initialDeviceLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (initialDeviceLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDeviceLimit), "Initial device limit must be positive.");
        }

        return new BuiltInHawkCadLibraryService(
            json,
            HawkCadComponentLibraryIndex.FromJson(json),
            initialDeviceLimit);
    }

    public BuiltInHawkCadLibrarySearchResult Search(string searchText, int maxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "Search result limit must be positive.");
        }

        string trimmed = searchText.Trim();
        if (trimmed.Length == 0)
        {
            return LoadInitialWindow();
        }

        IReadOnlyList<HawkCadComponentLibraryIndexEntry> matches = Index.Search(trimmed, maxResults);
        if (matches.Count == 0)
        {
            return new BuiltInHawkCadLibrarySearchResult(
                Components: [],
                TotalDevices: Index.TotalDevices,
                LoadedDevices: 0,
                StatusText: $"No HawkCAD library devices match \"{trimmed}\".");
        }

        HawkCadComponentLibraryImportResult importResult = HawkCadComponentLibraryImporter.ImportDevices(
            json,
            matches.Select(match => match.Name).ToArray());

        return new BuiltInHawkCadLibrarySearchResult(
            importResult.Components,
            Index.TotalDevices,
            importResult.Components.Count,
            $"Showing {importResult.Components.Count} of {Index.TotalDevices} HawkCAD library devices for \"{trimmed}\".");
    }

    private BuiltInHawkCadLibrarySearchResult LoadInitialWindow()
    {
        HawkCadComponentLibraryImportResult importResult = HawkCadComponentLibraryImporter.Import(json, initialDeviceLimit);
        string statusText = initialDeviceLimit == int.MaxValue && importResult.Components.Count < Index.TotalDevices
            ? $"Loaded {importResult.Components.Count} importable HawkCAD library devices from {Index.TotalDevices} indexed devices."
            : importResult.Components.Count >= Index.TotalDevices
            ? $"Showing all {Index.TotalDevices} HawkCAD library devices."
            : $"Showing first {importResult.Components.Count} of {Index.TotalDevices} HawkCAD library devices.";

        return new BuiltInHawkCadLibrarySearchResult(
            importResult.Components,
            Index.TotalDevices,
            importResult.Components.Count,
            statusText);
    }
}

public sealed record BuiltInHawkCadLibrarySearchResult(
    IReadOnlyList<ComponentDefinition> Components,
    int TotalDevices,
    int LoadedDevices,
    string StatusText);
