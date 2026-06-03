using DragonCAD.App.Commands;
using DragonCAD.App.Help;

namespace DragonCAD.App.Tests.Help;

public sealed class CommandCatalogTests
{
    [Fact]
    public void DefaultCatalogCoversIssue31ReferenceScopes()
    {
        IReadOnlySet<string> scopes = CommandCatalog.Default.Entries
            .Select(entry => entry.Scope)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(CommandCatalogScopes.Schematic, scopes);
        Assert.Contains(CommandCatalogScopes.Board, scopes);
        Assert.Contains(CommandCatalogScopes.ComponentEditor, scopes);
        Assert.Contains(CommandCatalogScopes.View, scopes);
        Assert.Contains(CommandCatalogScopes.Grid, scopes);
        Assert.Contains(CommandCatalogScopes.Layer, scopes);
        Assert.Contains(CommandCatalogScopes.Marketplace, scopes);
        Assert.Contains(CommandCatalogScopes.Fabrication, scopes);
    }

    [Fact]
    public void DefaultCatalogHasValidMetadata()
    {
        CommandCatalogValidationResult result = CommandCatalogValidator.Validate(CommandCatalog.Default);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ValidationReportsDuplicateShortcutsWithinScope()
    {
        CommandCatalog catalog = new(
            [
                Entry("ActivateSelectToolCommand", CommandCatalogScopes.Schematic, shortcuts: ["Esc"]),
                Entry("CancelPlacementCommand", CommandCatalogScopes.Schematic, shortcuts: ["Esc"])
            ]);

        CommandCatalogValidationResult result = CommandCatalogValidator.Validate(catalog);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CommandCatalogDiagnosticCodes.DuplicateShortcut);
    }

    [Fact]
    public void ValidationReportsDuplicateAliasesWithinScope()
    {
        CommandCatalog catalog = new(
            [
                Entry("ActivateWireToolCommand", CommandCatalogScopes.Schematic, aliases: ["WIRE"]),
                Entry("StartWireCommand", CommandCatalogScopes.Schematic, aliases: ["wire"])
            ]);

        CommandCatalogValidationResult result = CommandCatalogValidator.Validate(catalog);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CommandCatalogDiagnosticCodes.DuplicateAlias);
    }

    [Fact]
    public void ValidationReportsMissingCommandNames()
    {
        CommandCatalog catalog = new([Entry(" ", CommandCatalogScopes.Board)]);

        CommandCatalogValidationResult result = CommandCatalogValidator.Validate(catalog);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CommandCatalogDiagnosticCodes.MissingCommandName);
    }

    [Fact]
    public void ValidationReportsInvalidStatusValues()
    {
        CommandCatalog catalog = new([Entry("AutorouteCommand", CommandCatalogScopes.Board, status: "Maybe Later")]);

        CommandCatalogValidationResult result = CommandCatalogValidator.Validate(catalog);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CommandCatalogDiagnosticCodes.InvalidStatus);
    }

    [Fact]
    public void RegistryIncludesCatalogTopicForInAppHelp()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        HelpTopic topic = registry.GetTopicOrFallback("command-reference.catalog");

        Assert.Equal("Tool and shortcut catalog", topic.Title);
        Assert.Equal("docs/help/reference/command-catalog.md", topic.DocumentPath);
    }

    [Fact]
    public void MarkdownRendererUsesCommandCatalogMetadata()
    {
        CommandCatalog catalog = new(
            [
                Entry(
                    "ActivateWireToolCommand",
                    CommandCatalogScopes.Schematic,
                    status: CommandCatalogStatuses.Available,
                    shortcuts: ["W"],
                    aliases: ["WIRE"])
            ]);

        string markdown = CommandCatalogMarkdownRenderer.Render(catalog);

        Assert.Contains("# Tool and Shortcut Catalog", markdown);
        Assert.Contains("| `ActivateWireToolCommand` | Schematic | Available | `W` | `WIRE` |", markdown);
    }

    [Fact]
    public void RepositoryCatalogMarkdownMatchesDefaultCatalogMetadata()
    {
        string repositoryRoot = FindRepositoryRoot();
        string catalogPath = Path.Combine(repositoryRoot, "docs", "help", "reference", "command-catalog.md");

        string expectedMarkdown = CommandCatalogMarkdownRenderer.Render(CommandCatalog.Default);
        string actualMarkdown = File.ReadAllText(catalogPath);

        Assert.Equal(NormalizeLineEndings(expectedMarkdown), NormalizeLineEndings(actualMarkdown));
    }

    private static CommandCatalogEntry Entry(
        string commandName,
        string scope,
        string status = CommandCatalogStatuses.Available,
        IReadOnlyList<string>? shortcuts = null,
        IReadOnlyList<string>? aliases = null) =>
        new(
            CommandName: commandName,
            Scope: scope,
            Status: status,
            GitHubIssue: "#31",
            Description: "Test command.",
            Shortcuts: shortcuts ?? [],
            Aliases: aliases ?? [],
            UiLocations: ["Test"]);

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonCAD.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DragonCAD repository root.");
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
