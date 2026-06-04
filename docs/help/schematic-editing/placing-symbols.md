# Schematic Placement

Use schematic placement to turn a selected local or marketplace-safe component into a symbol on the active sheet. Placement stays inside DragonCAD and keeps the component's identity available for board sync, BOM planning, and fabrication review.

## Placement workflow

- Open the schematic workspace with `ShowSchematicTabCommand`.
- Search the effective component catalog with `SearchLibraryCommand`.
- Select a component row and arm it with `PlaceSelectedComponentCommand`.
- Drop the armed component on the sheet with a canvas click or `PlaceArmedComponentOnSchematicCommand`.
- Return to selection with `ActivateSelectToolCommand` or cancel armed placement with `CancelPlacementCommand`.

| Step | Check |
| --- | --- |
| Select component | Confirm the row has the intended value, package, and source. |
| Arm placement | The placement status should name the component waiting to be placed. |
| Drop symbol | The schematic gains a component instance and the PCB receives a synced footprint. |
| Review | Use the inspector before wiring so misplaced or wrong-package parts are caught early. |

For sourced parts, review [Marketplace-safe placement](../marketplace/safe-placement.md) before placing them into a design.
