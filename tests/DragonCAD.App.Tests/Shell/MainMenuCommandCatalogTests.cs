using DragonCAD.App.Shell;

namespace DragonCAD.App.Tests.Shell;

public sealed class MainMenuCommandCatalogTests
{
    [Fact]
    public void DefaultCatalogExposesStableDragonCadMenuOrder()
    {
        ShellMainMenuCatalog catalog = ShellMainMenuCatalog.CreateDefault();

        Assert.Equal(
            [
                "File",
                "Edit",
                "View",
                "Place",
                "Route",
                "Inspect",
                "Tools",
                "AI",
                "Window",
                "Help"
            ],
            catalog.Menus.Select(menu => menu.Label).ToArray());
    }

    [Fact]
    public void CommandsExposeDeterministicIdsMetadataAndEnabledState()
    {
        ShellMainMenuCatalog catalog = ShellMainMenuCatalog.CreateDefault();

        ShellMainMenuCommand save = catalog.GetRequiredCommand("dragoncad.file.save-project");

        Assert.Equal("Save Project", save.Label);
        Assert.Equal("save", save.IconId);
        Assert.Equal("Ctrl+S", save.ShortcutText);
        Assert.True(save.IsEnabled);
        Assert.Equal("", save.DisabledReason);
        Assert.Equal("SaveProjectCommand", save.CommandHandlerName);
    }

    [Fact]
    public void MissingHandlersRemainVisibleAsPlannedDisabledCommands()
    {
        ShellMainMenuCatalog catalog = ShellMainMenuCatalog.CreateDefault();

        ShellMainMenuCommand autoroute = catalog.GetRequiredCommand("dragoncad.route.autoroute");

        Assert.Equal("Autoroute", autoroute.Label);
        Assert.False(autoroute.IsEnabled);
        Assert.Equal("Planned command: handler is not implemented yet.", autoroute.DisabledReason);
        Assert.Equal("AutorouteCommand", autoroute.CommandHandlerName);
    }

    [Fact]
    public void ContextFilterKeepsEditorSpecificCommandsInTheirOwningWorkspace()
    {
        ShellMainMenuCatalog catalog = ShellMainMenuCatalog.CreateDefault();

        ShellMainMenuView schematicMenu = catalog.CreateView("Schematic");
        ShellMainMenuView boardMenu = catalog.CreateView("PcbLayout");

        Assert.Contains(
            schematicMenu.VisibleCommands,
            command => command.Id == "dragoncad.place.wire");
        Assert.DoesNotContain(
            schematicMenu.VisibleCommands,
            command => command.Id == "dragoncad.route.board-route");
        Assert.Contains(
            boardMenu.VisibleCommands,
            command => command.Id == "dragoncad.route.board-route");
        Assert.DoesNotContain(
            boardMenu.VisibleCommands,
            command => command.Id == "dragoncad.place.wire");
    }

    [Fact]
    public void ValidationReportsDuplicateCommandIds()
    {
        ShellMainMenuCatalog catalog = new(
            [
                new ShellMainMenu(
                    "File",
                    [
                        new ShellMainMenuGroup(
                            "Project",
                            [
                                ShellMainMenuCommand.Available(
                                    "dragoncad.file.save-project",
                                    "Save Project",
                                    "save",
                                    "Ctrl+S",
                                    "SaveProjectCommand",
                                    ["All"]),
                                ShellMainMenuCommand.Available(
                                    "dragoncad.file.save-project",
                                    "Save Again",
                                    "save",
                                    "Ctrl+S",
                                    "SaveProjectCommand",
                                    ["All"])
                            ])
                    ])
            ]);

        ShellMainMenuValidationResult result = ShellMainMenuValidator.Validate(catalog);

        ShellMainMenuDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("duplicate-command-id", diagnostic.Code);
        Assert.Equal("dragoncad.file.save-project", diagnostic.CommandId);
    }

    [Fact]
    public void DefaultCatalogDoesNotContainDuplicateCommandIds()
    {
        ShellMainMenuValidationResult result = ShellMainMenuValidator.Validate(ShellMainMenuCatalog.CreateDefault());

        Assert.Empty(result.Diagnostics);
    }
}
