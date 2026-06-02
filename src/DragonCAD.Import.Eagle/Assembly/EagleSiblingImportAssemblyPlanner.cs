namespace DragonCAD.Import.Eagle.Assembly;

public static class EagleSiblingImportAssemblyPlanner
{
    private static readonly string[] DesignExtensions = [".brd", ".sch"];
    private static readonly string[] NearbyLibraryFolderNames = ["libraries", "library", "lib"];

    public static EagleSiblingImportAssemblyPlan Plan(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("Eagle import source file was not found.", fullSourcePath);
        }

        string extension = Path.GetExtension(fullSourcePath);
        if (!IsSupportedExtension(extension))
        {
            throw new ArgumentException("Eagle import source path must end in .brd, .sch, or .lbr.", nameof(sourcePath));
        }

        string directory = Path.GetDirectoryName(fullSourcePath)!;
        string basename = Path.GetFileNameWithoutExtension(fullSourcePath);
        List<string> missingSiblingExtensions = [];
        List<EagleImportAssemblyDiagnostic> diagnostics = [];

        string? primarySchematicPath = FindPrimaryDesignPath(directory, basename, ".sch");
        string? primaryBoardPath = FindPrimaryDesignPath(directory, basename, ".brd");

        foreach (string missingExtension in FindMissingDesignExtensions(extension, primarySchematicPath, primaryBoardPath))
        {
            missingSiblingExtensions.Add(missingExtension);
            diagnostics.Add(new(
                EagleImportAssemblyDiagnosticCodes.MissingSibling,
                EagleImportAssemblyDiagnosticSeverity.Warning,
                $"{basename}{missingExtension} was not found next to {Path.GetFileName(fullSourcePath)}."));
        }

        IReadOnlyList<string> libraryPaths = FindLibraryPaths(directory, basename, fullSourcePath, extension, diagnostics);

        return new EagleSiblingImportAssemblyPlan(
            fullSourcePath,
            primarySchematicPath,
            primaryBoardPath,
            libraryPaths,
            missingSiblingExtensions,
            diagnostics);
    }

    private static string? FindPrimaryDesignPath(string directory, string basename, string extension)
    {
        string path = Path.Combine(directory, basename + extension);
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    private static IEnumerable<string> FindMissingDesignExtensions(string sourceExtension, string? schematicPath, string? boardPath)
    {
        if (boardPath is null)
        {
            yield return ".brd";
        }

        if (schematicPath is null)
        {
            yield return ".sch";
        }

        if (!sourceExtension.Equals(".lbr", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }
    }

    private static IReadOnlyList<string> FindLibraryPaths(
        string directory,
        string basename,
        string fullSourcePath,
        string sourceExtension,
        ICollection<EagleImportAssemblyDiagnostic> diagnostics)
    {
        SortedSet<string> libraryPaths = new(StringComparer.OrdinalIgnoreCase);

        if (sourceExtension.Equals(".lbr", StringComparison.OrdinalIgnoreCase))
        {
            libraryPaths.Add(fullSourcePath);
        }

        string siblingLibraryPath = Path.Combine(directory, basename + ".lbr");
        if (File.Exists(siblingLibraryPath))
        {
            libraryPaths.Add(Path.GetFullPath(siblingLibraryPath));
        }

        string[][] nearbyLibraryFolders = NearbyLibraryFolderNames
            .Select(folderName => Path.Combine(directory, folderName))
            .Where(Directory.Exists)
            .Select(GetFolderLibraries)
            .Where(static paths => paths.Length > 0)
            .OrderBy(static paths => paths[0], StringComparer.OrdinalIgnoreCase)
            .ThenBy(static paths => paths[0], StringComparer.Ordinal)
            .ToArray();

        if (nearbyLibraryFolders.Length == 1)
        {
            foreach (string libraryPath in nearbyLibraryFolders[0])
            {
                libraryPaths.Add(libraryPath);
            }
        }
        else if (nearbyLibraryFolders.Length > 1)
        {
            diagnostics.Add(new(
                EagleImportAssemblyDiagnosticCodes.MultipleLibraryFolders,
                EagleImportAssemblyDiagnosticSeverity.Warning,
                "Multiple nearby Eagle library folders contain .lbr files; select the intended project libraries before executing the import plan."));
        }

        return libraryPaths
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetFolderLibraries(string folderPath) =>
        Directory
            .EnumerateFiles(folderPath, "*.lbr", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFullPath)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static path => path, StringComparer.Ordinal)
            .ToArray();

    private static bool IsSupportedExtension(string extension) =>
        DesignExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
        || extension.Equals(".lbr", StringComparison.OrdinalIgnoreCase);
}
