using System.Text;

namespace DragonCAD.App.Help;

public static class HelpWikiExportCommand
{
    public const string GeneratedMarker = "<!-- generated-by: DragonCAD.HelpWikiExport -->";

    public static HelpWikiExportResult Export(HelpTopicRegistry registry, string repositoryRoot)
    {
        HelpWikiValidationResult validation = HelpWikiValidationCommand.Validate(registry, repositoryRoot);
        if (!validation.IsValid)
        {
            return new HelpWikiExportResult(validation, 0, "Generated pages: 0; validation failed.");
        }

        string wikiRoot = HelpWikiValidationCommand.ResolveRepositoryPath(repositoryRoot, "docs/wiki");
        Directory.CreateDirectory(wikiRoot);

        Dictionary<string, string> pages = RenderTopicPages(registry);
        foreach ((string slug, string content) in pages)
        {
            File.WriteAllText(Path.Combine(wikiRoot, slug + ".md"), content);
        }

        File.WriteAllText(Path.Combine(wikiRoot, "Home.md"), RenderHomePage(registry));
        HelpWikiGeneratedManifest.Write(repositoryRoot, pages.Keys);

        return new HelpWikiExportResult(validation, pages.Count, $"Generated pages: {pages.Count}");
    }

    internal static Dictionary<string, string> RenderTopicPages(HelpTopicRegistry registry) =>
        registry.Topics.ToDictionary(topic => topic.WikiSlug, topic => RenderTopicPage(registry, topic), StringComparer.OrdinalIgnoreCase);

    private static string RenderTopicPage(HelpTopicRegistry registry, HelpTopic topic)
    {
        HelpTopicGroup category = registry.Categories.First(group => string.Equals(group.Id, topic.GroupId, StringComparison.OrdinalIgnoreCase));
        StringBuilder page = new();
        page.AppendLine(GeneratedMarker);
        page.AppendLine();
        page.AppendLine("# " + topic.Title);
        page.AppendLine();
        page.AppendLine(topic.Summary);
        page.AppendLine();
        page.AppendLine("## Navigation");
        page.AppendLine();
        page.AppendLine("- [Help Home](Home.md)");
        page.AppendLine("- Category: " + category.Title);
        page.AppendLine();
        page.AppendLine("## Related topics");
        page.AppendLine();

        foreach (HelpTopic relatedTopic in topic.RelatedTopicIds.Select(registry.GetTopicOrFallback).Where(related => related.Id != HelpTopicRegistry.MissingTopicId))
        {
            page.AppendLine($"- [{relatedTopic.Title}]({relatedTopic.WikiSlug}.md)");
        }

        page.AppendLine();
        page.AppendLine("## Metadata");
        page.AppendLine();
        page.AppendLine("- Topic id: `" + topic.Id + "`");
        page.AppendLine("- Source: `" + topic.DocumentPath + "`");
        page.AppendLine("- Version: DragonCAD help registry v1");
        return page.ToString();
    }

    private static string RenderHomePage(HelpTopicRegistry registry)
    {
        StringBuilder page = new();
        page.AppendLine(GeneratedMarker);
        page.AppendLine();
        page.AppendLine("# DragonCAD Help");
        page.AppendLine();
        page.AppendLine("Generated from the in-app help registry.");

        foreach (HelpTopicGroup category in registry.Categories)
        {
            page.AppendLine();
            page.AppendLine("## " + category.Title);
            page.AppendLine();

            foreach (HelpTopic topic in registry.Topics.Where(topic => string.Equals(topic.GroupId, category.Id, StringComparison.OrdinalIgnoreCase)))
            {
                page.AppendLine($"- [{topic.Title}]({topic.WikiSlug}.md) - {topic.Summary}");
            }
        }

        return page.ToString();
    }
}

public sealed record HelpWikiExportResult(
    HelpWikiValidationResult Validation,
    int GeneratedPageCount,
    string Summary);
