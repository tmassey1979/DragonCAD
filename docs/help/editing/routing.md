# PCB Routing

PCB routing turns schematic airwires into copper traces on the active board layer. The router is local-first and edits the in-memory board model rather than opening external tools.

## Route a trace

- Open PCB layout with `ShowPcbLayoutTabCommand`.
- Choose the select tool with `ActivateBoardSelectToolCommand` when inspecting existing board objects.
- Choose route mode with `ActivateBoardRouteToolCommand`.
- Click pads or board points to add route vertices.
- Finish the route with `FinishBoardRouteCommand`.
- Delete an incorrect board selection with `DeleteBoardSelectionCommand`, or use `RipupSelectedTraceCommand` once that planned command is implemented.

| Routing object | What to inspect |
| --- | --- |
| Airwire | Confirms schematic connectivity that still needs copper. |
| Trace | Shows routed layer, width, and vertices. |
| Via | Changes layers while preserving electrical continuity. |

Use [Vias](vias.md), [Layers](layers.md), and [Grids](grids.md) together when a route needs layer changes or tighter alignment.
