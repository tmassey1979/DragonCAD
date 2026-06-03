using DragonCAD.App.Projects;

namespace DragonCAD.App.Tests.Projects;

public sealed class ProjectCenterViewModelTests
{
    [Fact]
    public void AddRecentProjectPersistsOutsideProjectFolder()
    {
        using var temporary = new TemporaryProjectCenterStore();
        ProjectCenterViewModel viewModel = ProjectCenterViewModel.CreateForStore(temporary.StorePath);

        string projectPath = Path.Combine(temporary.RootPath, "ClientBoard");
        viewModel.AddRecentProject(projectPath);

        ProjectCenterViewModel reloaded = ProjectCenterViewModel.CreateForStore(temporary.StorePath);

        ProjectCenterRecentProject recent = Assert.Single(reloaded.RecentProjects);
        Assert.Equal("ClientBoard", recent.Name);
        Assert.Equal(Path.GetFullPath(projectPath), recent.Path);
        Assert.Equal(temporary.StorePath, reloaded.RecentStorePath);
        Assert.False(File.Exists(Path.Combine(projectPath, "recent-projects.json")));
    }

    [Fact]
    public void AddRecentProjectDedupesPathAndMovesItToTop()
    {
        using var temporary = new TemporaryProjectCenterStore();
        ProjectCenterViewModel viewModel = ProjectCenterViewModel.CreateForStore(temporary.StorePath);
        string first = Path.Combine(temporary.RootPath, "First");
        string second = Path.Combine(temporary.RootPath, "Second");

        viewModel.AddRecentProject(first);
        viewModel.AddRecentProject(second);
        viewModel.AddRecentProject(first + Path.DirectorySeparatorChar);

        Assert.Equal([Path.GetFullPath(first), Path.GetFullPath(second)], viewModel.RecentProjects.Select(project => project.Path));
    }

    [Fact]
    public void SampleEntriesExpose7805AndArduinoUnoCommands()
    {
        using var temporary = new TemporaryProjectCenterStore();
        ProjectCenterViewModel viewModel = ProjectCenterViewModel.CreateForStore(temporary.StorePath);

        Assert.Equal(["7805 Regulator", "Arduino Uno Rev3"], viewModel.SampleProjects.Select(sample => sample.Name));
        Assert.Contains(viewModel.SampleProjects, sample => sample.Command == viewModel.Load7805SampleCommand);
        Assert.Contains(viewModel.SampleProjects, sample => sample.Command == viewModel.LoadArduinoUnoSampleCommand);
    }

    [Fact]
    public void OpenCommandsReportDiagnosticsAndUpdateRecentProjects()
    {
        using var temporary = new TemporaryProjectCenterStore();
        var launcher = new RecordingProjectCenterLauncher
        {
            FolderToOpen = Path.Combine(temporary.RootPath, "Opened"),
            ProjectToCreate = Path.Combine(temporary.RootPath, "Created")
        };
        ProjectCenterViewModel viewModel = ProjectCenterViewModel.CreateForStore(temporary.StorePath, launcher);

        viewModel.OpenFolderCommand.Execute(null);
        Assert.Equal("Opened project folder Opened.", viewModel.Diagnostics);

        viewModel.CreateProjectCommand.Execute(null);
        Assert.Equal("Created project folder Created.", viewModel.Diagnostics);

        viewModel.Load7805SampleCommand.Execute(null);
        Assert.Equal("Load sample requested: 7805 Regulator.", viewModel.Diagnostics);

        viewModel.LoadArduinoUnoSampleCommand.Execute(null);
        Assert.Equal("Load sample requested: Arduino Uno Rev3.", viewModel.Diagnostics);
        Assert.Equal(
            [Path.GetFullPath(launcher.ProjectToCreate), Path.GetFullPath(launcher.FolderToOpen)],
            viewModel.RecentProjects.Select(project => project.Path));
    }

    private sealed class RecordingProjectCenterLauncher : IProjectCenterLauncher
    {
        public string? FolderToOpen { get; init; }

        public string? ProjectToCreate { get; init; }

        public string? PickExistingProjectFolder() => FolderToOpen;

        public string? CreateProjectFolder() => ProjectToCreate;

        public void LoadSample(ProjectCenterSampleProject sample)
        {
        }
    }

    private sealed class TemporaryProjectCenterStore : IDisposable
    {
        public TemporaryProjectCenterStore()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "DragonCAD.ProjectCenter.Tests", Guid.NewGuid().ToString("N"));
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
