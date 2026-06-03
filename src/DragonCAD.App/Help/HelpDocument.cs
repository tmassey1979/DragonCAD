namespace DragonCAD.App.Help;

public sealed record HelpDocument(
    HelpTopic Topic,
    IReadOnlyList<string> MarkdownLines,
    IReadOnlyList<HelpMarkdownBlock> Blocks);
