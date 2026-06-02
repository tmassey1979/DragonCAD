namespace DragonCAD.App.Help;

public static class HelpWikiSyncCommand
{
    public static HelpWikiSyncResult SyncDryRun(HelpTopicRegistry registry, string repositoryRoot)
    {
        HelpWikiValidationResult validation = HelpWikiValidationCommand.Validate(registry, repositoryRoot, includeOrphanedPages: false);
        if (!validation.IsValid)
        {
            return HelpWikiSyncResult.BlockedBy(validation);
        }

        Dictionary<string, string> expectedPages = HelpWikiExportCommand.RenderTopicPages(registry);
        HashSet<string> generatedSlugs = HelpWikiGeneratedManifest.Read(repositoryRoot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> created = [];
        List<string> updated = [];
        List<string> unchanged = [];

        foreach ((string slug, string expectedContent) in expectedPages)
        {
            string pagePath = HelpWikiValidationCommand.ResolveRepositoryPath(repositoryRoot, $"docs/wiki/{slug}.md");
            if (!File.Exists(pagePath))
            {
                created.Add(slug);
                continue;
            }

            string existingContent = File.ReadAllText(pagePath);
            if (string.Equals(existingContent, expectedContent, StringComparison.Ordinal))
            {
                unchanged.Add(slug);
                continue;
            }

            if (generatedSlugs.Contains(slug) || existingContent.Contains(HelpWikiExportCommand.GeneratedMarker, StringComparison.Ordinal))
            {
                updated.Add(slug);
            }
        }

        HashSet<string> expectedSlugs = expectedPages.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> removed = generatedSlugs.Except(expectedSlugs, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();

        return new HelpWikiSyncResult(validation, false, created, updated, unchanged, removed);
    }
}

public sealed record HelpWikiSyncResult(
    HelpWikiValidationResult Validation,
    bool Blocked,
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> Removed)
{
    public static HelpWikiSyncResult BlockedBy(HelpWikiValidationResult validation) =>
        new(validation, true, [], [], [], []);
}
