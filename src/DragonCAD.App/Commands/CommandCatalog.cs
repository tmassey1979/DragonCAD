using System.Text;

namespace DragonCAD.App.Commands;

public sealed record CommandCatalog(IReadOnlyList<CommandCatalogEntry> Entries)
{
    public static CommandCatalog Default { get; } = new(
        [
            new("NewProjectCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Reset the workspace to a fresh project.", ["Ctrl+N"], ["NEW"], ["File > New Project"]),
            new("OpenProjectFolderCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Open an existing DragonCAD project folder.", ["Ctrl+O"], ["OPEN"], ["File > Open Project Folder"]),
            new("SaveProjectCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Save the current project to its existing folder.", ["Ctrl+S"], ["SAVE"], ["File > Save"]),
            new("SaveAsProjectCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Save the current project to a selected folder.", ["Ctrl+Shift+S"], ["SAVEAS"], ["File > Save As"]),
            new("Load7805SampleCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Load the 7805 regulator teaching sample.", [], ["LOAD7805"], ["File > Load 7805 Sample", "Schematic toolbar > 7805"]),
            new("LoadArduinoUnoSampleCommand", CommandCatalogScopes.Project, CommandCatalogStatuses.Available, "#31", "Load the Arduino Uno Rev3 reference sample.", [], ["LOADUNO"], ["File > Load Arduino Uno Sample", "Schematic toolbar > Uno"]),

            new("ShowSchematicTabCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Open the schematic workspace.", ["Ctrl+1"], ["SCH"], ["View > Schematic", "Project Explorer > Design > Schematics"]),
            new("ShowPcbLayoutTabCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Open the PCB layout workspace.", ["Ctrl+2"], ["BOARD"], ["View > PCB Layout", "Project Explorer > Design > PCB Layouts"]),
            new("ShowComponentEditorTabCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Open the component editor workspace.", ["Ctrl+3"], ["LIBEDIT"], ["View > Component Editor"]),
            new("ShowFabricationTabCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Open fabrication handoff and manufacturing review.", ["Ctrl+4"], ["FAB"], ["Fabrication > Open Fabrication Workspace", "Project Explorer > Manufacturing > Fabrication"]),
            new("FitActiveViewCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Fit the active schematic or PCB view.", ["F"], ["FIT"], ["View > Fit Active View", "Editor toolbar > Fit"]),
            new("CenterActiveViewCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Available, "#31", "Center the active schematic or PCB view.", ["C"], ["CENTER"], ["View > Center Active View", "Editor toolbar > Center"]),
            new("ZoomInCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Missing, "#31", "Zoom into the active editor view.", ["Ctrl++"], ["ZOOMIN"], ["View > Zoom In"]),
            new("ZoomOutCommand", CommandCatalogScopes.View, CommandCatalogStatuses.Missing, "#31", "Zoom out of the active editor view.", ["Ctrl+-"], ["ZOOMOUT"], ["View > Zoom Out"]),

            new("SearchLibraryCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Search the effective component catalog.", ["Ctrl+F"], ["SEARCH"], ["Component Manager > Search"]),
            new("PlaceSelectedComponentCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Arm the selected component for schematic placement.", ["P"], ["ADD", "PLACE"], ["Place > Place Selected Component", "Editor toolbar > Place"]),
            new("PlaceArmedComponentOnSchematicCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Place the armed component at the default schematic point.", ["Enter"], ["DROP"], ["Place > Place Armed Part on Schematic"]),
            new("ActivateSelectToolCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Switch schematic interaction to selection and movement.", ["Esc"], ["SELECT"], ["Edit > Select", "Schematic toolbar > Select"]),
            new("ActivateWireToolCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Start pin-to-pin schematic wiring.", ["W"], ["WIRE", "NET"], ["Place > Activate Wire Tool", "Schematic toolbar > Wire"]),
            new("RotateSelectedPartCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Rotate the selected schematic part and synced board footprint.", ["R"], ["ROTATE"], ["Edit > Rotate Selection", "Schematic toolbar > Rot 90"]),
            new("MirrorSelectedPartCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Mirror the selected schematic part and synced board footprint.", ["M"], ["MIRROR"], ["Edit > Mirror Selection", "Schematic inspector > Mirror"]),
            new("DuplicateSelectedPartCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Duplicate the selected schematic part.", ["Ctrl+D"], ["DUPLICATE", "COPY"], ["Edit > Duplicate Selection"]),
            new("DeleteActiveSelectionCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Delete the active schematic or board selection.", ["Delete"], ["DELETE"], ["Edit > Delete Selection"]),
            new("CancelPlacementCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Available, "#31", "Cancel armed schematic placement.", ["Shift+Esc"], ["CANCEL"], ["Schematic toolbar > Select"]),
            new("AddNetLabelCommand", CommandCatalogScopes.Schematic, CommandCatalogStatuses.Missing, "#31", "Attach a visible net label to a schematic wire.", ["L"], ["LABEL"], ["Place > Net Label"]),

            new("ActivateBoardSelectToolCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Switch board interaction to selection and movement.", ["Esc"], ["SELECT"], ["PCB toolbar > Select"]),
            new("ActivateBoardRouteToolCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Start PCB trace routing on the active layer.", ["W"], ["ROUTE"], ["Route > Board Route Tool", "PCB toolbar > Route"]),
            new("FinishBoardRouteCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Complete the active board route.", ["Enter"], ["FINISH"], ["Route > Finish Board Route", "PCB toolbar > Finish"]),
            new("PlaceBoardViaCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Place a via and switch routing layers.", ["V"], ["VIA"], ["Route > Place Board Via", "PCB toolbar > Via"]),
            new("InsertBoardViaIntoSelectedTraceSegmentCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Split the selected trace segment with a via.", ["Shift+V"], ["INSERTVIA"], ["PCB inspector > Insert Via In Trace"]),
            new("MoveSelectedBoardTraceToLayerCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Move the selected trace to the active copper layer.", ["Shift+L"], ["CHANGELAYER"], ["PCB toolbar > Apply", "PCB inspector > Move Trace To Active Layer"]),
            new("DeleteBoardSelectionCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Available, "#31", "Delete the selected board object.", ["Delete"], ["DELETE"], ["PCB toolbar > Del", "PCB inspector > Delete PCB Selection"]),
            new("RipupSelectedTraceCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Missing, "#31", "Rip up selected routed copper back to an airwire.", ["U"], ["RIPUP"], ["Route > Ripup"]),
            new("AutorouteCommand", CommandCatalogScopes.Board, CommandCatalogStatuses.Missing, "#31", "Run automatic trace routing for the current board.", [], ["AUTO"], ["Route > Autoroute"]),

            new("ToggleSelectedBoardLayerVisibilityCommand", CommandCatalogScopes.Layer, CommandCatalogStatuses.Available, "#31", "Toggle active board layer visibility.", ["Shift+E"], ["DISPLAY"], ["PCB toolbar > Eye"]),
            new("SetActiveBoardLayerCommand", CommandCatalogScopes.Layer, CommandCatalogStatuses.Available, "#31", "Set the board layer used for new or moved traces.", [], ["LAYER"], ["PCB inspector > Active Layer"]),
            new("OpenLayerSettingsCommand", CommandCatalogScopes.Layer, CommandCatalogStatuses.Missing, "#31", "Open a complete layer visibility and color settings panel.", [], ["LAYERS"], ["View > Layers"]),

            new("SetGridSizeCommand", CommandCatalogScopes.Grid, CommandCatalogStatuses.Missing, "#31", "Set the editor grid spacing.", [], ["GRID"], ["View > Grid"]),
            new("ToggleGridSnapCommand", CommandCatalogScopes.Grid, CommandCatalogStatuses.Missing, "#31", "Toggle snap-to-grid editing.", [], ["SNAP"], ["View > Snap"]),
            new("ToggleGridVisibilityCommand", CommandCatalogScopes.Grid, CommandCatalogStatuses.Missing, "#31", "Show or hide the editor grid.", ["G"], ["GRIDON"], ["View > Show Grid"]),

            new("NewComponentEditorCommand", CommandCatalogScopes.ComponentEditor, CommandCatalogStatuses.Available, "#31", "Create a new component editor draft.", [], ["LIBNEW"], ["Component Editor > New"]),
            new("OpenSelectedComponentEditorCommand", CommandCatalogScopes.ComponentEditor, CommandCatalogStatuses.Available, "#31", "Open the selected catalog component in the editor.", [], ["OPENDEV"], ["Component Manager > Open Editor"]),
            new("AddComponentEditorStarterGeometryCommand", CommandCatalogScopes.ComponentEditor, CommandCatalogStatuses.Available, "#31", "Add starter symbol and footprint geometry to the active component draft.", [], ["STARTGEOM"], ["Component Editor > Add Starter Geometry"]),
            new("ValidateComponentDraftCommand", CommandCatalogScopes.ComponentEditor, CommandCatalogStatuses.Missing, "#31", "Validate pins, symbols, footprints, and package mapping before saving a component.", [], ["LIBCHECK"], ["Component Editor > Validate"]),

            new("PrepareMarketplaceBomCsvCommand", CommandCatalogScopes.Marketplace, CommandCatalogStatuses.Available, "#31", "Prepare deterministic BOM CSV text from the cart.", [], ["BOM"], ["File > Prepare Marketplace BOM CSV", "Marketplace > Prepare BOM CSV"]),
            new("CreateMarketplaceOrderDraftCommand", CommandCatalogScopes.Marketplace, CommandCatalogStatuses.Available, "#31", "Create an in-app checkout draft for review.", [], ["ORDER"], ["File > Create Marketplace Order Draft", "Marketplace > Create Order Draft"]),
            new("SyncVendorCatalogsCommand", CommandCatalogScopes.Marketplace, CommandCatalogStatuses.Available, "#31", "Run configured vendor catalog syncs.", [], ["SYNC"], ["Marketplace > Sync Vendor Catalogs"]),
            new("RunVendorLiveSmokeCommand", CommandCatalogScopes.Marketplace, CommandCatalogStatuses.Available, "#31", "Run gated live vendor smoke checks.", [], ["SMOKE"], ["Marketplace > Vendor Live Smoke"]),

            new("ValidateDatasheetPromotionPackageCommand", CommandCatalogScopes.Fabrication, CommandCatalogStatuses.Available, "#31", "Validate a saved datasheet promotion package.", [], ["DATASHEETCHECK"], ["Fabrication > Validate Datasheet Package"]),
            new("ExportManufacturingPackageCommand", CommandCatalogScopes.Fabrication, CommandCatalogStatuses.Available, "#31", "Export Gerber, drill, BOM, pick-and-place, and handoff manifests.", [], ["CAM"], ["Fabrication > Export Manufacturing Package"]),
            new("ReviewFabricationOrderCommand", CommandCatalogScopes.Fabrication, CommandCatalogStatuses.Available, "#31", "Review fabrication order readiness before submission.", [], ["DRC"], ["Fabrication > Review Order"]),
            new("SubmitFabricationOrderCommand", CommandCatalogScopes.Fabrication, CommandCatalogStatuses.Missing, "#31", "Submit a fabrication order to a configured manufacturing provider.", [], ["SUBMIT"], ["Fabrication > Submit Order"])
        ]);
}

public sealed record CommandCatalogEntry(
    string CommandName,
    string Scope,
    string Status,
    string GitHubIssue,
    string Description,
    IReadOnlyList<string> Shortcuts,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> UiLocations);

public static class CommandCatalogScopes
{
    public const string Project = "Project";
    public const string Schematic = "Schematic";
    public const string Board = "Board";
    public const string ComponentEditor = "Component editor";
    public const string View = "View";
    public const string Grid = "Grid";
    public const string Layer = "Layer";
    public const string Marketplace = "Marketplace";
    public const string Fabrication = "Fabrication";
}

public static class CommandCatalogStatuses
{
    public const string Available = "Available";
    public const string Planned = "Planned";
    public const string Missing = "Missing";

    public static IReadOnlySet<string> ValidValues { get; } = new HashSet<string>(
        [Available, Planned, Missing],
        StringComparer.OrdinalIgnoreCase);
}

public static class CommandCatalogMarkdownRenderer
{
    public static string Render(CommandCatalog catalog)
    {
        StringBuilder builder = new();
        builder.AppendLine("# Tool and Shortcut Catalog");
        builder.AppendLine();
        builder.AppendLine("This page is generated from the read-only command catalog metadata used by in-app help.");
        builder.AppendLine();
        builder.AppendLine("| Command | Scope | Status | Shortcuts | Aliases | GitHub issue | UI locations | Purpose |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (CommandCatalogEntry entry in catalog.Entries.OrderBy(entry => entry.Scope).ThenBy(entry => entry.CommandName))
        {
            builder.Append("| `").Append(entry.CommandName).Append("` | ");
            builder.Append(Escape(entry.Scope)).Append(" | ");
            builder.Append(Escape(entry.Status)).Append(" | ");
            builder.Append(FormatTokens(entry.Shortcuts)).Append(" | ");
            builder.Append(FormatTokens(entry.Aliases)).Append(" | ");
            builder.Append(Escape(entry.GitHubIssue)).Append(" | ");
            builder.Append(Escape(string.Join("; ", entry.UiLocations))).Append(" | ");
            builder.Append(Escape(entry.Description)).AppendLine(" |");
        }

        return builder.ToString();
    }

    private static string FormatTokens(IReadOnlyList<string> values) =>
        values.Count == 0
            ? "-"
            : string.Join(", ", values.Select(value => $"`{Escape(value)}`"));

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
