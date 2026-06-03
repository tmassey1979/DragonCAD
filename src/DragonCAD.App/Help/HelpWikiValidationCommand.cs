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
        HashSet<string> knownCommandReferences = FindKnownCommandReferences(registry, repositoryRoot);

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
            else
            {
                foreach (string commandReference in ExtractCommandReferences(File.ReadAllText(documentPath)))
                {
                    if (!knownCommandReferences.Contains(commandReference))
                    {
                        diagnostics.Add(new(
                            HelpWikiDiagnosticCodes.BrokenCommandReference,
                            $"Topic '{topic.Id}' references command '{commandReference}' that is not listed in command reference help.",
                            topic.Id,
                            topic.DocumentPath));
                    }
                }
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

    private static HashSet<string> FindKnownCommandReferences(HelpTopicRegistry registry, string repositoryRoot)
    {
        HashSet<string> commands = new(StringComparer.Ordinal);
        foreach (HelpTopic topic in registry.Topics.Where(topic => topic.GroupId.Equals("command-reference", StringComparison.OrdinalIgnoreCase)))
        {
            string documentPath = ResolveRepositoryPath(repositoryRoot, topic.DocumentPath);
            if (!File.Exists(documentPath))
            {
                continue;
            }

            foreach (string commandReference in ExtractCommandReferences(File.ReadAllText(documentPath)))
            {
                commands.Add(commandReference);
            }
        }

        return commands;
    }

    private static IEnumerable<string> ExtractCommandReferences(string markdown)
    {
        foreach (Match match in CommandReferenceRegex().Matches(markdown))
        {
            yield return match.Groups["command"].Value;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9-]*$")]
    private static partial Regex ValidWikiSlugRegex();

    [GeneratedRegex("`(?<command>[A-Za-z][A-Za-z0-9_]*Command)`")]
    private static partial Regex CommandReferenceRegex();
}
