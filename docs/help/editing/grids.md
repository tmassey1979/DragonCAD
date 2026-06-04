# Grids

Grids make placement and routing easier to inspect. Current editor commands expose view fitting and grid visibility behavior while deeper grid-size and snap controls remain tracked in the command catalog.

## Grid habits

- Fit the active editor with `FitActiveViewCommand` before reviewing a full schematic or board.
- Center the active view with `CenterActiveViewCommand` after large placement moves.
- Use `ToggleGridVisibilityCommand` when the grid helps align parts or traces.
- Treat `SetGridSizeCommand` and `ToggleGridSnapCommand` as planned command references until those controls are implemented.

| View task | Command |
| --- | --- |
| Fit editor content | `FitActiveViewCommand` |
| Center editor content | `CenterActiveViewCommand` |
| Show or hide grid | `ToggleGridVisibilityCommand` |

For board editing, combine grid checks with [PCB routing](routing.md) and [Layers](layers.md).
