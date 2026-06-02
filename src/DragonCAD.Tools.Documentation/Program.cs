using DragonCAD.App.Help;

if (args.Length is 0 or > 2)
{
    PrintUsage();
    return 2;
}

string command = args[0];
string repositoryRoot = args.Length == 2 ? Path.GetFullPath(args[1]) : FindRepositoryRoot(Environment.CurrentDirectory);
HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

return command switch
{
    "validate" => RunValidation(registry, repositoryRoot),
    "export" => RunExport(registry, repositoryRoot),
    "sync-dry-run" => RunSyncDryRun(registry, repositoryRoot),
    _ => PrintUsage()
};

static int RunValidation(HelpTopicRegistry registry, string repositoryRoot)
{
    HelpWikiValidationResult validation = HelpWikiValidationCommand.Validate(registry, repositoryRoot);
    PrintDiagnostics(validation);
    Console.WriteLine(validation.IsValid ? "Validation passed." : "Validation failed.");
    return validation.IsValid ? 0 : 1;
}

static int RunExport(HelpTopicRegistry registry, string repositoryRoot)
{
    HelpWikiExportResult result = HelpWikiExportCommand.Export(registry, repositoryRoot);
    PrintDiagnostics(result.Validation);
    Console.WriteLine(result.Summary);
    return result.Validation.IsValid ? 0 : 1;
}

static int RunSyncDryRun(HelpTopicRegistry registry, string repositoryRoot)
{
    HelpWikiSyncResult result = HelpWikiSyncCommand.SyncDryRun(registry, repositoryRoot);
    PrintDiagnostics(result.Validation);
    Console.WriteLine(result.Blocked ? "Sync dry-run blocked by validation failures." : "Sync dry-run passed.");
    Console.WriteLine($"Created: {result.Created.Count}");
    Console.WriteLine($"Updated: {result.Updated.Count}");
    Console.WriteLine($"Unchanged: {result.Unchanged.Count}");
    Console.WriteLine($"Removed: {result.Removed.Count}");
    return result.Blocked ? 1 : 0;
}

static void PrintDiagnostics(HelpWikiValidationResult validation)
{
    foreach (HelpWikiDiagnostic diagnostic in validation.Diagnostics)
    {
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    }
}

static int PrintUsage()
{
    Console.WriteLine("Usage: DragonCAD.Tools.Documentation <validate|export|sync-dry-run> [repository-root]");
    return 2;
}

static string FindRepositoryRoot(string startDirectory)
{
    DirectoryInfo? directory = new(startDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "DragonCAD.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return startDirectory;
}
