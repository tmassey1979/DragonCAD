# Command and Shortcut Reference

Review common DragonCAD commands, shortcut conventions, and command availability by workspace context.

Command reference pages should stay short, searchable, and aligned with in-app command names.

## Project and samples

| Command | UI location | Purpose |
| --- | --- | --- |
| `NewProjectCommand` | File > New Project | Reset the current workspace to a fresh project. |
| `OpenProjectFolderCommand` | File > Open Project Folder | Load a saved DragonCAD project folder. |
| `SaveProjectCommand` | File > Save | Save the current project to its existing folder. |
| `SaveAsProjectCommand` | File > Save As | Save the current project to a selected folder. |
| `Load7805SampleCommand` | File > Load 7805 Sample; Schematic toolbar > 7805 | Load the 7805 regulator teaching sample. |
| `LoadArduinoUnoSampleCommand` | File > Load Arduino Uno Sample; Schematic toolbar > Uno | Load the Arduino Uno Rev3 reference sample. |

## Schematic editor

| Command | UI location | Purpose |
| --- | --- | --- |
| `ShowSchematicTabCommand` | View > Schematic; left Project Explorer > Design > Schematics | Open the schematic workspace. |
| `SearchLibraryCommand` | Component Manager > Search | Search the effective component catalog. |
| `PlaceSelectedComponentCommand` | Place > Place Selected Component; editor toolbar > Place | Arm the selected component for schematic placement. |
| `PlaceArmedComponentOnSchematicCommand` | Place > Place Armed Part on Schematic | Place the armed component at the default schematic point. |
| `ActivateSelectToolCommand` | Edit > Select; Schematic toolbar > Select | Switch schematic interaction to selection and movement. |
| `ActivateWireToolCommand` | Place > Activate Wire Tool; Schematic toolbar > Wire | Start pin-to-pin schematic wiring. |
| `RotateSelectedPartCommand` | Edit > Rotate Selection; Schematic toolbar > Rot 90 | Rotate the selected schematic part and synced board footprint. |
| `MirrorSelectedPartCommand` | Edit > Mirror Selection; Schematic inspector > Mirror | Mirror the selected schematic part and synced board footprint. |
| `DuplicateSelectedPartCommand` | Edit > Duplicate Selection | Duplicate the selected schematic part. |
| `DeleteActiveSelectionCommand` | Edit > Delete Selection; Delete shortcut | Delete the active schematic or board selection. |
| `FitActiveViewCommand` | View > Fit Active View; editor toolbar > Fit | Fit the active schematic or PCB view. |
| `CenterActiveViewCommand` | View > Center Active View; editor toolbar > Center | Center the active schematic or PCB view. |

## PCB layout

| Command | UI location | Purpose |
| --- | --- | --- |
| `ShowPcbLayoutTabCommand` | View > PCB Layout; left Project Explorer > Design > PCB Layouts | Open the PCB layout workspace. |
| `ActivateBoardSelectToolCommand` | PCB toolbar > Select | Switch board interaction to selection and movement. |
| `ActivateBoardRouteToolCommand` | Route > Board Route Tool; PCB toolbar > Route | Start PCB trace routing on the active layer. |
| `FinishBoardRouteCommand` | Route > Finish Board Route; PCB toolbar > Finish | Complete the active board route. |
| `PlaceBoardViaCommand` | Route > Place Board Via; PCB toolbar > Via | Place a via and switch routing layers. |
| `InsertBoardViaIntoSelectedTraceSegmentCommand` | PCB inspector > Insert Via In Trace | Split the selected trace segment with a via. |
| `MoveSelectedBoardTraceToLayerCommand` | PCB toolbar > Apply; PCB inspector > Move Trace To Active Layer | Move the selected trace to the active copper layer. |
| `ToggleSelectedBoardLayerVisibilityCommand` | PCB toolbar > Eye | Toggle active board layer visibility. |
| `DeleteBoardSelectionCommand` | PCB toolbar > Del; PCB inspector > Delete PCB Selection | Delete the selected board object. |

## Fabrication and review

| Command | UI location | Purpose |
| --- | --- | --- |
| `ShowFabricationTabCommand` | Fabrication > Open Fabrication Workspace; left Project Explorer > Manufacturing > Fabrication | Open fabrication handoff and manufacturing review. |
| `PrepareMarketplaceBomCsvCommand` | File > Prepare Marketplace BOM CSV; Marketplace > Prepare BOM CSV | Prepare deterministic BOM CSV text from the cart. |
| `CreateMarketplaceOrderDraftCommand` | File > Create Marketplace Order Draft; Marketplace > Create Order Draft | Create an in-app checkout draft for review. |
| `ValidateDatasheetPromotionPackageCommand` | Fabrication > Validate Datasheet Package | Validate a saved datasheet promotion package. |
