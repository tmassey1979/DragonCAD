# Layers

Layer controls determine which copper layer receives new traces and which board objects are visible while editing. Keep layer choices deliberate so board review, fabrication outputs, and troubleshooting stay understandable.

## Layer workflow

- Open PCB layout with `ShowPcbLayoutTabCommand`.
- Select the active copper layer before using `ActivateBoardRouteToolCommand`.
- Toggle selected layer visibility with `ToggleSelectedBoardLayerVisibilityCommand`.
- Move selected routed copper to the active layer with `MoveSelectedBoardTraceToLayerCommand`.
- Treat `SetActiveBoardLayerCommand` and `OpenLayerSettingsCommand` as command catalog references for deeper layer work.

| Layer task | Command |
| --- | --- |
| Route on active layer | `ActivateBoardRouteToolCommand` |
| Move selected trace | `MoveSelectedBoardTraceToLayerCommand` |
| Toggle visibility | `ToggleSelectedBoardLayerVisibilityCommand` |

If a route disappears unexpectedly, check layer visibility before deleting or redrawing copper.
