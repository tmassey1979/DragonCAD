namespace DragonCAD.App.Help;

public sealed class HelpDocumentState
{
    private HelpDocumentState(HelpTopicNavigator navigator)
    {
        Navigator = navigator;
        Document = navigator.SelectedDocument;
        Title = navigator.SelectedTopic.Title;
        DocumentId = "help:" + navigator.SelectedTopic.Id;
    }

    public string DocumentId { get; }

    public string DockGroup => "Help";

    public string Title { get; }

    public bool IsDockable => true;

    public HelpTopicNavigator Navigator { get; }

    public HelpDocument Document { get; }

    public static HelpDocumentState Open(HelpTopicRegistry registry, string? topicId, string? workspaceRoot = null)
    {
        HelpTopicNavigator navigator = new(registry, workspaceRoot);
        navigator.SelectTopic(topicId);
        return new HelpDocumentState(navigator);
    }
}
