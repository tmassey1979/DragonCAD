# Tool and Shortcut Catalog

This page is generated from the read-only command catalog metadata used by in-app help.

| Command | Scope | Status | Shortcuts | Aliases | GitHub issue | UI locations | Purpose |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ActivateBoardRouteToolCommand` | Board | Available | `W` | `ROUTE` | #31 | Route > Board Route Tool; PCB toolbar > Route | Start PCB trace routing on the active layer. |
| `ActivateBoardSelectToolCommand` | Board | Available | `Esc` | `SELECT` | #31 | PCB toolbar > Select | Switch board interaction to selection and movement. |
| `AutorouteCommand` | Board | Missing | - | `AUTO` | #31 | Route > Autoroute | Run automatic trace routing for the current board. |
| `DeleteBoardSelectionCommand` | Board | Available | `Delete` | `DELETE` | #31 | PCB toolbar > Del; PCB inspector > Delete PCB Selection | Delete the selected board object. |
| `FinishBoardRouteCommand` | Board | Available | `Enter` | `FINISH` | #31 | Route > Finish Board Route; PCB toolbar > Finish | Complete the active board route. |
| `InsertBoardViaIntoSelectedTraceSegmentCommand` | Board | Available | `Shift+V` | `INSERTVIA` | #31 | PCB inspector > Insert Via In Trace | Split the selected trace segment with a via. |
| `MoveSelectedBoardTraceToLayerCommand` | Board | Available | `Shift+L` | `CHANGELAYER` | #31 | PCB toolbar > Apply; PCB inspector > Move Trace To Active Layer | Move the selected trace to the active copper layer. |
| `PlaceBoardViaCommand` | Board | Available | `V` | `VIA` | #31 | Route > Place Board Via; PCB toolbar > Via | Place a via and switch routing layers. |
| `RipupSelectedTraceCommand` | Board | Missing | `U` | `RIPUP` | #31 | Route > Ripup | Rip up selected routed copper back to an airwire. |
| `AddComponentEditorStarterGeometryCommand` | Component editor | Available | - | `STARTGEOM` | #31 | Component Editor > Add Starter Geometry | Add starter symbol and footprint geometry to the active component draft. |
| `NewComponentEditorCommand` | Component editor | Available | - | `LIBNEW` | #31 | Component Editor > New | Create a new component editor draft. |
| `OpenSelectedComponentEditorCommand` | Component editor | Available | - | `OPENDEV` | #31 | Component Manager > Open Editor | Open the selected catalog component in the editor. |
| `ValidateComponentDraftCommand` | Component editor | Missing | - | `LIBCHECK` | #31 | Component Editor > Validate | Validate pins, symbols, footprints, and package mapping before saving a component. |
| `ExportManufacturingPackageCommand` | Fabrication | Available | - | `CAM` | #31 | Fabrication > Export Manufacturing Package | Export Gerber, drill, BOM, pick-and-place, and handoff manifests. |
| `ReviewFabricationOrderCommand` | Fabrication | Available | - | `DRC` | #31 | Fabrication > Review Order | Review fabrication order readiness before submission. |
| `SubmitFabricationOrderCommand` | Fabrication | Missing | - | `SUBMIT` | #31 | Fabrication > Submit Order | Submit a fabrication order to a configured manufacturing provider. |
| `ValidateDatasheetPromotionPackageCommand` | Fabrication | Available | - | `DATASHEETCHECK` | #31 | Fabrication > Validate Datasheet Package | Validate a saved datasheet promotion package. |
| `SetGridSizeCommand` | Grid | Missing | - | `GRID` | #31 | View > Grid | Set the editor grid spacing. |
| `ToggleGridSnapCommand` | Grid | Missing | - | `SNAP` | #31 | View > Snap | Toggle snap-to-grid editing. |
| `ToggleGridVisibilityCommand` | Grid | Missing | `G` | `GRIDON` | #31 | View > Show Grid | Show or hide the editor grid. |
| `OpenLayerSettingsCommand` | Layer | Missing | - | `LAYERS` | #31 | View > Layers | Open a complete layer visibility and color settings panel. |
| `SetActiveBoardLayerCommand` | Layer | Available | - | `LAYER` | #31 | PCB inspector > Active Layer | Set the board layer used for new or moved traces. |
| `ToggleSelectedBoardLayerVisibilityCommand` | Layer | Available | `Shift+E` | `DISPLAY` | #31 | PCB toolbar > Eye | Toggle active board layer visibility. |
| `CreateMarketplaceOrderDraftCommand` | Marketplace | Available | - | `ORDER` | #31 | File > Create Marketplace Order Draft; Marketplace > Create Order Draft | Create an in-app checkout draft for review. |
| `PrepareMarketplaceBomCsvCommand` | Marketplace | Available | - | `BOM` | #31 | File > Prepare Marketplace BOM CSV; Marketplace > Prepare BOM CSV | Prepare deterministic BOM CSV text from the cart. |
| `RunVendorLiveSmokeCommand` | Marketplace | Available | - | `SMOKE` | #31 | Marketplace > Vendor Live Smoke | Run gated live vendor smoke checks. |
| `SyncVendorCatalogsCommand` | Marketplace | Available | - | `SYNC` | #31 | Marketplace > Sync Vendor Catalogs | Run configured vendor catalog syncs. |
| `Load7805SampleCommand` | Project | Available | - | `LOAD7805` | #31 | File > Load 7805 Sample; Schematic toolbar > 7805 | Load the 7805 regulator teaching sample. |
| `LoadArduinoUnoSampleCommand` | Project | Available | - | `LOADUNO` | #31 | File > Load Arduino Uno Sample; Schematic toolbar > Uno | Load the Arduino Uno Rev3 reference sample. |
| `NewProjectCommand` | Project | Available | `Ctrl+N` | `NEW` | #31 | File > New Project | Reset the workspace to a fresh project. |
| `OpenProjectFolderCommand` | Project | Available | `Ctrl+O` | `OPEN` | #31 | File > Open Project Folder | Open an existing DragonCAD project folder. |
| `SaveAsProjectCommand` | Project | Available | `Ctrl+Shift+S` | `SAVEAS` | #31 | File > Save As | Save the current project to a selected folder. |
| `SaveProjectCommand` | Project | Available | `Ctrl+S` | `SAVE` | #31 | File > Save | Save the current project to its existing folder. |
| `ActivateSelectToolCommand` | Schematic | Available | `Esc` | `SELECT` | #31 | Edit > Select; Schematic toolbar > Select | Switch schematic interaction to selection and movement. |
| `ActivateWireToolCommand` | Schematic | Available | `W` | `WIRE`, `NET` | #31 | Place > Activate Wire Tool; Schematic toolbar > Wire | Start pin-to-pin schematic wiring. |
| `AddNetLabelCommand` | Schematic | Missing | `L` | `LABEL` | #31 | Place > Net Label | Attach a visible net label to a schematic wire. |
| `CancelPlacementCommand` | Schematic | Available | `Shift+Esc` | `CANCEL` | #31 | Schematic toolbar > Select | Cancel armed schematic placement. |
| `DeleteActiveSelectionCommand` | Schematic | Available | `Delete` | `DELETE` | #31 | Edit > Delete Selection | Delete the active schematic or board selection. |
| `DuplicateSelectedPartCommand` | Schematic | Available | `Ctrl+D` | `DUPLICATE`, `COPY` | #31 | Edit > Duplicate Selection | Duplicate the selected schematic part. |
| `MirrorSelectedPartCommand` | Schematic | Available | `M` | `MIRROR` | #31 | Edit > Mirror Selection; Schematic inspector > Mirror | Mirror the selected schematic part and synced board footprint. |
| `PlaceArmedComponentOnSchematicCommand` | Schematic | Available | `Enter` | `DROP` | #31 | Place > Place Armed Part on Schematic | Place the armed component at the default schematic point. |
| `PlaceSelectedComponentCommand` | Schematic | Available | `P` | `ADD`, `PLACE` | #31 | Place > Place Selected Component; Editor toolbar > Place | Arm the selected component for schematic placement. |
| `RotateSelectedPartCommand` | Schematic | Available | `R` | `ROTATE` | #31 | Edit > Rotate Selection; Schematic toolbar > Rot 90 | Rotate the selected schematic part and synced board footprint. |
| `SearchLibraryCommand` | Schematic | Available | `Ctrl+F` | `SEARCH` | #31 | Component Manager > Search | Search the effective component catalog. |
| `CenterActiveViewCommand` | View | Available | `C` | `CENTER` | #31 | View > Center Active View; Editor toolbar > Center | Center the active schematic or PCB view. |
| `FitActiveViewCommand` | View | Available | `F` | `FIT` | #31 | View > Fit Active View; Editor toolbar > Fit | Fit the active schematic or PCB view. |
| `ShowComponentEditorTabCommand` | View | Available | `Ctrl+3` | `LIBEDIT` | #31 | View > Component Editor | Open the component editor workspace. |
| `ShowFabricationTabCommand` | View | Available | `Ctrl+4` | `FAB` | #31 | Fabrication > Open Fabrication Workspace; Project Explorer > Manufacturing > Fabrication | Open fabrication handoff and manufacturing review. |
| `ShowPcbLayoutTabCommand` | View | Available | `Ctrl+2` | `BOARD` | #31 | View > PCB Layout; Project Explorer > Design > PCB Layouts | Open the PCB layout workspace. |
| `ShowSchematicTabCommand` | View | Available | `Ctrl+1` | `SCH` | #31 | View > Schematic; Project Explorer > Design > Schematics | Open the schematic workspace. |
| `ZoomInCommand` | View | Missing | `Ctrl++` | `ZOOMIN` | #31 | View > Zoom In | Zoom into the active editor view. |
| `ZoomOutCommand` | View | Missing | `Ctrl+-` | `ZOOMOUT` | #31 | View > Zoom Out | Zoom out of the active editor view. |
