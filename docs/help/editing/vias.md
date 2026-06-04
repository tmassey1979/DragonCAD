# Vias

Vias connect routed copper between board layers. Use them when a top-layer route needs to continue on the bottom layer or when a selected trace segment should be split cleanly.

## Via commands

- Start routing with `ActivateBoardRouteToolCommand`.
- Place a free via with `PlaceBoardViaCommand`.
- Select an existing trace segment with `ActivateBoardSelectToolCommand`.
- Insert a via into that selected segment with `InsertBoardViaIntoSelectedTraceSegmentCommand`.
- Move the selected trace to the active layer with `MoveSelectedBoardTraceToLayerCommand` when the copper layer is wrong.

| Via use | Result |
| --- | --- |
| During routing | Adds a layer transition and continues the route on the next layer. |
| Into selected trace | Splits the trace at the insertion point and preserves the route path. |
| Layer correction | Helps keep top and bottom copper intentional before fabrication review. |

Review [Fabrication outputs](../fabrication/outputs.md) before treating a via-heavy board as ready to manufacture.
