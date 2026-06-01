using DragonCAD.App.BuiltInLibraries;

namespace DragonCAD.App.Tests.BuiltInLibraries;

public sealed class BuiltInHawkCadLibraryServiceTests
{
    [Fact]
    public void InitialLoadMaterializesOnlyTheRequestedWindow()
    {
        BuiltInHawkCadLibraryService service = BuiltInHawkCadLibraryService.FromJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            initialDeviceLimit: 1);

        BuiltInHawkCadLibrarySearchResult result = service.InitialLoad;

        Assert.Equal(2, result.TotalDevices);
        Assert.Single(result.Components);
        Assert.Equal("Showing first 1 of 2 HawkCAD library devices.", result.StatusText);
    }

    [Fact]
    public void SearchImportsMatchingDevicesFromTheFullIndex()
    {
        BuiltInHawkCadLibraryService service = BuiltInHawkCadLibraryService.FromJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            initialDeviceLimit: 1);

        BuiltInHawkCadLibrarySearchResult result = service.Search("resistor", maxResults: 10);

        Assert.Equal(2, result.TotalDevices);
        Assert.Equal("Showing 1 of 2 HawkCAD library devices for \"resistor\".", result.StatusText);
        var component = Assert.Single(result.Components);
        Assert.Equal("sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603", component.DisplayName);
    }

    [Fact]
    public void SearchReportsNoMatchesWithoutKeepingStaleRows()
    {
        BuiltInHawkCadLibraryService service = BuiltInHawkCadLibraryService.FromJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            initialDeviceLimit: 1);

        BuiltInHawkCadLibrarySearchResult result = service.Search("not-a-real-part", maxResults: 10);

        Assert.Empty(result.Components);
        Assert.Equal("No HawkCAD library devices match \"not-a-real-part\".", result.StatusText);
    }

    [Fact]
    public void InitialLoadReportsAllDevicesWhenRequestedWindowCoversTheLibrary()
    {
        BuiltInHawkCadLibraryService service = BuiltInHawkCadLibraryService.FromJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            initialDeviceLimit: int.MaxValue);

        BuiltInHawkCadLibrarySearchResult result = service.InitialLoad;

        Assert.Equal(2, result.TotalDevices);
        Assert.Equal(2, result.LoadedDevices);
        Assert.Equal("Showing all 2 HawkCAD library devices.", result.StatusText);
    }
}
