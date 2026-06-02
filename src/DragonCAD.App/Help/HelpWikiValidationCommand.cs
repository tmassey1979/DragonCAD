using System.Text.RegularExpressions;

namespace DragonCAD.App.Help;

public static partial class HelpWikiValidationCommand
{
    public static HelpWikiValidationResult Validate(HelpTopicRegistry registry, string repositoryRoot) =>
        Validate(registry, repositoryRoot, includeOrphanedPages: true);

    internal static HelpWikiValidationResult Validate(HelpTopicRegistry registry, string repositoryRoot, bool includeOrphanedPages)
    {
        List<HelpWikiDiagnostic> diagnostics = [];
        HashSet<string> topicIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> duplicateTopicIds = [];

        foreach (HelpTopic topic in registry.Topics)
        {
            if (!topicIds.Add(topic.Id))
            {
                duplicateTopicIds.Add(topic.Id);
            }
        }

        foreach (string duplicateTopicId in duplicateTopicIds)
        {
            diagnostics.Add(new(
                HelpWikiDiagnosticCodes.DuplicateTopicId,
                $"Duplicate help topic id '{duplicateTopicId}'.",
                duplicateTopicId));
        }

        HashSet<string> categoryIds = registry.Categories.Select(category => category.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> wikiSlugs = new(StringComparer.OrdinalIgnoreCase);

        foreach (HelpTopic topic in registry.Topics)
        {
            if (!categoryIds.Contains(topic.GroupId))
            {
                diagnostics.Add(new(
                    HelpWikiDiagnosticCodes.BrokenRelatedTopic,
                    $"Topic '{topic.Id}' references missing category '{topic.GroupId}'.",
                    topic.Id));
            }

            if (string.IsNullOrWhiteSpace(topic.Summary))
            {
                diagnostics.Add(new(
                    HelpWikiDiagnosticCodes.EmptySummary,
                    $"Topic '{topic.Id}' has an empty summary.",
                    topic.Id));
            }

            string documentPath = ResolveRepositoryPath(repositoryRoot, topic.DocumentPath);
            if (!File.Exists(documentPath))
            {
                diagnostics.Add(new(
                    HelpWikiDiagnosticCodes.MissingFile,
                    $"Topic '{topic.Id}' references missing markdown file '{topic.DocumentPath}'.",
                    topic.Id,
                    topic.DocumentPath));
            }

            foreach (string relatedTopicId in topic.RelatedTopicIds)
            {
                if (!topicIds.Contains(relatedTopicId))
                {
                    diagnostics.Add(new(
                        HelpWikiDiagnosticCodes.BrokenRelatedTopic,
                        $"Topic '{topic.Id}' references missing related topic '{relatedTopicId}'.",
                        topic.Id));
                }
            }

            if (!ValidWikiSlugRegex().IsMatch(topic.WikiSlug) || !wikiSlugs.Add(topic.WikiSlug))
            {
                diagnostics.Add(new(
                    HelpWikiDiagnosticCodes.BrokenWikiSlug,
                    $"Topic '{topic.Id}' has an invalid or duplicate wiki slug '{topic.WikiSlug}'.",
                    topic.Id));
            }
        }

        if (!includeOrphanedPages)
        {
            return new HelpWikiValidationResult(diagnostics);
        }

        foreach (string orphanedSlug in HelpWikiGeneratedManifest.Read(repositoryRoot).Except(wikiSlugs, StringComparer.OrdinalIgnoreCase))
        {
            diagnostics.Add(new(
                HelpWikiDiagnosticCodes.OrphanedPage,
                $"Generated wiki page '{orphanedSlug}' is not registered as a help topic.",
                Path: $"docs/wiki/{orphanedSlug}.md"));
        }

        return new HelpWikiValidationResult(diagnostics);
    }

    internal static string ResolveRepositoryPath(string repositoryRoot, string relativePath) =>
        Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9-]*$")]
    private static partial Regex ValidWikiSlugRegex();
}
