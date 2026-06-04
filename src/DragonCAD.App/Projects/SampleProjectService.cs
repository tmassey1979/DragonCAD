using DragonCAD.App.BoardEditor;
using DragonCAD.Core.Projects;

namespace DragonCAD.App.Projects;

public static class SampleProjectIds
{
    public const string Regulator7805 = "7805";
    public const string ArduinoUno = "arduino-uno";
}

public sealed record LoadedSampleProject(
    ProjectCenterSampleProject Entry,
    DragonProject Project,
    ProjectShellState ShellState,
    IReadOnlyList<BoardLayer> BoardLayers);

public sealed class SampleProjectService
{
    private static readonly IReadOnlyList<SampleProjectDefinition> SampleDefinitions =
    [
        new(
            SampleProjectIds.Regulator7805,
            "7805 Regulator",
            "Native linear-regulator project with schematic, board, passives, routed nets, and layer data.",
            "7805-regulator")
    ];

    private static readonly IReadOnlyList<BoardLayer> DefaultBoardLayers =
    [
        new("Top", "#E63D32"),
        new("Bottom", "#2D8CFF"),
        new("Silkscreen", "#E2E8F0"),
        new("Dimension", "#A3E635"),
        new("Keepout", "#F43F5E"),
        new("Names", "#F8FAFC"),
        new("Values", "#CBD5E1"),
        new("Drills", "#94A3B8")
    ];

    private readonly string samplesRoot;
    private readonly DragonProjectFolderStore projectStore;
    private readonly ProjectShellStateStore shellStateStore;

    public SampleProjectService(
        string samplesRoot,
        DragonProjectFolderStore? projectStore = null,
        ProjectShellStateStore? shellStateStore = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(samplesRoot);

        this.samplesRoot = samplesRoot;
        this.projectStore = projectStore ?? new DragonProjectFolderStore();
        this.shellStateStore = shellStateStore ?? new ProjectShellStateStore();
    }

    public static SampleProjectService CreateDefault() => new(ResolveDefaultSamplesRoot());

    public IReadOnlyList<ProjectCenterSampleProject> ListSamples() =>
        SampleDefinitions
            .Select(definition => new ProjectCenterSampleProject(
                definition.Id,
                definition.Name,
                definition.Description,
                new DelegateCommand(() => { })))
            .ToArray();

    public LoadedSampleProject Load(ProjectCenterSampleProject sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        return Load(sample.Id);
    }

    public LoadedSampleProject Load(string sampleId)
    {
        SampleProjectDefinition definition = SampleDefinitions.Single(definition => definition.Id == sampleId);
        string projectRoot = Path.Combine(samplesRoot, definition.FolderName);

        DragonProjectLoadResult projectLoadResult = projectStore.Load(projectRoot);
        if (projectLoadResult.Project is null || projectLoadResult.Diagnostics.Count > 0)
        {
            DragonProjectDiagnostic diagnostic = projectLoadResult.Diagnostics.FirstOrDefault()
                ?? new DragonProjectDiagnostic(DragonProjectDiagnosticSeverity.Error, "SampleProjectLoadFailed", "Sample project could not be loaded.");
            throw new InvalidOperationException($"Sample '{sampleId}' could not be loaded from '{projectRoot}': {diagnostic.Code}: {diagnostic.Message}");
        }

        ProjectShellState shellState = shellStateStore.Load(projectRoot)
            ?? throw new InvalidOperationException($"Sample '{sampleId}' is missing {ProjectShellStateStore.ShellStatePath}.");

        ProjectCenterSampleProject entry = new(
            definition.Id,
            definition.Name,
            definition.Description,
            new DelegateCommand(() => { }));

        return new LoadedSampleProject(entry, projectLoadResult.Project, shellState, DefaultBoardLayers);
    }

    private static string ResolveDefaultSamplesRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "samples");
            if (IsSamplesRoot(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(directory.FullName, "DragonCAD.slnx")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "samples");
    }

    private static bool IsSamplesRoot(string candidate) =>
        Directory.Exists(candidate) &&
        SampleDefinitions.Any(definition => Directory.Exists(Path.Combine(candidate, definition.FolderName)));

    private sealed record SampleProjectDefinition(
        string Id,
        string Name,
        string Description,
        string FolderName);
}
