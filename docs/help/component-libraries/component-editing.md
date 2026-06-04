# Component Editing

Use component editing to create or adjust component drafts before they are placed into a schematic or promoted for reuse. Keep edits local until pins, symbols, footprints, and package mapping are coherent.

## Draft workflow

- Open the component editor with `ShowComponentEditorTabCommand`.
- Start a draft with `NewComponentEditorCommand`.
- Open a selected catalog component with `OpenSelectedComponentEditorCommand`.
- Add starter symbol and footprint geometry with `AddComponentEditorStarterGeometryCommand`.
- Treat `ValidateComponentDraftCommand` as the planned command for full draft validation.

| Draft area | Review before reuse |
| --- | --- |
| Pins | Names, directions, and count match the component datasheet. |
| Symbol | Pin layout is readable in schematic context. |
| Footprint | Pads and package geometry match placement expectations. |
| Mapping | Symbol pins map to footprint pads without gaps. |

After editing, review [Schematic placement](../schematic-editing/placing-symbols.md) and [Marketplace-safe placement](../marketplace/safe-placement.md) before placing the part.
