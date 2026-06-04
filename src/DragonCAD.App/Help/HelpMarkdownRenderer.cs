using System.Text;
using System.Text.RegularExpressions;

namespace DragonCAD.App.Help;

public static partial class HelpMarkdownRenderer
{
    public static IReadOnlyList<HelpMarkdownBlock> Render(string markdown, HelpMarkdownAssetResolver? assetResolver = null)
    {
        string normalizedMarkdown = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        string[] lines = normalizedMarkdown.Split('\n');
        List<HelpMarkdownBlock> blocks = [];
        StringBuilder paragraph = new();
        List<string> codeLines = [];
        bool isInCodeBlock = false;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
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

            if (TryGetImage(trimmed, assetResolver, out HelpMarkdownBlock imageBlock))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(imageBlock);
                continue;
            }

            if (TryGetHeading(trimmed, out int headingLevel, out string headingText))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(CreateBlock(HelpMarkdownBlockKind.Heading, headingText, headingLevel));
                continue;
            }

            if (IsTableStart(lines, lineIndex))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(CreateTableBlock(lines, lineIndex, out int consumedLineCount));
                lineIndex += consumedLineCount - 1;
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

    internal static bool IsLocalRelativeAsset(string source)
    {
        string normalizedSource = source.Trim();
        return normalizedSource.Length > 0 &&
            !Path.IsPathFullyQualified(normalizedSource) &&
            !normalizedSource.Contains("..", StringComparison.Ordinal) &&
            (!Uri.TryCreate(normalizedSource, UriKind.Absolute, out Uri? absoluteUri) || string.IsNullOrEmpty(absoluteUri.Scheme));
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

    private static bool TryGetImage(string line, HelpMarkdownAssetResolver? assetResolver, out HelpMarkdownBlock block)
    {
        Match match = ImagePattern().Match(line);
        if (!match.Success || match.Index != 0 || match.Length != line.Length)
        {
            block = CreateBlock(HelpMarkdownBlockKind.Paragraph, line, 0);
            return false;
        }

        string altText = match.Groups["alt"].Value;
        string source = match.Groups["target"].Value;
        HelpMarkdownImage image = assetResolver?.Resolve(altText, source) ?? new HelpMarkdownImage(altText, source, IsLocalRelativeAsset(source), false, null);
        block = new HelpMarkdownBlock(HelpMarkdownBlockKind.Image, altText, 0, [], [], [image], [], []);
        return true;
    }

    private static bool IsTableStart(string[] lines, int lineIndex)
    {
        if (lineIndex + 1 >= lines.Length)
        {
            return false;
        }

        return IsTableRow(lines[lineIndex]) && IsTableSeparator(lines[lineIndex + 1]);
    }

    private static HelpMarkdownBlock CreateTableBlock(string[] lines, int lineIndex, out int consumedLineCount)
    {
        IReadOnlyList<string> headers = SplitTableRow(lines[lineIndex]);
        List<IReadOnlyList<string>> rows = [];
        int rowIndex = lineIndex + 2;
        while (rowIndex < lines.Length && IsTableRow(lines[rowIndex]))
        {
            IReadOnlyList<string> row = SplitTableRow(lines[rowIndex]);
            if (row.Count == headers.Count)
            {
                rows.Add(row);
            }

            rowIndex++;
        }

        consumedLineCount = rowIndex - lineIndex;
        string markdownText = string.Join(" ", headers.Concat(rows.SelectMany(row => row)));
        return new HelpMarkdownBlock(
            HelpMarkdownBlockKind.Table,
            string.Join(" ", headers),
            0,
            [],
            ExtractInlineCode(markdownText),
            [],
            headers,
            rows);
    }

    private static bool IsTableRow(string line)
    {
        string trimmed = line.Trim();
        return trimmed.StartsWith('|') && trimmed.EndsWith('|');
    }

    private static bool IsTableSeparator(string line)
    {
        IReadOnlyList<string> cells = SplitTableRow(line);
        return cells.Count > 0 && cells.All(IsTableSeparatorCell);
    }

    private static bool IsTableSeparatorCell(string cell)
    {
        string normalizedCell = cell.Trim(':');
        return normalizedCell.Length >= 3 && normalizedCell.Trim('-').Length == 0;
    }

    private static IReadOnlyList<string> SplitTableRow(string line) =>
        line.Trim().Trim('|').Split('|').Select(cell => StripInlineMarkdown(cell.Trim())).ToArray();

    private static HelpMarkdownBlock CreateBlock(HelpMarkdownBlockKind kind, string markdownText, int headingLevel)
    {
        HelpMarkdownLink[] links = LinkPattern()
            .Matches(markdownText)
            .Select(match => new HelpMarkdownLink(match.Groups["text"].Value, match.Groups["target"].Value))
            .ToArray();
        string displayText = StripInlineMarkdown(markdownText);
        return new HelpMarkdownBlock(kind, displayText.Trim(), headingLevel, links, ExtractInlineCode(markdownText), [], [], []);
    }

    private static string StripInlineMarkdown(string markdownText)
    {
        string withoutImages = ImagePattern().Replace(markdownText, match => match.Groups["alt"].Value);
        string withoutLinks = LinkPattern().Replace(withoutImages, match => match.Groups["text"].Value);
        return InlineCodePattern().Replace(withoutLinks, match => match.Groups["code"].Value);
    }

    private static IReadOnlyList<HelpMarkdownInlineCode> ExtractInlineCode(string markdownText) =>
        InlineCodePattern()
            .Matches(markdownText)
            .Select(match => new HelpMarkdownInlineCode(match.Groups["code"].Value))
            .ToArray();

    [GeneratedRegex(@"(?<!!)\[(?<text>[^\]]+)\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex ImagePattern();

    [GeneratedRegex("`(?<code>[^`]+)`", RegexOptions.CultureInvariant)]
    private static partial Regex InlineCodePattern();
}
