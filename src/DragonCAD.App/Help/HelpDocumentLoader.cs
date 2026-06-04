namespace DragonCAD.App.Help;

public static class HelpDocumentLoader
{
    public static HelpDocument LoadTopic(HelpTopicRegistry registry, string? topicId, string? workspaceRoot = null)
    {
        HelpTopic topic = registry.GetTopicOrFallback(topicId);
        string markdown = ReadTopicMarkdown(topic, workspaceRoot, out HelpMarkdownAssetResolver? assetResolver);
        return new HelpDocument(
            topic,
            markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'),
            HelpMarkdownRenderer.Render(markdown, assetResolver));
    }

    private static string ReadTopicMarkdown(HelpTopic topic, string? workspaceRoot, out HelpMarkdownAssetResolver? assetResolver)
    {
        string? documentPath = ResolveDocumentPath(topic.DocumentPath, workspaceRoot);
        if (documentPath is not null)
        {
            assetResolver = CreateAssetResolver(documentPath);
            return File.ReadAllText(documentPath);
        }

        assetResolver = null;
        return "# " + topic.Title + Environment.NewLine + Environment.NewLine + topic.Summary;
    }

    private static HelpMarkdownAssetResolver? CreateAssetResolver(string documentPath)
    {
        DirectoryInfo? directory = new(Path.GetDirectoryName(documentPath)!);
        while (directory is not null)
        {
            if (string.Equals(directory.Name, "help", StringComparison.OrdinalIgnoreCase) &&
                directory.Parent is { Name: "docs" })
            {
                return new HelpMarkdownAssetResolver(directory.FullName, Path.GetDirectoryName(documentPath)!);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? ResolveDocumentPath(string documentPath, string? workspaceRoot)
    {
        string normalizedPath = documentPath.Replace('/', Path.DirectorySeparatorChar);
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            string candidate = Path.Combine(workspaceRoot, normalizedPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, normalizedPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        string currentDirectoryCandidate = Path.Combine(Environment.CurrentDirectory, normalizedPath);
        return File.Exists(currentDirectoryCandidate) ? currentDirectoryCandidate : null;
    }
}
