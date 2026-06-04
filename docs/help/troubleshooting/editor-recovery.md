# Editor Recovery

Use editor recovery steps when the schematic or PCB view looks stale, a tool stays armed, or connectivity does not match what you expected. These checks stay inside the app and do not rely on external browser help.

## Fast recovery checks

- Return to selection with `ActivateSelectToolCommand` or `ActivateBoardSelectToolCommand`.
- Cancel armed schematic placement with `CancelPlacementCommand`.
- Fit and center the active view with `FitActiveViewCommand` and `CenterActiveViewCommand`.
- Check whether layer visibility is hiding selected copper with `ToggleSelectedBoardLayerVisibilityCommand`.
- Delete only the active bad selection with `DeleteActiveSelectionCommand` or `DeleteBoardSelectionCommand`.

| Problem | First local check |
| --- | --- |
| Component will not place | Confirm a catalog row is selected and placement is armed. |
| Wire or route will not finish | Switch to select, then reactivate the intended wire or route tool. |
| Airwire is missing | Review [Net editing](../schematic-editing/net-editing.md) before routing. |
| Trace seems gone | Review [Layers](../editing/layers.md) before deleting copper. |

If the project state itself looks wrong, save a copy with `SaveAsProjectCommand` before continuing experiments.
