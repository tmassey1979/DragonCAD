using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DragonCAD.App.Projects;

public sealed class ProjectCenterViewModel : INotifyPropertyChanged
{
    private const int MaxRecentProjects = 12;
    private readonly ProjectCenterRecentProjectStore recentProjectStore;
    private readonly IProjectCenterLauncher launcher;
    private string diagnostics = "Choose a recent project, bundled sample, or project folder.";

    public ProjectCenterViewModel()
        : this(DefaultRecentStorePath(), new ProjectCenterNoOpLauncher())
    {
    }

    private ProjectCenterViewModel(string recentStorePath, IProjectCenterLauncher launcher)
    {
        RecentStorePath = recentStorePath;
        this.launcher = launcher;
        recentProjectStore = new ProjectCenterRecentProjectStore(recentStorePath);
        RecentProjects = new ObservableCollection<ProjectCenterRecentProject>(recentProjectStore.Load());
        PinnedFolders = new ObservableCollection<ProjectCenterPinnedFolder>(CreateDefaultPinnedFolders());
        CreateProjectCommand = new DelegateCommand(CreateProject);
        OpenFolderCommand = new DelegateCommand(OpenFolder);
        Load7805SampleCommand = new DelegateCommand(() => LoadSampleById("7805"));
        LoadArduinoUnoSampleCommand = new DelegateCommand(() => LoadSampleById("arduino-uno"));
        SampleProjects = CreateSampleProjects();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ProjectCenterViewModel CreateForStore(string recentStorePath, IProjectCenterLauncher? launcher = null) =>
        new(recentStorePath, launcher ?? new ProjectCenterNoOpLauncher());

    public ObservableCollection<ProjectCenterRecentProject> RecentProjects { get; }

    public ObservableCollection<ProjectCenterPinnedFolder> PinnedFolders { get; }

    public IReadOnlyList<ProjectCenterSampleProject> SampleProjects { get; }

    public string RecentStorePath { get; }

    public string Diagnostics
    {
        get => diagnostics;
        private set
        {
            if (diagnostics == value)
            {
                return;
            }

            diagnostics = value;
            OnPropertyChanged();
        }
    }

    public ICommand CreateProjectCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public ICommand Load7805SampleCommand { get; }

    public ICommand LoadArduinoUnoSampleCommand { get; }

    public void AddRecentProject(string path)
    {
        string normalizedPath = ProjectCenterRecentProjectStore.NormalizePath(path);
        ProjectCenterRecentProject recentProject = new(ProjectCenterRecentProjectStore.DisplayNameFor(normalizedPath), normalizedPath);

        ProjectCenterRecentProject? existing = RecentProjects.FirstOrDefault(
            project => string.Equals(project.Path, normalizedPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentProjects.Remove(existing);
        }

        RecentProjects.Insert(0, recentProject);
        while (RecentProjects.Count > MaxRecentProjects)
        {
            RecentProjects.RemoveAt(RecentProjects.Count - 1);
        }

        recentProjectStore.Save(RecentProjects);
    }

    private void OpenFolder()
    {
        string? folderPath = launcher.PickExistingProjectFolder();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Diagnostics = "Open project folder canceled.";
            return;
        }

        AddRecentProject(folderPath);
        Diagnostics = $"Opened project folder {ProjectCenterRecentProjectStore.DisplayNameFor(folderPath)}.";
    }

    private void CreateProject()
    {
        string? folderPath = launcher.CreateProjectFolder();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Diagnostics = "Create project canceled.";
            return;
        }

        AddRecentProject(folderPath);
        Diagnostics = $"Created project folder {ProjectCenterRecentProjectStore.DisplayNameFor(folderPath)}.";
    }

    private void LoadSampleById(string sampleId)
    {
        ProjectCenterSampleProject sample = SampleProjects.Single(sample => sample.Id == sampleId);
        launcher.LoadSample(sample);
        Diagnostics = $"Load sample requested: {sample.Name}.";
    }

    private IReadOnlyList<ProjectCenterSampleProject> CreateSampleProjects() =>
        [
            new(
                "7805",
                "7805 Regulator",
                "Small linear-regulator board with input, output, and ground nets.",
                Load7805SampleCommand),
            new(
                "arduino-uno",
                "Arduino Uno Rev3",
                "Reference controller sample with headers, USB bridge, and routed board context.",
                LoadArduinoUnoSampleCommand)
        ];

    private static IReadOnlyList<ProjectCenterPinnedFolder> CreateDefaultPinnedFolders()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        string projects = Path.Combine(documents, "DragonCAD");
        return [new ProjectCenterPinnedFolder("DragonCAD Projects", projects)];
    }

    private static string DefaultRecentStorePath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            localApplicationData = AppContext.BaseDirectory;
        }

        return Path.Combine(localApplicationData, "DragonCAD", "ProjectCenter", "recent-projects.json");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class ProjectCenterNoOpLauncher : IProjectCenterLauncher
    {
        public string? PickExistingProjectFolder() => null;

        public string? CreateProjectFolder() => null;

        public void LoadSample(ProjectCenterSampleProject sample)
        {
        }
    }
}
