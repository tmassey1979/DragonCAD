using System.Windows.Input;

namespace DragonCAD.App.Projects;

public sealed record ProjectCenterRecentProject(string Name, string Path);

public sealed record ProjectCenterPinnedFolder(string Name, string Path);

public sealed record ProjectCenterSampleProject(
    string Id,
    string Name,
    string Description,
    ICommand Command);

public interface IProjectCenterLauncher
{
    string? PickExistingProjectFolder();

    string? CreateProjectFolder();

    void LoadSample(ProjectCenterSampleProject sample);
}
