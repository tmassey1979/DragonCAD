namespace DragonCAD.App.Help;

public static class HelpWikiDiagnosticCodes
{
    public const string MissingFile = "missing-file";
    public const string DuplicateTopicId = "duplicate-topic-id";
    public const string BrokenRelatedTopic = "broken-related-topic";
    public const string BrokenWikiSlug = "broken-wiki-slug";
    public const string EmptySummary = "empty-summary";
    public const string OrphanedPage = "orphaned-page";
}

public sealed record HelpWikiDiagnostic(
    string Code,
    string Message,
    string? TopicId = null,
    string? Path = null);

public sealed record HelpWikiValidationResult(IReadOnlyList<HelpWikiDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}
