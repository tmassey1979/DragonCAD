namespace DragonCAD.Import.Eagle.Assembly;

public sealed record EagleSiblingImportAssemblyPlan
{
    public EagleSiblingImportAssemblyPlan(
        string sourcePath,
        string? primarySchematicPath,
        string? primaryBoardPath,
        IReadOnlyList<string> libraryPaths,
        IReadOnlyList<string> missingSiblingExtensions,
        IReadOnlyList<EagleImportAssemblyDiagnostic> diagnostics)
    {
        SourcePath = sourcePath;
        PrimarySchematicPath = primarySchematicPath;
        PrimaryBoardPath = primaryBoardPath;
        LibraryPaths = libraryPaths
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        MissingSiblingExtensions = missingSiblingExtensions
            .OrderBy(static extension => extension, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static extension => extension, StringComparer.Ordinal)
            .ToArray();
        Diagnostics = diagnostics
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    public string SourcePath { get; }

    public string? PrimarySchematicPath { get; }

    public string? PrimaryBoardPath { get; }

    public IReadOnlyList<string> LibraryPaths { get; }

    public IReadOnlyList<string> MissingSiblingExtensions { get; }

    public IReadOnlyList<EagleImportAssemblyDiagnostic> Diagnostics { get; }
}
