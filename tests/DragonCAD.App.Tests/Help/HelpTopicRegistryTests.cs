using DragonCAD.App.Help;

namespace DragonCAD.App.Tests.Help;

public sealed class HelpTopicRegistryTests
{
    [Fact]
    public void BuiltInRegistryExposesStableTopicIdsAndCategories()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        Assert.Equal(
            [
                "getting-started",
                "tutorials",
                "schematic-editing",
                "pcb-layout",
                "component-libraries",
                "marketplace",
                "fabrication",
                "project-system",
                "eagle-migration",
                "troubleshooting",
                "command-reference"
            ],
            registry.Categories.Select(category => category.Id));

        Assert.Contains(registry.Topics, topic => topic.Id == "getting-started.workspace" && topic.GroupId == "getting-started");
        Assert.Contains(registry.Topics, topic => topic.Id == "tutorials.7805-regulator" && topic.GroupId == "tutorials");
        Assert.Contains(registry.Topics, topic => topic.Id == "tutorials.arduino-uno" && topic.GroupId == "tutorials");
        Assert.Contains(registry.Topics, topic => topic.Id == "schematic-editing.placing-wires" && topic.GroupId == "schematic-editing");
        Assert.Contains(registry.Topics, topic => topic.Id == "pcb-layout.board-basics" && topic.GroupId == "pcb-layout");
        Assert.Contains(registry.Topics, topic => topic.Id == "component-libraries.library-basics" && topic.GroupId == "component-libraries");
        Assert.Contains(registry.Topics, topic => topic.Id == "fabrication.outputs" && topic.GroupId == "fabrication");
        Assert.Contains(registry.Topics, topic => topic.Id == "marketplace.vendor-catalogs" && topic.GroupId == "marketplace");
        Assert.Contains(registry.Topics, topic => topic.Id == "project-system.project-folders" && topic.GroupId == "project-system");
        Assert.Contains(registry.Topics, topic => topic.Id == "eagle-migration.importing-eagle-projects" && topic.GroupId == "eagle-migration");
        Assert.Contains(registry.Topics, topic => topic.Id == "troubleshooting.common-issues" && topic.GroupId == "troubleshooting");
        Assert.Contains(registry.Topics, topic => topic.Id == "command-reference.shortcuts" && topic.GroupId == "command-reference");

