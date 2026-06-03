using System.Text;
using System.Text.RegularExpressions;

namespace DragonCAD.App.Help;

public static partial class HelpMarkdownRenderer
{
    public static IReadOnlyList<HelpMarkdownBlock> Render(string markdown)
    {
        string normalizedMarkdown = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        string[] lines = normalizedMarkdown.Split('\n');
        List<HelpMarkdownBlock> blocks = [];
        StringBuilder paragraph = new();
        List<string> codeLines = [];
        bool isInCodeBlock = false;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                if (isInCodeBlock)
                {
                    blocks.Add(CreateBlock(HelpMarkdownBlockKind.CodeBlock, string.Join(Environment.NewLine, codeLines), 0));
                    codeLines.Clear();
                    isInCodeBlock = false;
                }
                else
                {
                    FlushParagraph(blocks, paragraph);
                    isInCodeBlock = true;
                }

                continue;
            }

            if (isInCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            if (trimmed.Length == 0)
            {
                FlushParagraph(blocks, paragraph);
                continue;
            }

            if (TryGetHeading(trimmed, out int headingLevel, out string headingText))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(CreateBlock(HelpMarkdownBlockKind.Heading, headingText, headingLevel));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(CreateBlock(HelpMarkdownBlockKind.ListItem, trimmed[2..].Trim(), 0));
                continue;
            }

            if (paragraph.Length > 0)
            {
                paragraph.Append(' ');
            }

            paragraph.Append(trimmed);
        }

        if (isInCodeBlock)
        {
            blocks.Add(CreateBlock(HelpMarkdownBlockKind.CodeBlock, string.Join(Environment.NewLine, codeLines), 0));
        }

        FlushParagraph(blocks, paragraph);
        return blocks;
    }

    private static void FlushParagraph(List<HelpMarkdownBlock> blocks, StringBuilder paragraph)
    {
        if (paragraph.Length == 0)
        {
            return;
        }

        blocks.Add(CreateBlock(HelpMarkdownBlockKind.Paragraph, paragraph.ToString(), 0));
        paragraph.Clear();
    }

    private static bool TryGetHeading(string line, out int headingLevel, out string headingText)
    {
        int markerLength = 0;
        while (markerLength < line.Length && line[markerLength] == '#')
        {
            markerLength++;
        }

        if (markerLength is 0 or > 6 || markerLength >= line.Length || line[markerLength] != ' ')
        {
            headingLevel = 0;
            headingText = "";
            return false;
        }

        headingLevel = markerLength;
        headingText = line[(markerLength + 1)..].Trim();
        return true;
    }

    private static HelpMarkdownBlock CreateBlock(HelpMarkdownBlockKind kind, string markdownText, int headingLevel)
    {
        HelpMarkdownLink[] links = LinkPattern()
            .Matches(markdownText)
            .Select(match => new HelpMarkdownLink(match.Groups["text"].Value, match.Groups["target"].Value))
            .ToArray();
        string displayText = LinkPattern().Replace(markdownText, match => match.Groups["text"].Value);
        return new HelpMarkdownBlock(kind, displayText.Trim(), headingLevel, links);
    }

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex LinkPattern();
}
