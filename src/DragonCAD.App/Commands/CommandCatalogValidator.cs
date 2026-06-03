namespace DragonCAD.App.Commands;

public static class CommandCatalogValidator
{
    public static CommandCatalogValidationResult Validate(CommandCatalog catalog)
    {
        List<CommandCatalogDiagnostic> diagnostics = [];

        foreach (CommandCatalogEntry entry in catalog.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.CommandName))
            {
                diagnostics.Add(new(
                    CommandCatalogDiagnosticCodes.MissingCommandName,
                    "Command catalog entry has an empty command name.",
                    entry.Scope));
            }

            if (!CommandCatalogStatuses.ValidValues.Contains(entry.Status))
            {
                diagnostics.Add(new(
                    CommandCatalogDiagnosticCodes.InvalidStatus,
                    $"Command '{entry.CommandName}' has invalid status '{entry.Status}'.",
                    entry.CommandName));
            }
        }

        AddDuplicateTokenDiagnostics(
            catalog.Entries,
            entry => entry.Shortcuts,
            CommandCatalogDiagnosticCodes.DuplicateShortcut,
            "shortcut",
            diagnostics);
        AddDuplicateTokenDiagnostics(
            catalog.Entries,
            entry => entry.Aliases,
            CommandCatalogDiagnosticCodes.DuplicateAlias,
            "alias",
            diagnostics);

        return new CommandCatalogValidationResult(diagnostics);
    }

    private static void AddDuplicateTokenDiagnostics(
        IReadOnlyList<CommandCatalogEntry> entries,
        Func<CommandCatalogEntry, IReadOnlyList<string>> tokensSelector,
        string code,
        string label,
        List<CommandCatalogDiagnostic> diagnostics)
    {
        foreach (IGrouping<string, CommandCatalogEntry> scopeGroup in entries.GroupBy(entry => entry.Scope, StringComparer.OrdinalIgnoreCase))
        {
            Dictionary<string, string> firstCommandByToken = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> reportedTokens = new(StringComparer.OrdinalIgnoreCase);

            foreach (CommandCatalogEntry entry in scopeGroup)
            {
                foreach (string token in tokensSelector(entry).Where(token => !string.IsNullOrWhiteSpace(token)))
                {
                    if (firstCommandByToken.TryGetValue(token, out string? firstCommand))
                    {
                        if (reportedTokens.Add(token))
                        {
                            diagnostics.Add(new(
                                code,
                                $"Scope '{scopeGroup.Key}' assigns {label} '{token}' to both '{firstCommand}' and '{entry.CommandName}'.",
                                entry.CommandName));
                        }
                    }
                    else
                    {
                        firstCommandByToken[token] = entry.CommandName;
                    }
                }
            }
        }
    }
}

public sealed record CommandCatalogValidationResult(IReadOnlyList<CommandCatalogDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

public sealed record CommandCatalogDiagnostic(
    string Code,
    string Message,
    string? CommandName = null);

public static class CommandCatalogDiagnosticCodes
{
    public const string DuplicateShortcut = "duplicate-shortcut";
    public const string DuplicateAlias = "duplicate-alias";
    public const string MissingCommandName = "missing-command-name";
    public const string InvalidStatus = "invalid-status";
}
