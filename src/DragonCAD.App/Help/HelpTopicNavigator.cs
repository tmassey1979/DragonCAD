namespace DragonCAD.App.Help;

public sealed class HelpTopicNavigator
{
    private readonly HelpTopicRegistry registry;
    private readonly string? workspaceRoot;
    private string searchText = "";

    public HelpTopicNavigator(HelpTopicRegistry registry, string? workspaceRoot = null)
    {
        this.registry = registry;
        this.workspaceRoot = workspaceRoot;
        FilteredTopics = registry.Topics;
        SelectedTopic = registry.Topics[0];
        SelectedDocument = HelpDocumentLoader.LoadTopic(registry, SelectedTopic.Id, workspaceRoot);
    }

    public string SearchText
    {
        get => searchText;
        set
        {
            string normalizedValue = value ?? "";
            if (searchText == normalizedValue)
            {
                return;
            }

            searchText = normalizedValue;
            FilteredTopics = registry.Search(searchText);
            if (FilteredTopics.Count > 0 && !FilteredTopics.Any(topic => topic.Id == SelectedTopic.Id))
            {
                SelectTopic(FilteredTopics[0].Id);
            }
        }
    }

    public IReadOnlyList<HelpTopic> FilteredTopics { get; private set; }

    public HelpTopic SelectedTopic { get; private set; }

    public HelpDocument SelectedDocument { get; private set; }

    public void SelectTopic(string? topicId)
    {
        SelectedTopic = registry.GetTopicOrFallback(topicId);
        SelectedDocument = HelpDocumentLoader.LoadTopic(registry, SelectedTopic.Id, workspaceRoot);
    }

    public bool TryNavigateInternalLink(string? linkTarget)
    {
        string normalizedTarget = (linkTarget ?? "").Trim();
        if (normalizedTarget.Length == 0 ||
            Uri.TryCreate(normalizedTarget, UriKind.Absolute, out Uri? absoluteUri) && !string.IsNullOrEmpty(absoluteUri.Scheme))
        {
            return false;
        }

        string targetWithoutFragment = normalizedTarget.Split('#')[0].Replace('\\', '/').TrimStart('/');
        HelpTopic? topic = registry.Topics.FirstOrDefault(candidate =>
            string.Equals(candidate.DocumentPath.Replace('\\', '/'), targetWithoutFragment, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(candidate.DocumentPath), Path.GetFileName(targetWithoutFragment), StringComparison.OrdinalIgnoreCase));
        if (topic is null)
        {
            return false;
        }

        SelectTopic(topic.Id);
        return true;
    }
}
