using DragonCAD.App.BoardEditor;
using DragonCAD.App.Projects;

namespace DragonCAD.App.Tests.Samples;

public sealed class SampleProjectServiceTests
{
    [Fact]
    public void Lists7805RegulatorSampleForProjectCenter()
    {
        SampleProjectService service = SampleProjectService.CreateDefault();

        ProjectCenterSampleProject sample = Assert.Single(
            service.ListSamples(),
            candidate => candidate.Id == SampleProjectIds.Regulator7805);

        Assert.Equal("7805 Regulator", sample.Name);
        Assert.Contains("linear-regulator", sample.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Loads7805RegulatorNativeProjectAndEditorState()
    {
        SampleProjectService service = SampleProjectService.CreateDefault();

        LoadedSampleProject sample = service.Load(SampleProjectIds.Regulator7805);

        Assert.Equal("7805 Regulator", sample.Project.Manifest.Name);
        Assert.Equal(3, sample.Project.Schematic.Components.Count);
        Assert.Equal(3, sample.Project.Schematic.Nets.Count);
        Assert.Equal(3, sample.Project.Board.Placements.Count);
        Assert.Equal(5, sample.Project.Board.Traces.Count);
        Assert.Equal(3, sample.ShellState.SchematicComponents.Count);
        Assert.Equal(5, sample.ShellState.SchematicWires.Count);
        Assert.Equal(3, sample.ShellState.BoardComponents.Count);
        Assert.Equal(5, sample.ShellState.BoardTraces.Count);
        Assert.Equal(8, sample.BoardLayers.Count);

        int symbolPinCount = sample.ShellState.SchematicComponents.Sum(component => component.SymbolPreview.Pins.Count);
        int footprintPadCount = sample.ShellState.BoardComponents.Sum(component => component.FootprintPreview.Pads.Count);
        Assert.Equal(7, symbolPinCount);
        Assert.Equal(7, footprintPadCount);
    }

    [Fact]
    public void Loaded7805RegulatorKeepsSchematicAndBoardConnectionsAligned()
    {
        SampleProjectService service = SampleProjectService.CreateDefault();

        LoadedSampleProject sample = service.Load(SampleProjectIds.Regulator7805);

        Assert.Equal(
            sample.Project.Schematic.Components.Select(component => component.Reference).Order(StringComparer.Ordinal),
            sample.Project.Board.Placements.Select(placement => placement.Reference).Order(StringComparer.Ordinal));

        AssertNetPins(sample, "VIN", ["CIN.P", "U1.IN"]);
        AssertNetPins(sample, "VOUT", ["COUT.P", "U1.OUT"]);
        AssertNetPins(sample, "GND", ["CIN.N", "COUT.N", "U1.GND"]);

        Assert.All(sample.Project.Board.Traces, trace => Assert.Contains(
            trace.Layer,
            sample.BoardLayers.Select(layer => layer.Name),
            StringComparer.Ordinal));
        Assert.Contains(sample.ShellState.BoardTraces, trace => trace.LayerName == "Bottom");
        Assert.Contains(sample.ShellState.BoardTraces, trace => trace.LayerName == "Top");
    }

    [Fact]
    public void ProjectCenterSampleEntryLoadsThroughSampleService()
    {
        using var temporary = new TemporaryProjectCenterStore();
        ProjectCenterViewModel viewModel = ProjectCenterViewModel.CreateForStore(temporary.StorePath);
        SampleProjectService service = SampleProjectService.CreateDefault();

        ProjectCenterSampleProject projectCenterSample = Assert.Single(
            viewModel.SampleProjects,
            sample => sample.Id == SampleProjectIds.Regulator7805);
        LoadedSampleProject loaded = service.Load(projectCenterSample);

        Assert.Equal(projectCenterSample.Id, loaded.Entry.Id);
        Assert.Equal(projectCenterSample.Name, loaded.Entry.Name);
        Assert.Equal(loaded.Project.Schematic.Components.Count, loaded.Project.Board.Placements.Count);
    }

    private static void AssertNetPins(LoadedSampleProject sample, string netName, IReadOnlyList<string> expectedPins)
    {
        Assert.Equal(
            expectedPins.Order(StringComparer.Ordinal),
            sample.Project.Schematic.Nets.Single(net => net.Name == netName).Pins.Order(StringComparer.Ordinal));
    }

    private sealed class TemporaryProjectCenterStore : IDisposable
    {
        public TemporaryProjectCenterStore()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "DragonCAD.SampleProject.Tests", Guid.NewGuid().ToString("N"));
            StorePath = Path.Combine(RootPath, "app-state", "recent-projects.json");
        }

        public string RootPath { get; }

        public string StorePath { get; }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
