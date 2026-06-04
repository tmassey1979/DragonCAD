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

            Open a workspace with `OpenProjectFolderCommand` and review status.

            - Create a project folder
            - Open [Project folders](../project-system/project-folders.md)

            | Tool | Command |
            | --- | ---: |
            | Wire | `ActivateWireToolCommand` |

            ![Workspace map](images/workspace-map.png)

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
                    HelpMarkdownBlockKind.Table,
                    HelpMarkdownBlockKind.Image,
                    HelpMarkdownBlockKind.CodeBlock
                ],
                blocks.Select(block => block.Kind));
        Assert.Equal(1, blocks[0].HeadingLevel);
        Assert.Equal("Workspace Basics", blocks[0].Text);
        Assert.Equal("Open a workspace with OpenProjectFolderCommand and review status.", blocks[1].Text);
        HelpMarkdownInlineCode inlineCode = Assert.Single(blocks[1].InlineCode);
        Assert.Equal("OpenProjectFolderCommand", inlineCode.Text);
        Assert.Equal("Create a project folder", blocks[2].Text);
        HelpMarkdownLink link = Assert.Single(blocks[3].Links);
        Assert.Equal("Project folders", link.Text);
        Assert.Equal("../project-system/project-folders.md", link.Target);
        Assert.Equal(["Tool", "Command"], blocks[4].TableHeaders);
        Assert.Equal("Wire", Assert.Single(blocks[4].TableRows)[0]);
        Assert.Equal("ActivateWireToolCommand", blocks[4].TableRows[0][1]);
        HelpMarkdownImage image = Assert.Single(blocks[5].Images);
        Assert.Equal("Workspace map", image.AltText);
        Assert.Equal("images/workspace-map.png", image.Source);
        Assert.Equal("Ctrl+S saves the project", blocks[6].Text);
    }

    [Fact]
    public void LoaderResolvesOnlySafeLocalHelpImages()
    {
        using HelpViewerTestWorkspace workspace = HelpViewerTestWorkspace.CreateWithImages();
        HelpTopic topic = TestTopic("getting-started.workspace", "docs/help/getting-started/workspace.md");
        HelpTopicRegistry registry = new([new HelpTopicGroup("getting-started", "Getting started", "Start here.")], [topic]);

        HelpDocument document = HelpDocumentLoader.LoadTopic(registry, topic.Id, workspace.RootPath);

        HelpMarkdownImage safeImage = document.Blocks.Single(block => block.Text == "Safe map").Images.Single();
        HelpMarkdownImage externalImage = document.Blocks.Single(block => block.Text == "External").Images.Single();
        HelpMarkdownImage traversalImage = document.Blocks.Single(block => block.Text == "Traversal").Images.Single();
        Assert.True(safeImage.IsLocalAsset);
        Assert.True(safeImage.Exists);
        Assert.EndsWith(Path.Combine("docs", "help", "getting-started", "images", "workspace-map.png"), safeImage.ResolvedPath, StringComparison.Ordinal);
        Assert.False(externalImage.IsLocalAsset);
        Assert.False(externalImage.Exists);
        Assert.Null(externalImage.ResolvedPath);
        Assert.False(traversalImage.IsLocalAsset);
        Assert.False(traversalImage.Exists);
        Assert.Null(traversalImage.ResolvedPath);
    }

    [Fact]
    public void HelpDocumentStateOpensAsDockableTabReadyDocument()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        HelpDocumentState documentState = HelpDocumentState.Open(registry, "pcb-layout.routing");

        Assert.Equal("help:pcb-layout.routing", documentState.DocumentId);
        Assert.Equal("Help", documentState.DockGroup);
        Assert.Equal("PCB routing", documentState.Title);
        Assert.True(documentState.IsDockable);
        Assert.Equal("pcb-layout.routing", documentState.Navigator.SelectedTopic.Id);
        Assert.NotEmpty(documentState.Document.Blocks);
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

    public static HelpViewerTestWorkspace CreateWithImages()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "dragoncad-help-viewer-tests", Guid.NewGuid().ToString("N"));
        string documentPath = Path.Combine(rootPath, "docs", "help", "getting-started", "workspace.md");
        string imagePath = Path.Combine(rootPath, "docs", "help", "getting-started", "images", "workspace-map.png");
        Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
        File.WriteAllBytes(imagePath, [0x89, 0x50, 0x4E, 0x47]);
        File.WriteAllText(
            documentPath,
            """
            # Workspace Basics

            ![Safe map](images/workspace-map.png)

            ![External](https://example.com/workspace-map.png)

            ![Traversal](../../README.md)
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
