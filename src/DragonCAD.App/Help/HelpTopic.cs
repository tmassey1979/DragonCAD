namespace DragonCAD.App.Help;

public sealed record HelpTopic(
    string Id,
    string GroupId,
    string Title,
    string Summary,
    string DocumentPath,
    IReadOnlyList<string> RelatedTopicIds,
    string WikiSlug,
    IReadOnlyList<string> Keywords);
