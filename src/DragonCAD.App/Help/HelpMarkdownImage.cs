namespace DragonCAD.App.Help;

public sealed record HelpMarkdownImage(
    string AltText,
    string Source,
    bool IsLocalAsset,
    bool Exists,
    string? ResolvedPath);
