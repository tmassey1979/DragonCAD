namespace DragonCAD.App.Help;

internal static class HelpWikiGeneratedManifest
{
    public const string RelativePath = "docs/wiki/.dragoncad-generated-pages";

    public static IReadOnlyList<string> Read(string repositoryRoot)
    {
        string manifestPath = HelpWikiValidationCommand.ResolveRepositoryPath(repositoryRoot, RelativePath);
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        return File.ReadAllLines(manifestPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void Write(string repositoryRoot, IEnumerable<string> slugs)
    {
        string manifestPath = HelpWikiValidationCommand.ResolveRepositoryPath(repositoryRoot, RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllLines(manifestPath, slugs.Order(StringComparer.OrdinalIgnoreCase));
    }
}
