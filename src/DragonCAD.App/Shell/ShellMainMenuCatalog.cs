using System.Collections.ObjectModel;

namespace DragonCAD.App.Shell;

public sealed class ShellMainMenuCatalog
{
    public const string AllContexts = "All";

    private readonly IReadOnlyDictionary<string, ShellMainMenuCommand> commandsById;

    public ShellMainMenuCatalog(IReadOnlyList<ShellMainMenu> menus)
    {
        Menus = new ReadOnlyCollection<ShellMainMenu>(menus.ToArray());
        commandsById = Menus
            .SelectMany(menu => menu.Groups)
            .SelectMany(group => group.Commands)
            .GroupBy(command => command.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    public IReadOnlyList<ShellMainMenu> Menus { get; }

    public static ShellMainMenuCatalog CreateDefault() =>
        new(
            [
                new ShellMainMenu(
                    "File",
                    [
                        new ShellMainMenuGroup(
                            "Project",
                            [
                                Available("dragoncad.file.new-project", "New Project", "file-plus", "Ctrl+N", "NewProjectCommand"),
                                Available("dragoncad.file.open-project-folder", "Open Project Folder", "folder-open", "Ctrl+O", "OpenProjectFolderCommand"),
                                Available("dragoncad.file.save-project", "Save Project", "save", "Ctrl+S", "SaveProjectCommand"),
                                Available("dragoncad.file.save-project-as", "Save Project As", "save-all", "Ctrl+Shift+S", "SaveAsProjectCommand")
                            ]),
                        new ShellMainMenuGroup(
                            "Samples",
                            [
                                Available("dragoncad.file.load-7805-sample", "Load 7805 Sample", "file-input", "", "Load7805SampleCommand"),
                                Available("dragoncad.file.load-arduino-uno-sample", "Load Arduino Uno Sample", "file-input", "", "LoadArduinoUnoSampleCommand")
                            ])
                    ]),
                new ShellMainMenu(
                    "Edit",
                    [
                        new ShellMainMenuGroup(
                            "Selection",
                            [
                                Available("dragoncad.edit.select", "Select", "mouse-pointer-2", "Esc", "ActivateSelectToolCommand", ["Schematic"]),
                                Available("dragoncad.edit.rotate-selection", "Rotate Selection", "rotate-cw", "R", "RotateSelectedPartCommand", ["Schematic"]),
                                Available("dragoncad.edit.mirror-selection", "Mirror Selection", "flip-horizontal", "M", "MirrorSelectedPartCommand", ["Schematic"]),
                                Available("dragoncad.edit.duplicate-selection", "Duplicate Selection", "copy", "Ctrl+D", "DuplicateSelectedPartCommand", ["Schematic"]),
                                Available("dragoncad.edit.delete-selection", "Delete Selection", "trash-2", "Delete", "DeleteActiveSelectionCommand", ["Schematic"])
                            ])
                    ]),
                new ShellMainMenu(
                    "View",
                    [
                        new ShellMainMenuGroup(
                            "Workspaces",
                            [
                                Available("dragoncad.view.component-manager", "Component Manager", "blocks", "Ctrl+0", "ShowComponentManagerTabCommand"),
                                Available("dragoncad.view.schematic", "Schematic", "circuit-board", "Ctrl+1", "ShowSchematicTabCommand"),
                                Available("dragoncad.view.pcb-layout", "PCB Layout", "cpu", "Ctrl+2", "ShowPcbLayoutTabCommand"),
                                Available("dragoncad.view.component-editor", "Component Editor", "package-plus", "Ctrl+3", "ShowComponentEditorTabCommand"),
                                Available("dragoncad.view.fabrication", "Fabrication", "factory", "Ctrl+4", "ShowFabricationTabCommand"),
                                Available("dragoncad.view.help", "Help", "circle-help", "F1", "ShowHelpTabCommand")
                            ]),
                        new ShellMainMenuGroup(
                            "Canvas",
                            [
                                Available("dragoncad.view.fit-active-view", "Fit Active View", "scan", "F", "FitActiveViewCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.center-active-view", "Center Active View", "crosshair", "C", "CenterActiveViewCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.zoom-in", "Zoom In", "zoom-in", "Ctrl++", "ZoomInCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.zoom-out", "Zoom Out", "zoom-out", "Ctrl+-", "ZoomOutCommand", ["Schematic", "PcbLayout"])
                            ]),
                        new ShellMainMenuGroup(
                            "Grid",
                            [
                                Available("dragoncad.view.toggle-grid", "Toggle Grid", "grid-3x3", "G", "ToggleGridVisibilityCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.toggle-grid-style", "Toggle Grid Style", "grid-2x2", "", "ToggleGridStyleCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.increase-grid-spacing", "Increase Grid Spacing", "plus", "", "IncreaseGridSpacingCommand", ["Schematic", "PcbLayout"]),
                                Available("dragoncad.view.decrease-grid-spacing", "Decrease Grid Spacing", "minus", "", "DecreaseGridSpacingCommand", ["Schematic", "PcbLayout"])
                            ])
                    ]),
                new ShellMainMenu(
                    "Place",
                    [
                        new ShellMainMenuGroup(
                            "Schematic",
                            [
                                Available("dragoncad.place.selected-component", "Place Selected Component", "component", "P", "PlaceSelectedComponentCommand", ["Schematic", "ComponentManager", "Marketplace"]),
                                Available("dragoncad.place.armed-component", "Place Armed Part", "package-check", "Enter", "PlaceArmedComponentOnSchematicCommand", ["Schematic"]),
                                Available("dragoncad.place.wire", "Wire", "workflow", "W", "ActivateWireToolCommand", ["Schematic"]),
                                Planned("dragoncad.place.net-label", "Net Label", "tag", "L", "AddNetLabelCommand", ["Schematic"])
                            ])
                    ]),
                new ShellMainMenu(
                    "Route",
                    [
                        new ShellMainMenuGroup(
                            "Board",
                            [
                                Available("dragoncad.route.board-route", "Board Route Tool", "route", "W", "ActivateBoardRouteToolCommand", ["PcbLayout"]),
                                Available("dragoncad.route.finish-route", "Finish Route", "check", "Enter", "FinishBoardRouteCommand", ["PcbLayout"]),
                                Available("dragoncad.route.place-via", "Place Via", "circle-dot", "V", "PlaceBoardViaCommand", ["PcbLayout"]),
                                Available("dragoncad.route.insert-via", "Insert Via In Trace", "circle-plus", "Shift+V", "InsertBoardViaIntoSelectedTraceSegmentCommand", ["PcbLayout"]),
                                Planned("dragoncad.route.ripup", "Ripup", "undo-2", "U", "RipupSelectedTraceCommand", ["PcbLayout"]),
                                Planned("dragoncad.route.autoroute", "Autoroute", "wand-sparkles", "", "AutorouteCommand", ["PcbLayout"])
                            ])
                    ]),
                new ShellMainMenu(
                    "Inspect",
                    [
                        new ShellMainMenuGroup(
                            "Board",
                            [
                                Available("dragoncad.inspect.toggle-layer-visibility", "Toggle Layer Visibility", "eye", "Shift+E", "ToggleSelectedBoardLayerVisibilityCommand", ["PcbLayout"]),
                                Available("dragoncad.inspect.move-trace-to-layer", "Move Trace To Active Layer", "layers", "Shift+L", "MoveSelectedBoardTraceToLayerCommand", ["PcbLayout"])
                            ]),
                        new ShellMainMenuGroup(
                            "Design",
                            [
                                Available("dragoncad.inspect.datasheet-package", "Validate Datasheet Package", "file-check-2", "", "ValidateDatasheetPromotionPackageCommand", ["Datasheets", "Fabrication"]),
                                Planned("dragoncad.inspect.component-draft", "Validate Component Draft", "badge-check", "", "ValidateComponentDraftCommand", ["ComponentEditor"])
                            ])
                    ]),
                new ShellMainMenu(
                    "Tools",
                    [
                        new ShellMainMenuGroup(
                            "Libraries",
                            [
                                Available("dragoncad.tools.search-library", "Search Library", "search", "Ctrl+F", "SearchLibraryCommand", ["ComponentManager", "Schematic"]),
                                Available("dragoncad.tools.new-component", "New Component", "package-plus", "", "NewComponentEditorCommand", ["ComponentManager", "ComponentEditor"]),
                                Available("dragoncad.tools.open-component-editor", "Open Selected Component Editor", "package-open", "", "OpenSelectedComponentEditorCommand", ["ComponentManager", "Schematic"]),
                                Available("dragoncad.tools.add-starter-geometry", "Add Starter Geometry", "shapes", "", "AddComponentEditorStarterGeometryCommand", ["ComponentEditor"])
                            ]),
                        new ShellMainMenuGroup(
                            "Marketplace",
                            [
                                Available("dragoncad.tools.prepare-marketplace-bom", "Prepare Marketplace BOM CSV", "sheet", "", "PrepareMarketplaceBomCsvCommand", ["Marketplace"]),
                                Available("dragoncad.tools.create-marketplace-order", "Create Marketplace Order Draft", "shopping-cart", "", "CreateMarketplaceOrderDraftCommand", ["Marketplace"]),
                                Available("dragoncad.tools.sync-vendor-catalogs", "Sync Vendor Catalogs", "refresh-cw", "", "RunVendorCatalogSyncCommand", ["Marketplace"]),
                                Available("dragoncad.tools.vendor-live-smoke", "Vendor Live Smoke", "activity", "", "RunInUseVendorCatalogSyncCommand", ["Marketplace"])
                            ]),
                        new ShellMainMenuGroup(
                            "Fabrication",
                            [
                                Planned("dragoncad.tools.export-manufacturing-package", "Export Manufacturing Package", "archive", "", "ExportManufacturingPackageCommand", ["Fabrication"]),
                                Planned("dragoncad.tools.submit-fabrication-order", "Submit Fabrication Order", "send", "", "SubmitFabricationOrderCommand", ["Fabrication"])
                            ])
                    ]),
                new ShellMainMenu(
                    "AI",
                    [
                        new ShellMainMenuGroup(
                            "Assistant",
                            [
                                Available("dragoncad.ai.submit-prompt", "Submit Prompt", "sparkles", "Ctrl+Enter", "SubmitAiPromptCommand")
                            ])
                    ]),
                new ShellMainMenu(
                    "Window",
                    [
                        new ShellMainMenuGroup(
                            "Workspace Layouts",
                            [
                                Planned("dragoncad.window.schematic-focus", "Schematic Focus", "panel-left", "", "ApplySchematicFocusLayoutCommand"),
                                Planned("dragoncad.window.pcb-focus", "PCB Focus", "panel-right", "", "ApplyPcbFocusLayoutCommand"),
                                Planned("dragoncad.window.component-authoring", "Component Authoring", "panel-top", "", "ApplyComponentAuthoringLayoutCommand"),
                                Planned("dragoncad.window.manufacturing-review", "Manufacturing Review", "panel-bottom", "", "ApplyManufacturingReviewLayoutCommand")
                            ])
                    ]),
                new ShellMainMenu(
                    "Help",
                    [
                        new ShellMainMenuGroup(
                            "Documentation",
                            [
                                Available("dragoncad.help.open-help", "Open Help", "circle-help", "F1", "ShowHelpTabCommand")
                            ])
                    ])
            ]);

    public ShellMainMenuCommand GetRequiredCommand(string id) =>
        commandsById.TryGetValue(id, out ShellMainMenuCommand? command)
            ? command
            : throw new InvalidOperationException($"Unknown shell menu command: {id}.");

    public ShellMainMenuView CreateView(string? activeWorkspaceTab)
    {
        string normalizedContext = string.IsNullOrWhiteSpace(activeWorkspaceTab)
            ? AllContexts
            : activeWorkspaceTab.Trim();

        return new ShellMainMenuView(
            Menus
                .Select(menu => menu with
                {
                    Groups = menu.Groups
                        .Select(group => group with
                        {
                            Commands = group.Commands
                                .Where(command => command.IsVisibleInContext(normalizedContext))
                                .ToArray()
                        })
                        .Where(group => group.Commands.Count > 0)
                        .ToArray()
                })
                .Where(menu => menu.Groups.Count > 0)
                .ToArray());
    }

    private static ShellMainMenuCommand Available(
        string id,
        string label,
        string iconId,
        string shortcutText,
        string commandHandlerName,
        IReadOnlyList<string>? visibleInWorkspaceTabs = null) =>
        ShellMainMenuCommand.Available(
            id,
            label,
            iconId,
            shortcutText,
            commandHandlerName,
            visibleInWorkspaceTabs ?? [AllContexts]);

    private static ShellMainMenuCommand Planned(
        string id,
        string label,
        string iconId,
        string shortcutText,
        string commandHandlerName,
        IReadOnlyList<string>? visibleInWorkspaceTabs = null) =>
        ShellMainMenuCommand.Planned(
            id,
            label,
            iconId,
            shortcutText,
            commandHandlerName,
            visibleInWorkspaceTabs ?? [AllContexts]);
}

public sealed record ShellMainMenu(string Label, IReadOnlyList<ShellMainMenuGroup> Groups);

public sealed record ShellMainMenuGroup(string Label, IReadOnlyList<ShellMainMenuCommand> Commands);

public sealed record ShellMainMenuCommand(
    string Id,
    string Label,
    string IconId,
    string ShortcutText,
    bool IsEnabled,
    string DisabledReason,
    string CommandHandlerName,
    IReadOnlyList<string> VisibleInWorkspaceTabs)
{
    public static ShellMainMenuCommand Available(
        string id,
        string label,
        string iconId,
        string shortcutText,
        string commandHandlerName,
        IReadOnlyList<string> visibleInWorkspaceTabs) =>
        new(id, label, iconId, shortcutText, true, "", commandHandlerName, visibleInWorkspaceTabs);

    public static ShellMainMenuCommand Planned(
        string id,
        string label,
        string iconId,
        string shortcutText,
        string commandHandlerName,
        IReadOnlyList<string> visibleInWorkspaceTabs) =>
        new(
            id,
            label,
            iconId,
            shortcutText,
            false,
            "Planned command: handler is not implemented yet.",
            commandHandlerName,
            visibleInWorkspaceTabs);

    public bool IsVisibleInContext(string activeWorkspaceTab) =>
        VisibleInWorkspaceTabs.Contains(ShellMainMenuCatalog.AllContexts, StringComparer.Ordinal) ||
        VisibleInWorkspaceTabs.Contains(activeWorkspaceTab, StringComparer.Ordinal);
}

public sealed record ShellMainMenuView(IReadOnlyList<ShellMainMenu> Menus)
{
    public IReadOnlyList<ShellMainMenuCommand> VisibleCommands { get; } = Menus
        .SelectMany(menu => menu.Groups)
        .SelectMany(group => group.Commands)
        .ToArray();
}

public static class ShellMainMenuValidator
{
    public static ShellMainMenuValidationResult Validate(ShellMainMenuCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        ShellMainMenuDiagnostic[] diagnostics = catalog.Menus
            .SelectMany(menu => menu.Groups)
            .SelectMany(group => group.Commands)
            .GroupBy(command => command.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new ShellMainMenuDiagnostic("duplicate-command-id", group.Key))
            .ToArray();

        return new ShellMainMenuValidationResult(diagnostics);
    }
}

public sealed record ShellMainMenuValidationResult(IReadOnlyList<ShellMainMenuDiagnostic> Diagnostics);

public sealed record ShellMainMenuDiagnostic(string Code, string CommandId);
