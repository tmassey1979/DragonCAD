using System.Text.Json;

namespace DragonCAD.App.Projects;

public sealed class ProjectCenterRecentProjectStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string storePath;

    public ProjectCenterRecentProjectStore(string storePath)
    {
        this.storePath = storePath;
    }

    public IReadOnlyList<ProjectCenterRecentProject> Load()
    {
        if (!File.Exists(storePath))
        {
            return [];
        }

        try
        {
            PersistedRecentProjectList? list = JsonSerializer.Deserialize<PersistedRecentProjectList>(
                File.ReadAllText(storePath),
                SerializerOptions);

            return list?.RecentProjects
                .Where(project => !string.IsNullOrWhiteSpace(project.Path))
                .Select(project => new ProjectCenterRecentProject(DisplayNameFor(project.Path), NormalizePath(project.Path)))
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<ProjectCenterRecentProject> recentProjects)
    {
        string? directory = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var list = new PersistedRecentProjectList(
            recentProjects.Select(project => new PersistedRecentProject(NormalizePath(project.Path))).ToArray());

        File.WriteAllText(storePath, JsonSerializer.Serialize(list, SerializerOptions));
    }

    public static string NormalizePath(string path)
    {
        string trimmed = path.Trim();
        while (trimmed.Length > Path.GetPathRoot(trimmed)?.Length && trimmed.EndsWith(Path.DirectorySeparatorChar))
        {
            trimmed = trimmed[..^1];
        }

        return Path.GetFullPath(trimmed);
    }

    public static string DisplayNameFor(string path)
    {
        string normalized = NormalizePath(path);
        return new DirectoryInfo(normalized).Name;
    }

    private sealed record PersistedRecentProjectList(IReadOnlyList<PersistedRecentProject> RecentProjects);

    private sealed record PersistedRecentProject(string Path);
}
