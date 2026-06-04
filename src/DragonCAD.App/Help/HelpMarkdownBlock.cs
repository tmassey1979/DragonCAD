namespace DragonCAD.App.Help;

public sealed record HelpMarkdownBlock(
    HelpMarkdownBlockKind Kind,
    string Text,
    int HeadingLevel,
    IReadOnlyList<HelpMarkdownLink> Links,
    IReadOnlyList<HelpMarkdownInlineCode> InlineCode,
    IReadOnlyList<HelpMarkdownImage> Images,
    IReadOnlyList<string> TableHeaders,
    IReadOnlyList<IReadOnlyList<string>> TableRows)
{
    public bool IsHeading => Kind == HelpMarkdownBlockKind.Heading;

    public bool IsParagraph => Kind == HelpMarkdownBlockKind.Paragraph;

    public bool IsListItem => Kind == HelpMarkdownBlockKind.ListItem;

    public bool IsTable => Kind == HelpMarkdownBlockKind.Table;

    public bool IsImage => Kind == HelpMarkdownBlockKind.Image;

    public bool IsCodeBlock => Kind == HelpMarkdownBlockKind.CodeBlock;

    public double HeadingFontSize => HeadingLevel switch
    {
        <= 1 => 24,
        2 => 20,
        _ => 17
    };
}