        Assert.All(
            registry.Topics,
            topic =>
            {
                Assert.False(string.IsNullOrWhiteSpace(topic.Summary));
                Assert.False(string.IsNullOrWhiteSpace(topic.DocumentPath));
                Assert.False(string.IsNullOrWhiteSpace(topic.WikiSlug));
            });
    }

    [Fact]
    public void LookupReturnsTopicByStableIdAndFallsBackForMissingTopics()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        HelpTopic found = registry.GetTopicOrFallback("pcb-layout.board-basics");
        HelpTopic missing = registry.GetTopicOrFallback("schematic-editing.unknown-tool");

        Assert.Equal("Board editing basics", found.Title);
        Assert.Equal(HelpTopicRegistry.MissingTopicId, missing.Id);
        Assert.Equal("Help topic not found", missing.Title);
        Assert.Contains("schematic-editing.unknown-tool", missing.Summary);
    }

    [Fact]
    public void SearchMatchesTitleSummaryAndKeywordsInRegistryOrder()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        IReadOnlyList<HelpTopic> boardResults = registry.Search("routing copper traces");
        IReadOnlyList<HelpTopic> vendorResults = registry.Search("supplier catalog");

        Assert.Equal(["pcb-layout.board-basics"], boardResults.Select(topic => topic.Id));
        Assert.Equal(["marketplace.vendor-catalogs"], vendorResults.Select(topic => topic.Id));
    }

    [Fact]
    public void ValidationReportsValidRegistry()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(HelpTopicRegistry.CreateDefault(), workspace.RootPath);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ValidationReportsDuplicateTopicId()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpTopic topic = TestTopic("getting-started.workspace");
        HelpTopicRegistry registry = new([TestCategory("getting-started")], [topic, topic with { Title = "Duplicate" }]);

        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(registry, workspace.RootPath);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == HelpWikiDiagnosticCodes.DuplicateTopicId);
    }

    [Fact]
    public void ValidationReportsMissingMarkdownFile()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpTopic topic = TestTopic("getting-started.missing") with
        {
            DocumentPath = "docs/help/getting-started/missing.md"
        };
        HelpTopicRegistry registry = new([TestCategory("getting-started")], [topic]);

        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(registry, workspace.RootPath);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == HelpWikiDiagnosticCodes.MissingFile);
    }

    [Fact]
    public void ValidationReportsBrokenRelatedTopic()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpTopic topic = TestTopic("getting-started.workspace") with
        {
            RelatedTopicIds = ["pcb-layout.unknown"]
        };
        HelpTopicRegistry registry = new([TestCategory("getting-started")], [topic]);

        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(registry, workspace.RootPath);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == HelpWikiDiagnosticCodes.BrokenRelatedTopic);
    }

    [Fact]
    public void ValidationReportsBrokenCommandReference()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        string topicPath = Path.Combine(workspace.RootPath, "docs", "help", "getting-started", "workspace.md");
        File.AppendAllText(topicPath, Environment.NewLine + "Use `MissingSampleCommand` from the toolbar.");

        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(HelpTopicRegistry.CreateDefault(), workspace.RootPath);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == HelpWikiDiagnosticCodes.BrokenCommandReference);
    }

    [Fact]
    public void RepositoryHelpDocsReferenceOnlyKnownCommands()
    {
        string repositoryRoot = FindRepositoryRoot();

        HelpWikiValidationResult result = HelpWikiValidationCommand.Validate(HelpTopicRegistry.CreateDefault(), repositoryRoot);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == HelpWikiDiagnosticCodes.BrokenCommandReference);
    }

    [Fact]
    public void WikiExportGeneratesPageWithNavigationRelatedTopicsAndMetadata()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        HelpWikiExportResult result = HelpWikiExportCommand.Export(registry, workspace.RootPath);

        string workspacePage = File.ReadAllText(Path.Combine(workspace.RootPath, "docs", "wiki", "Workspace-Basics.md"));
        Assert.Contains("<!-- generated-by: DragonCAD.HelpWikiExport -->", workspacePage);
        Assert.Contains("Source: `docs/help/getting-started/workspace.md`", workspacePage);
        Assert.Contains("[Help Home](Home.md)", workspacePage);
        Assert.Contains("[Board editing basics](Board-Editing-Basics.md)", workspacePage);
        Assert.Contains("Generated pages: 12", result.Summary);
    }

    [Fact]
    public void SyncDryRunReportsCreatedUpdatedUnchangedAndRemovedPages()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpWikiExportCommand.Export(HelpTopicRegistry.CreateDefault(), workspace.RootPath);

        string wikiRoot = Path.Combine(workspace.RootPath, "docs", "wiki");
        foreach (string pagePath in Directory.EnumerateFiles(wikiRoot, "*.md"))
        {
            string slug = Path.GetFileNameWithoutExtension(pagePath);
            if (slug is not "Workspace-Basics" and not "Vendor-Catalogs")
            {
                File.Delete(pagePath);
            }
        }

        File.WriteAllText(Path.Combine(wikiRoot, "Workspace-Basics.md"), HelpWikiExportCommand.GeneratedMarker + Environment.NewLine + "old generated content");
        File.WriteAllText(Path.Combine(workspace.RootPath, "docs", "wiki", "Removed-Generated.md"), HelpWikiExportCommand.GeneratedMarker + Environment.NewLine);
        File.AppendAllLines(Path.Combine(wikiRoot, ".dragoncad-generated-pages"), ["Removed-Generated"]);

        HelpWikiSyncResult result = HelpWikiSyncCommand.SyncDryRun(HelpTopicRegistry.CreateDefault(), workspace.RootPath);

        Assert.True(result.Validation.IsValid);
        Assert.Equal(10, result.Created.Count);
        Assert.Contains("Workspace-Basics", result.Updated);
        Assert.Contains("Vendor-Catalogs", result.Unchanged);
        Assert.Contains("Removed-Generated", result.Removed);
    }

    [Fact]
    public void SyncDryRunBlocksWhenValidationFails()
    {
        using HelpWikiTestWorkspace workspace = HelpWikiTestWorkspace.Create();
        HelpTopicRegistry registry = new([TestCategory("getting-started")], [TestTopic("getting-started.missing") with
        {
            DocumentPath = "docs/help/getting-started/missing.md"
        }]);

        HelpWikiSyncResult result = HelpWikiSyncCommand.SyncDryRun(registry, workspace.RootPath);

        Assert.False(result.Validation.IsValid);
        Assert.True(result.Blocked);
        Assert.Empty(result.Created);
        Assert.Empty(result.Updated);
        Assert.Empty(result.Unchanged);
        Assert.Empty(result.Removed);
    }

    private static HelpTopicGroup TestCategory(string id) => new(id, "Getting started", "Getting started docs.");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonCAD.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DragonCAD repository root.");
    }

    private static HelpTopic TestTopic(string id) =>
        new(
            Id: id,
            GroupId: "getting-started",
            Title: "Workspace basics",
            Summary: "Open a workspace and review project state.",
            DocumentPath: "docs/help/getting-started/workspace.md",
            RelatedTopicIds: [],
            WikiSlug: "Workspace-Basics",
            Keywords: ["workspace"]);
}

internal sealed class HelpWikiTestWorkspace : IDisposable
{
    private HelpWikiTestWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public static HelpWikiTestWorkspace Create()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "dragoncad-help-tests", Guid.NewGuid().ToString("N"));
        foreach (HelpTopic topic in HelpTopicRegistry.CreateDefault().Topics)
        {
            string documentPath = Path.Combine(rootPath, topic.DocumentPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(documentPath)!);
            File.WriteAllText(documentPath, "# " + topic.Title + Environment.NewLine + Environment.NewLine + topic.Summary);
        }

        return new HelpWikiTestWorkspace(rootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
