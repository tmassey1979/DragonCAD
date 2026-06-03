using DragonCAD.App.Help;

namespace DragonCAD.App.Tests.Help;

public sealed class HelpViewerTests
{
    [Fact]
    public void LoaderReadsRegisteredTopicMarkdownFromWorkspaceRoot()
    {
        using HelpViewerTestWorkspace workspace = HelpViewerTestWorkspace.Create();
        HelpTopic topic = TestTopic("getting-started.workspace", "docs/help/getting-started/workspace.md");
        HelpTopicRegistry registry = new([new HelpTopicGroup("getting-started", "Getting started", "Start here.")], [topic]);

        HelpDocument document = HelpDocumentLoader.LoadTopic(registry, topic.Id, workspace.RootPath);

        Assert.Equal(topic.Id, document.Topic.Id);
        Assert.Equal("# Workspace Basics", document.MarkdownLines[0]);
        Assert.Contains(document.Blocks, block => block.Kind == HelpMarkdownBlockKind.Heading && block.Text == "Workspace Basics");
    }

    [Fact]
    public void MarkdownRendererBuildsFormattedModelForCommonMarkdownBlocks()
    {
        const string markdown = """
            # Workspace Basics

            Open a workspace and review status.

            - Create a project folder
            - Open [Project folders](../project-system/project-folders.md)

            ```text
            Ctrl+S saves the project
            ```
            """;

        IReadOnlyList<HelpMarkdownBlock> blocks = HelpMarkdownRenderer.Render(markdown);

        Assert.Equal(
            [
                HelpMarkdownBlockKind.Heading,
                HelpMarkdownBlockKind.Paragraph,
                HelpMarkdownBlockKind.ListItem,
                HelpMarkdownBlockKind.ListItem,
                HelpMarkdownBlockKind.CodeBlock
            ],
            blocks.Select(block => block.Kind));
        Assert.Equal(1, blocks[0].HeadingLevel);
        Assert.Equal("Workspace Basics", blocks[0].Text);
        Assert.Equal("Open a workspace and review status.", blocks[1].Text);
        Assert.Equal("Create a project folder", blocks[2].Text);
        HelpMarkdownLink link = Assert.Single(blocks[3].Links);
        Assert.Equal("Project folders", link.Text);
        Assert.Equal("../project-system/project-folders.md", link.Target);
        Assert.Equal("Ctrl+S saves the project", blocks[4].Text);
    }

    [Fact]
    public void TopicNavigatorFiltersTopicsAndSelectsFirstMatch()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();
        HelpTopicNavigator navigator = new(registry);

        navigator.SearchText = "supplier catalog";

        HelpTopic topic = Assert.Single(navigator.FilteredTopics);
        Assert.Equal("marketplace.vendor-catalogs", topic.Id);
        Assert.Equal(topic, navigator.SelectedTopic);
    }

    [Fact]
    public void TopicNavigatorResolvesInternalMarkdownLinksWithoutExternalNavigation()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();
        HelpTopicNavigator navigator = new(registry);
        navigator.SelectTopic("getting-started.workspace");

        bool resolved = navigator.TryNavigateInternalLink("../project-system/project-folders.md");

        Assert.True(resolved);
        Assert.Equal("project-system.project-folders", navigator.SelectedTopic.Id);
    }

    [Fact]
    public void TopicNavigatorRejectsExternalLinks()
    {
        HelpTopicNavigator navigator = new(HelpTopicRegistry.CreateDefault());

        bool resolved = navigator.TryNavigateInternalLink("https://example.com/help");

        Assert.False(resolved);
        Assert.Equal("getting-started.workspace", navigator.SelectedTopic.Id);
    }

    private static HelpTopic TestTopic(string id, string documentPath) =>
        new(
            Id: id,
            GroupId: "getting-started",
            Title: "Workspace basics",
            Summary: "Open a workspace.",
            DocumentPath: documentPath,
            RelatedTopicIds: [],
            WikiSlug: "Workspace-Basics",
            Keywords: ["workspace"]);
}

internal sealed class HelpViewerTestWorkspace : IDisposable
{
    private HelpViewerTestWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public static HelpViewerTestWorkspace Create()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "dragoncad-help-viewer-tests", Guid.NewGuid().ToString("N"));
        string documentPath = Path.Combine(rootPath, "docs", "help", "getting-started", "workspace.md");
        Directory.CreateDirectory(Path.GetDirectoryName(documentPath)!);
        File.WriteAllText(
            documentPath,
            """
            # Workspace Basics

            Open a workspace and review status.
            """);

        return new HelpViewerTestWorkspace(rootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
