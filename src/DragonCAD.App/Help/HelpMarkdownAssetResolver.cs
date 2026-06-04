namespace DragonCAD.App.Help;

public sealed class HelpMarkdownAssetResolver
{
    private readonly string helpRoot;
    private readonly string documentDirectory;

    public HelpMarkdownAssetResolver(string helpRoot, string documentDirectory)
    {
        this.helpRoot = Path.GetFullPath(helpRoot);
        this.documentDirectory = Path.GetFullPath(documentDirectory);
    }

    public HelpMarkdownImage Resolve(string altText, string source)
    {
        if (!HelpMarkdownRenderer.IsLocalRelativeAsset(source))
        {
            return new HelpMarkdownImage(altText, source, false, false, null);
        }

        string candidatePath = Path.GetFullPath(Path.Combine(documentDirectory, source.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderHelpRoot(candidatePath))
        {
            return new HelpMarkdownImage(altText, source, false, false, null);
        }

        return new HelpMarkdownImage(altText, source, true, File.Exists(candidatePath), candidatePath);
    }

    private bool IsUnderHelpRoot(string candidatePath) =>
        candidatePath.StartsWith(helpRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(candidatePath, helpRoot, StringComparison.OrdinalIgnoreCase);
}
