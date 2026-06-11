# DragonCAD Editor Interaction Backlog

This backlog captures the missing UI-facing editor tools needed to move DragonCAD from a viewer/basic placer into an EAGLE-like interactive CAD workspace. Stories are intentionally vertical, bounded, and suitable for parallel agents.

## Multi-Agent Constraints

- Stories must keep changes inside their listed owned paths.
- App/editor stories may call existing core models and commands, but must not redesign core file formats.
- Rendering stories may improve editor canvas drawing and hit testing, but must not mutate project state directly.
- Component stories may use the current library model and imported HawkCAD/EAGLE assets, but must not change importer behavior unless the story explicitly owns it.
- Marketplace-grade catalog, BOM, vendor, datasheet, and manufacturing handoff work lives in `docs/marketplace-library-epic.md`; editor stories should consume those contracts instead of inventing local catalog models.
- Every implementation story should include tests for the view model or rendering mapper before UI wiring is considered complete.

---

## Current Status Snapshot

Status is based on the current app and test surface, not on long-term product intent. "Partial" means meaningful backend or UI behavior exists, but at least one acceptance criterion still needs a focused implementation pass.

| Story | Status | Current evidence | Remaining gap |
| --- | --- | --- | --- |
| EDIT-001 - Schematic Pin-To-Wire Tool | Implemented | Schematic wire tool starts from component pins, snaps to grid, builds orthogonal segments, previews pending routes, and syncs completed nets into board airwires. | Continue tuning pin hit tolerance through manual usability passes. |
| EDIT-002 - Schematic Wire Editing Handles | Partial | Wire segment selection, segment movement, whole-wire delete, and selected-segment delete are implemented and covered by schematic/shell tests. | Vertex handles and explicit visual edit handles are still missing. |
| EDIT-003 - Component Placement Repeat Flow | Partial | Add-part placement can arm a selected library component and place it on the schematic with deterministic references. | Full chooser repeat workflow and Escape/cancel UX still need polish. |
| EDIT-004 - Component Properties Editing | Partial | Selected schematic reference, name, value, rotation, mirror state, and board metadata sync are implemented. | Attribute editing and package-change-to-footprint replacement are not complete. |
| EDIT-005 - Symbol Geometry Fidelity | Partial | Symbols now render library geometry, pin labels, pin lines, and connection points from component previews. | Broader primitive coverage, text fidelity, and fixture coverage for all target symbol classes remain. |
| EDIT-006 - PCB Footprint Geometry Fidelity | Partial | Board components render footprint preview pads/lines with layer colors and support selection/move/rotate/mirror. | Full pad/SMD/hole/keepout/text/arc fidelity and geometry-based selection coverage remain. |
| EDIT-007 - PCB Routing Interaction | Partial | Manual board route tool creates orthogonal traces, supports active layer assignment, via insertion/layer switching, trace movement, trace deletion, and trace layer reassignment. | Pad-start/pad-finish semantics, airwire retirement, and 45-degree mode remain. |
| EDIT-008 - Layer Palette and Visibility Controls | Partial | Board active-layer selector, visibility toggles, layer-colored traces, and visible-trace filtering exist. | Schematic/component-editor layer controls and checkbox dropdown polish remain. |
| EDIT-009 - Grid Controls and Snap Modes | Partial | Schematic and board grid visibility/style/spacing commands exist; placement, wire, via, trace, and movement flows snap to grid. | Per-editor persistence, unit selector polish, and component editor grid support remain. |
| EDIT-010 - Component Editor Creation Tools | Remaining | Component manager exposes package options and preview metadata. | Dedicated component editor creation tools, save/reload, and pin/pad mapping UI are still open. |
| EDIT-011 - Tool Icon Rail and Context Toolbars | Partial | Schematic and board toolbars/rails expose placement, wire, route, via, rotate, mirror, delete, grid, and layer commands. | Icon-only polish, cursor coverage, and component-editor tool rail remain. |
| EDIT-012 - Cross-Editor Schematic-To-Board Sync | Partial | Schematic placement creates board components; schematic net/wire operations create/update board airwires; metadata changes sync to board. | Delete policy, package replacement, save/reload identity, and richer net-to-pad mapping remain. |
| EDIT-013 - Help And Command Documentation Viewer | Remaining | No completed markdown help viewer was found in the app surface. | Build local markdown-rendered help panel and editor tool docs. |
| EDIT-014 - Editor Sample Projects | Partial | 7805 sample loading is covered by shell tests and creates wired schematic/board footprints. | Project-center/file-menu loading and richer persisted sample assets remain. |

---

## Remaining Editor Usability Roadmap

These stories describe the remaining user-facing flow from placing a schematic part through board layout, library sourcing, and fabrication ordering. They are documentation/status guidance only; implementation agents should still work from the detailed story sections and marketplace epic before editing app source.

### Marketplace And Library Placement Gates

Editor stories can expose marketplace and library status, but placement behavior depends on trusted-library maturity. Use these gates when sequencing editor work with `docs/marketplace-library-epic.md`.

| Editor dependency | Marketplace/library source | Placement rule | Editor backlog touchpoint |
| --- | --- | --- | --- |
| Trusted component search and repeat placement | `MKT-003`, `MKT-004`, `MKT-008`, `MKT-009`, `MKT-016`, `MKT-023` | Only verified placeable components can arm the schematic cursor. Vendor rows, imported candidates, and datasheet drafts must show diagnostics until reviewed and promoted. | `USE-001`, `EDIT-003`, `EDIT-010` |
| Package and footprint selection | `MKT-003`, `MKT-004`, `MKT-008`, `MKT-026` | Package changes must preserve provenance and cannot replace verified geometry silently. Placement waits for a trusted package-to-footprint mapping. | `USE-001`, `USE-003`, `EDIT-004`, `EDIT-006`, `EDIT-012` |
| Datasheet-generated proposals | `MKT-007`, `MKT-020`, `MKT-023`, `MKT-030` | Generated symbols, footprints, and 3D proposals remain review-required. Editor previews may display them, but schematic/board placement requires promotion. | `USE-004`, `EDIT-010`, `EDIT-013D` |
| BOM and vendor sourcing context | `MKT-010`, `MKT-018`, `MKT-022`, `MKT-028`, `MKT-029` | BOM/cart plans are derived from placed components and selected alternates. The editor must not invent editor-only catalog records to satisfy sourcing gaps. | `USE-004`, `USE-005`, `Marketplace Wave 8`, `Marketplace Wave 9` |
| Fabrication handoff readiness | `MKT-012`, `MKT-013`, `MKT-019`, `MKT-024`, `MKT-031` | Board routing and package selection provide artifacts for handoff checks; handoff panels remain review/export flows unless live provider stories are added. | `USE-003`, `USE-005`, `EDIT-007`, `EDIT-012` |

These gates intentionally avoid claiming live vendor ordering or API-backed placement. Live provider sync, carts, checkout, manufacturing upload, payment, shipping, and automatic promotion require explicit marketplace stories before editor work may depend on them.

### Editor-To-Marketplace Workflow Cross-Links

Use this table when an editor story needs marketplace/library state. The editor backlog owns workflow behavior; `docs/marketplace-library-epic.md` owns catalog, sourcing, promotion, cart, and provider capability rules.

| Editor workflow | Marketplace/library dependency | Required editor behavior |
| --- | --- | --- |
| Placing components | Trusted placeable components from `MKT-009`, future persisted promotions from `MKT-061`, and provenance/audit state from `MKT-026`/`MKT-032`. | Add-part and repeat placement may preview catalog-only rows, but only trusted placeable records may arm the cursor or create schematic/board components. |
| Syncing schematic to board | Package/footprint mappings from trusted library promotion and canonical identity work. | Board sync must preserve source identity, selected package, reference, value, and footprint mapping; package changes need diagnostics before replacing board geometry. |
| BOM sourcing from the design | BOM/order planning from `MKT-010`, `MKT-018`, `MKT-022`, `MKT-028`, and project-derived follow-up `MKT-062`. | BOM rows must be derived from placed schematic/board components and selected alternates; editor-only placeholders must not satisfy sourcing gaps. |
| Vendor refresh while editing | In-use vendor sync and freshness state from `MKT-025`, `MKT-029`, and the MKT-015 baseline. | Refresh commands should be explicit user actions, show stale/fresh state, and avoid surprise network calls while placing or routing. |
| Fabrication handoff from board state | OSH Park/PCBCart readiness from `MKT-012`, `MKT-013`, `MKT-019`, `MKT-024`, `MKT-031`, and future project-derived wiring in `MKT-062`. | Board routing and package choices provide handoff inputs; handoff panels must remain review/export/manual next-step flows unless a provider-specific live story exists. |

### USE-001 - Schematic Part Placement Flow

**As a** schematic designer, **I want** to search, preview, place, and repeat-place verified components from one add-part workflow, **so that** I can build a schematic quickly without confusing catalog-only results with placeable parts.

**AC:**
- Add-part search distinguishes trusted placeable components from vendor catalog matches, imported candidates, and datasheet-generated drafts.
- Selecting a component shows symbol, package, value, provenance, and placement readiness before the cursor is armed.
- After placement, the same component remains armed until Escape, cancel, or a new component selection changes the active part.
- Deterministic reference designators are assigned and visible immediately after placement.
- Placement diagnostics explain why a component cannot be placed, including missing symbol, missing package mapping, unreviewed draft state, or catalog-only status.

**Implementation Dev Notes:**
- Coordinate with `EDIT-003`, `EDIT-004`, `EDIT-005`, `EDIT-010`, and marketplace component-browser stories.
- Use the Marketplace And Library Placement Gates table before adding any placement path that consumes marketplace, imported, or datasheet-derived records.
- Use the Editor-To-Marketplace Workflow Cross-Links table for trusted placement and provenance dependencies.
- Do not let marketplace rows, generated datasheet drafts, or unreviewed imported candidates bypass trusted component review.
- The implementation should preserve schematic-to-board identity from the first placement command so later board synchronization does not depend on display text.

### USE-002 - Schematic Wire Routing And Net Editing

**As a** schematic designer, **I want** wires to start and finish on real pins, route predictably on the grid, and expose editable net labels and handles, **so that** connection intent is clear before I move to the board.

**AC:**
- Pin hover, pin click, grid snapping, pending wire preview, and completed net feedback are visible during routing.
- Wires can be selected, moved by segment or vertex, deleted, and relabeled without breaking unrelated nets.
- Net labels remain associated with the intended net after wire movement, segment edits, and component movement.
- Routing diagnostics distinguish incomplete wires, incompatible targets, duplicate labels, and unresolved pins.
- Completed schematic nets provide stable input to board airwire generation.

**Implementation Dev Notes:**
- Coordinate with `EDIT-001`, `EDIT-002`, Wave 4 net labels, and Wave 5 net label rendering.
- Keep wire routing behavior inside schematic editor view-model commands and mapper tests before shell polish.
- Do not change board routing behavior in this story; board updates should flow through explicit synchronization commands.

### USE-003 - Board Synchronization And Routing Completion

**As a** hardware designer, **I want** placed schematic parts and named nets to produce matching board components, airwires, and routable pads, **so that** schematic capture and PCB layout stay synchronized while I route the board.

**AC:**
- Schematic placement creates or updates the matching board component with stable identity, selected package, reference, value, rotation, and mirror state.
- Schematic net changes create, update, or retire board airwires without duplicating connections.
- Board routing can start from pads, route with 90-degree and 45-degree modes, insert vias, switch copper layers, and finish on compatible pads.
- Completing a route updates the related airwire state without deleting unrelated schematic intent.
- Package changes and component deletes have explicit synchronization diagnostics before board state changes are applied.

**Implementation Dev Notes:**
- Coordinate with `EDIT-006`, `EDIT-007`, `EDIT-012`, Wave 3 via insertion, Wave 4 trace width editing, and Wave 5 via size editing.
- Coordinate package/footprint identity with trusted-library promotion and project-derived BOM/handoff follow-ups in `docs/marketplace-library-epic.md`.
- Synchronization should remain explicit and testable; avoid hidden canvas side effects.
- Do not implement autorouting or full ERC/DRC as part of this usability pass.

### USE-004 - Component Library And Marketplace Review Path

**As a** DragonCAD user, **I want** one library and marketplace path for trusted parts, vendor matches, datasheet drafts, and provenance, **so that** the editor can place verified parts while sourcing and review workflows remain visible.

**AC:**
- Component search exposes trusted components, vendor matches, imported candidates, and datasheet-generated drafts with clear status badges.
- Datasheet-generated symbols, footprints, packages, and 3D proposals stay in a review queue until promoted.
- Canonical merge suggestions and provenance history are visible before duplicate or generated components are trusted.
- BOM planning uses placed schematic/board components and does not invent editor-only catalog records.
- Marketplace panels clearly distinguish offline request plans, local review artifacts, and live provider actions that are not yet implemented.

**Implementation Dev Notes:**
- Coordinate with `docs/marketplace-library-epic.md`, `docs/component-marketplace-roadmap.md`, `MKT-016` through `MKT-033`, and `EDIT-010`.
- Treat `MKT-015` as the current documentation/status source for marketplace sequencing and six-agent boundaries.
- Treat future `MKT-061` and `MKT-062` as the handoff points for durable trusted-library promotion and active-design BOM sourcing.
- Do not write generated parts directly into the permanent library without an explicit reviewed promotion story.
- Treat marketplace integration as a review-first workflow; source, merge, and order state should remain explainable from component provenance.

### USE-005 - Fabrication And Ordering Handoff

**As a** product builder, **I want** fabrication packages, BOM carts, and vendor handoff actions reviewed before leaving DragonCAD, **so that** prototype and production ordering can proceed without accidental checkout or incomplete manufacturing data.

**AC:**
- Fabrication readiness shows Gerber, drill, board outline, paste, BOM, pick-and-place, assembly, and manifest status.
- OSH Park prototype and PCBCart production handoffs list required artifacts, warnings, accepted blockers, hashes, and manual next actions.
- BOM cart rows are generated from the reviewed BOM order plan with quantities, alternates, stale-price warnings, and vendor grouping.
- Exported purchasing and fabrication artifacts are deterministic and reviewable before any external upload or order action.
- Live checkout, manufacturing upload, payment, shipping, and provider confirmation remain disabled unless a provider-specific story explicitly implements them.

**Implementation Dev Notes:**
- Coordinate with fabrication stories, Wave 3 BOM CSV, Wave 4 pick/place CSV, Wave 5 Gerber manifest summary, and marketplace cart/fabrication readiness stories.
- Use `MKT-062` for the future bridge from active schematic/board state into BOM cost rollups, cart drafts, and OSH Park/PCBCart readiness.
- Ordering UX must preserve the current review-only posture: local order records and handoff artifacts are not live vendor orders.
- Keep prototype and production handoff decisions separate because OSH Park and PCBCart require different manufacturing evidence.

---

## EDIT-001 - Schematic Pin-To-Wire Tool

**As a** schematic designer, **I want** to start wires by clicking component pins, **so that** I can connect real symbols without hunting for exact pin endpoints.

**AC:**
- Clicking within an expanded pin hit area starts a schematic wire at the snapped pin connection point.
- Clicking additional grid points adds 90-degree wire segments.
- Clicking another compatible pin completes the connection and assigns a visible net name.
- Wires render immediately while drawing, including a pending preview segment.
- Tests cover pin hit tolerance, grid snapping, 90-degree segment creation, and completed pin-to-pin net creation.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/SchematicEditor/**`, `tests/DragonCAD.App.Tests/SchematicEditor/**`.
- Use existing `SchematicEditorViewModel` wire tool state instead of adding canvas-only state.
- Do not change component library importers or board synchronization in this story.
- Validate with `dotnet test DragonCAD.slnx --no-build --filter SchematicEditor`.

**Agent Boundary:** Schematic editor interaction only.

---

## EDIT-002 - Schematic Wire Editing Handles

**As a** schematic designer, **I want** to select and move wire segments and vertices, **so that** I can clean up schematic routing after placement.

**AC:**
- Clicking a wire segment selects that segment with a visible highlight.
- Dragging a horizontal segment moves it vertically on-grid.
- Dragging a vertical segment moves it horizontally on-grid.
- Dragging a vertex moves connected segments while preserving orthogonal routing where possible.
- Delete removes the selected segment or whole wire according to current selection state.
- Tests cover segment selection, vertex selection, move behavior, and delete behavior.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/SchematicEditor/**`, `tests/DragonCAD.App.Tests/SchematicEditor/**`.
- Reuse the board trace segment behavior where practical, but do not move shared code unless a dedicated refactor story is created.
- Do not touch PCB editor files.
- Validate with schematic editor tests and a manual app check.

**Agent Boundary:** Schematic wire manipulation only.

---

## EDIT-003 - Component Placement Repeat Flow

**As an** EAGLE-style user, **I want** the add-part tool to keep placing the selected part until I cancel or choose another part, **so that** repeated component placement is fast.

**AC:**
- Opening the add-part dialog selects a component/device/package combination.
- After placing one instance, the same part remains armed for another placement.
- Escape cancels placement mode.
- Clicking the add-part toolbar button again reopens the chooser.
- Reference designators auto-increment deterministically.
- Tests cover repeated placement, cancel behavior, and reference numbering.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/ComponentBrowser/**`, `src/DragonCAD.App/SchematicEditor/**`, `tests/DragonCAD.App.Tests/ComponentBrowser/**`, `tests/DragonCAD.App.Tests/SchematicEditor/**`.
- Do not modify library file format.
- Keep selected-part state in view models, not the canvas control.
- Validate with app tests and manual schematic placement.

**Agent Boundary:** Component selection and schematic placement workflow.

---

## EDIT-004 - Component Properties Editing

**As a** schematic designer, **I want** to edit placed component reference, value, attributes, and package choice, **so that** schematic and board metadata stay accurate.

**AC:**
- Selecting a schematic component shows editable reference, value, name, package, and attributes in the properties panel.
- Reference changes are validated for uniqueness.
- Value and attribute changes update visible schematic text.
- Package changes update the synchronized PCB footprint when a matching footprint exists.
- Tests cover valid edits, duplicate reference rejection, attribute persistence, and board sync update.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/SchematicEditor/**`, `src/DragonCAD.App/ComponentEditor/**`, `tests/DragonCAD.App.Tests/SchematicEditor/**`.
- Board sync may be called through existing shell/view-model synchronization methods only.
- Do not edit Eagle importers or core serialization in this story.
- Validate with `dotnet test DragonCAD.slnx --no-build --filter Component`.

**Agent Boundary:** Placed component property editing.

---

## EDIT-005 - Symbol Geometry Fidelity

**As a** schematic designer, **I want** symbols to render their real lines, arcs, text, pins, and connection points, **so that** imported and native components look like CAD symbols instead of boxes.

**AC:**
- Symbol renderer draws body geometry from the native component library model.
- Pins render with line stubs, names, numbers, orientation, and connection points.
- Text primitives avoid junk placeholder text and honor rotation and visibility flags.
- Hit testing uses rendered pin connection points, not bounding-box guesses.
- Tests cover geometry mapping for at least one resistor, capacitor, regulator, connector, and IC symbol.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/SchematicEditor/**`, `tests/DragonCAD.App.Tests/SchematicEditor/**`, `docs/assets/**` if screenshots are added.
- Do not alter importer parsing; consume the current native library output.
- Keep rendering conversion separate from placement state.
- Validate with app tests and a generated schematic screenshot.

**Agent Boundary:** Schematic symbol rendering and hit testing.

---

## EDIT-006 - PCB Footprint Geometry Fidelity

**As a** PCB designer, **I want** footprints to render pads, SMDs, silkscreen, outlines, holes, and keepouts on correct layers, **so that** the board view matches the component package.

**AC:**
- Footprint renderer draws layer-colored primitives from the selected package/footprint.
- Through-hole pads include copper ring and drill hole.
- SMD pads render on the correct copper layer.
- Silkscreen and documentation outlines are separately visible by layer.
- Component selection uses footprint geometry, not only a generic rectangle.
- Tests cover TO-220, DIP/SOIC, resistor/capacitor, connector, and module footprints.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/BoardEditor/**`, `tests/DragonCAD.App.Tests/BoardEditor/**`.
- Do not modify schematic editor or library importers.
- Use existing board layer visibility state.
- Validate with board editor tests and manual visual check.

**Agent Boundary:** PCB footprint rendering and selection.

---

## EDIT-007 - PCB Routing Interaction

**As a** PCB designer, **I want** to route traces from pads with layer-aware snapping, vias, and 45/90-degree modes, **so that** I can create real board copper interactively.

**AC:**
- Clicking a pad starts a route on the active layer.
- Route preview snaps to grid and supports 90-degree and 45-degree corner modes.
- Via placement switches active copper layer and continues the route.
- Completing on another pad creates a routed connection and removes or updates the related airwire.
- Tests cover pad start, route segment creation, via insertion, layer switch, and airwire update.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/BoardEditor/**`, `tests/DragonCAD.App.Tests/BoardEditor/**`.
- Do not implement autorouting in this story.
- Respect layer visibility and active layer state.
- Validate with board editor tests and manual route placement.

**Agent Boundary:** Manual PCB route tool only.

---

## EDIT-008 - Layer Palette and Visibility Controls

**As a** PCB or schematic editor user, **I want** a layer dropdown with current-layer selection and visibility checkboxes, **so that** I can control what I edit and what I see.

**AC:**
- Each editor shows a current layer selector.
- The selector includes checkbox visibility toggles for all available layers.
- Hidden layers do not render and do not participate in normal hit testing.
- Active layer remains selectable even if other layers are hidden.
- Stable contrasting default colors are used for copper, silkscreen, dimension, grid, nets, and selected objects.
- Tests cover visibility toggles, active layer changes, and filtered hit testing.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `src/DragonCAD.App/BoardEditor/**`, `src/DragonCAD.App/SchematicEditor/**`, `src/DragonCAD.App/ComponentEditor/**`, matching app tests.
- Coordinate carefully if another agent owns one of those editor folders; otherwise split this story per editor.
- Do not change core layer contracts.
- Validate with app tests and visual smoke check.

**Agent Boundary:** Shared editor layer UI. Do not run in parallel with editor-specific layer work.

---

## EDIT-009 - Grid Controls and Snap Modes

**As an** EAGLE-style user, **I want** dot/line grid controls, spacing, units, and snap behavior per editor, **so that** placement and routing are precise and predictable.

**AC:**
- Editors support dot grid, line grid, hidden grid, and spacing selection.
- Grid spacing is displayed in engineering units.
- Placement, movement, wire routing, trace routing, and via placement snap to the selected grid.
- Fine-grid and coarse-grid shortcuts are available.
- Tests cover grid spacing conversion, snap behavior, and per-editor grid persistence.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `src/DragonCAD.App/SchematicEditor/**`, `src/DragonCAD.App/BoardEditor/**`, `src/DragonCAD.App/ComponentEditor/**`, matching app tests.
- Do not touch integer geometry kernel unless a missing conversion bug is proven.
- Keep UI state deterministic and serializable where current layout settings already persist.
- Validate with app tests and manual zoom/pan checks.

**Agent Boundary:** Shared editor grid behavior. Coordinate before parallel work.

---

## EDIT-010 - Component Editor Creation Tools

**As a** library author, **I want** tools to draw symbols, pins, pads, outlines, and package mappings, **so that** I can create or fix components inside DragonCAD.

**AC:**
- Component editor opens from the component manager with a new-component command and an edit-existing command.
- Component editor has explicit tool modes for symbol line, symbol arc, symbol pin, symbol text, footprint through-hole pad, SMD pad, silkscreen/outline, package keepout, and pin-to-pad mapping.
- User can create a component with at least one symbol, one package footprint, and one device/package mapping without importing an external library file.
- Properties panel edits component name, value/default prefix, package name, pin names/numbers, pad numbers, pad shape/size, layer, rotation, and custom attributes.
- Pin-to-pad mapping UI shows unmapped pins, unmapped pads, duplicate mappings, and mapping completeness before save.
- Save persists the created component in the native library format and reloads it as a trusted placeable component.
- Tests cover new component creation, drawing tool state, pin/pad mapping, validation errors, property edits, and save/reload.

### Mini-Stories

#### EDIT-010A - New Component Workspace

**As a** library author, **I want** a dedicated component editor workspace with symbol, footprint, and mapping views, **so that** I can build a complete placeable component without leaving DragonCAD.

**AC:**
- New-component command opens a component editor tab with empty symbol, footprint, and mapping sections.
- Edit-existing command opens a copy of a trusted library component and shows its symbol, footprint, package options, and device mapping.
- Unsaved changes are visible in the tab title or editor state and block accidental close with a deterministic prompt.
- Component validation reports missing symbol, missing footprint, missing package name, and incomplete mapping before save.
- Tests cover new/edit command creation, dirty state, validation summary, and close behavior.

**Implementation Dev Notes:**
- Start with view-model state and commands before adding canvas tools.
- Reuse the current component manager/library service contracts where possible.
- Keep editor state serializable so save/reload tests can compare persisted output.
- Do not add importer dependencies or marketplace review behavior in this slice.

**Agent Boundary:** Component editor workspace/view-model only. Do not change schematic placement, board placement, importers, or marketplace promotion flows.

#### EDIT-010B - Symbol Drawing Tools

**As a** component librarian, **I want** symbol drawing tools for pins, lines, arcs, and text, **so that** schematic symbols can be created with correct electrical connection points and readable labels.

**AC:**
- Tool rail exposes symbol select, line, arc, pin, and text modes with active-tool feedback.
- Pin tool captures pin number, name, direction/type, visible label setting, and connection endpoint.
- Line, arc, and text tools snap to the component editor grid and support move/delete after creation.
- Properties panel updates the selected symbol primitive without recreating the whole symbol.
- Tests cover tool activation, primitive creation, pin endpoint geometry, property updates, and save/reload of symbol primitives.

**Implementation Dev Notes:**
- Keep symbol primitives in the native library model shape consumed by schematic rendering.
- Prefer deterministic integer/grid geometry over free-form canvas coordinates.
- Use existing editor hit-testing patterns where practical, but keep component-editor-specific behavior in component editor files.
- Do not broaden schematic symbol rendering except where an existing renderer cannot display a newly persisted primitive.

**Agent Boundary:** Component editor symbol tools only. Coordinate with symbol fidelity work before touching shared symbol rendering.

#### EDIT-010C - Footprint Drawing Tools

**As a** package author, **I want** footprint tools for through-hole pads, SMD pads, outlines, silkscreen, holes, and keepouts, **so that** PCB packages can be authored with manufacturable geometry.

**AC:**
- Tool rail exposes footprint select, through-hole pad, SMD pad, outline/line, text, hole, and keepout modes.
- Pad tools capture pad number, shape, drill where applicable, copper size, layer, and rotation.
- Footprint primitives snap to the component editor grid and can be moved, edited, and deleted.
- Validation flags duplicate pad numbers, missing pad numbers, invalid drill/size combinations, and footprints with no pads.
- Tests cover pad creation, SMD creation, outline creation, validation errors, property edits, and save/reload of footprint primitives.

**Implementation Dev Notes:**
- Preserve compatibility with board footprint preview and placement consumers.
- Keep manufacturing-rule validation limited to obvious component-authoring errors; full DRC belongs elsewhere.
- Avoid changing board routing semantics while adding footprint authoring.
- Manual validation should include creating a simple resistor footprint and reopening it.

**Agent Boundary:** Component editor footprint tools only. Do not change board routing, fabrication package generation, or vendor/manufacturer handoff logic.

#### EDIT-010D - Pin-To-Pad Mapping And Promotion

**As a** library maintainer, **I want** a clear pin-to-pad mapping editor and trusted save path, **so that** authored components can be placed in schematics and synchronized to boards safely.

**AC:**
- Mapping view lists symbol pins and footprint pads side by side with current pairings.
- User can create, update, and remove mappings without losing symbol or footprint geometry.
- Mapping validation flags unmapped required pins, duplicate pad assignments, missing package selection, and pin/pad number mismatches.
- Successful save registers the component as trusted/placeable through the same library path used by add-part placement.
- Tests cover mapping commands, validation diagnostics, trusted save registration, and schematic/board library lookup after reload.

**Implementation Dev Notes:**
- Use deterministic mapping commands rather than canvas-only gestures.
- Keep trust/promotion local to manually authored components; do not bypass datasheet/import review queues.
- Coordinate with cross-editor sync only through existing library lookup contracts.
- Do not implement marketplace provenance or datasheet-generated promotion decisions in this story.

**Agent Boundary:** Component editor mapping and native trusted save path only. Do not alter datasheet review, imported component promotion, or marketplace provenance workflows.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/ComponentEditor/**`, `tests/DragonCAD.App.Tests/ComponentEditor/**`.
- Use the native library model; do not modify importers.
- Do not change schematic or board placement except through existing library consumption.
- Validate with component editor tests and manual create/save/open.

**Agent Boundary:** Component editor only.

---

## EDIT-011 - Tool Icon Rail and Context Toolbars

**As an** editor user, **I want** icon-based left tool rails and context-sensitive top toolbars, **so that** the app feels like a real CAD workstation instead of a generic form.

**AC:**
- Schematic, board, and component tabs each show editor-specific icon tools.
- Active tool is visually highlighted.
- Tooltips describe each icon.
- Cursor changes match active tool and hover target.
- Toolbar commands call existing view-model commands, not canvas-only code.
- Tests cover command binding for key toolbar actions.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `src/DragonCAD.App/SchematicEditor/**`, `src/DragonCAD.App/BoardEditor/**`, `src/DragonCAD.App/ComponentEditor/**`, matching app tests.
- Use existing icon assets or simple Avalonia vector icons; do not add large binary assets.
- Do not alter editor models.
- Validate with app tests and manual interaction pass.

**Agent Boundary:** UI shell/editor command surfaces.

---

## EDIT-012 - Cross-Editor Schematic-To-Board Sync

**As a** hardware designer, **I want** placed schematic parts and nets to create matching board components and airwires, **so that** schematic capture and PCB layout stay connected.

**AC:**
- Placing a schematic component creates or updates the matching board component.
- Changing reference/value/package updates the board component metadata.
- Creating a schematic wire/net creates or updates board airwires between matching pads.
- Deleting a schematic component removes or marks the board counterpart according to current project policy.
- Tests cover placement sync, metadata sync, net sync, delete behavior, and save/reload identity.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Synchronization/**` if created, `src/DragonCAD.App/SchematicEditor/**`, `src/DragonCAD.App/BoardEditor/**`, matching app tests.
- Do not implement full ERC/DRC here.
- Keep synchronization explicit and testable; avoid hidden canvas side effects.
- Validate with app tests and a manual schematic-to-board sample.

**Agent Boundary:** App-level editor synchronization. Do not run alongside broad schematic or board model rewrites.

---

## EDIT-013 - Help And Command Documentation Viewer

**As a** DragonCAD user, **I want** built-in markdown help for editor tools and commands, **so that** I can learn the available CAD workflows inside the app.

**AC:**
- Help opens as a docked document tab or side panel without launching an external browser.
- Markdown renders as formatted content, not raw markdown text, and supports headings, lists, tables, inline code, links, and images where local assets exist.
- Help index lists editor workflows, commands, tools, and troubleshooting topics.
- Tool help pages exist for schematic placement, wiring, net editing, PCB routing, vias, layers, grids, component editing, marketplace-safe component placement, and sample projects.
- The active tool can open its relevant help page from the toolbar, context menu, or keyboard shortcut.
- Missing or malformed help topics show a friendly fallback with the requested topic ID and do not crash the app.
- Tests cover help topic lookup, active-tool topic resolution, markdown loading, local asset resolution, and fallback behavior.

### Mini-Stories

#### EDIT-013A - Local Help Topic Registry

**As a** DragonCAD user, **I want** a searchable local help index, **so that** I can find editor workflow guidance without leaving the app.

**AC:**
- Help registry maps stable topic IDs to local markdown files, display titles, editor area, and related tool/command IDs.
- Help index groups topics by schematic, board, component editor, library/marketplace, project samples, and troubleshooting.
- Search matches topic title, summary, tags, and command/tool IDs.
- Missing topic IDs resolve to a deterministic fallback topic instead of throwing.
- Tests cover registry loading, grouping, search, topic lookup, and missing-topic fallback.

**Implementation Dev Notes:**
- Keep the registry local-first and deterministic; no remote docs calls.
- Topic IDs should be stable enough for toolbar/context commands to reference.
- Store markdown docs under `docs/help/**` and load/copy them through the app help surface as the project standardizes packaging.
- Avoid coupling the registry to current visual layout so it can serve panel, tab, or command palette entry points.

**Agent Boundary:** Help topic registry and local markdown docs only. Do not wire editor toolbar commands in parallel with command-surface work unless coordinated.

#### EDIT-013B - Markdown Rendering Surface

**As a** DragonCAD user, **I want** markdown help to render as readable in-app content, **so that** command documentation feels like part of the CAD workspace instead of a text file dump.

**AC:**
- Help viewer renders headings, paragraphs, ordered/unordered lists, tables, inline code, fenced code, links, and local images.
- Viewer supports scroll position, topic title, related topics, and reload after changing topics.
- External links are visibly distinct and require an explicit launch action.
- Local image paths are constrained to packaged/help asset roots.
- Tests cover markdown conversion, link classification, local asset resolution, and malformed markdown fallback.

**Implementation Dev Notes:**
- Prefer a lightweight markdown renderer/control that fits Avalonia and the existing app style.
- Do not host the primary help experience in a browser shell.
- Treat local asset resolution as an app concern so docs can include screenshots later without arbitrary file access.
- Keep rendering tests focused on converted structure and safety decisions, not pixel-perfect typography.

**Agent Boundary:** In-app markdown viewer only. Do not modify unrelated shell navigation, editor commands, or external documentation publishing.

#### EDIT-013C - Active Tool Help Commands

**As an** editor user, **I want** the active CAD tool to open the matching help topic, **so that** I can recover quickly when I forget a placement, routing, grid, layer, or component-editing workflow.

**AC:**
- Active schematic, board, and component editor tools expose a help topic ID through view-model state or command metadata.
- Help command opens the current tool topic when available and falls back to the editor overview topic when no tool is active.
- Toolbar/context help command and keyboard shortcut call the same application command.
- Topic opens in an existing help panel/tab when one is already present instead of creating duplicates.
- Tests cover active-tool resolution, fallback topic selection, command binding, and duplicate-panel prevention.

**Implementation Dev Notes:**
- Keep topic resolution data-driven so new editor tools can add help without changing the help viewer.
- Coordinate with tool icon rail/context toolbar work for command placement.
- Do not change tool behavior to make help routing work; help should observe active command state.
- Manual validation should open help from schematic placement, wire, board route, grid/layer, and component-editor tools.

**Agent Boundary:** Help command wiring and active-tool topic resolution only. Do not implement new editor tools or change canvas interactions.

#### EDIT-013D - Author Initial Editor Help Content

**As a** DragonCAD learner, **I want** concise built-in help pages for the main editor workflows, **so that** I can complete common schematic, board, and component authoring tasks without guessing command behavior.

**AC:**
- Initial markdown topics cover schematic add-part/repeat placement, pin-to-wire routing, net labels, board airwires, PCB trace/via routing, layer visibility, grid/snap modes, component editor creation tools, and sample projects.
- Each topic names prerequisites, primary steps, common mistakes, and related topics.
- Help content distinguishes trusted placeable components from vendor catalog rows, imported candidates, and datasheet-generated drafts.
- Markdown files avoid unsupported claims about live ordering, AI generation, or external vendor checkout.
- Docs can be read directly as markdown and through the in-app help viewer.

**Implementation Dev Notes:**
- Keep topics short and task-focused; deep architecture belongs in backlog or design docs.
- Use relative links between local help topics where possible.
- Include component editor help after `EDIT-010` tool names stabilize, or mark labels generically enough to survive UI polish.
- Validate by reading markdown directly and through the help viewer once rendering exists.

**Agent Boundary:** Local help markdown content only. Do not alter application code unless paired with help viewer implementation in a coordinated slice.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Help/**`, `docs/help/**`, `tests/DragonCAD.App.Tests/Help/**`.
- Do not use a browser shell for primary UI.
- Keep help content local-first.
- Validate with app tests and manual help menu check.

**Agent Boundary:** Help viewer and local markdown docs.

---

## EDIT-014 - Editor Sample Projects

**As a** developer or tester, **I want** sample schematic and board projects with real components, **so that** editor behavior can be verified without importing external files every time.

**AC:**
- A 7805 regulator sample includes schematic symbols, passives, nets, board footprints, traces, and layer data.
- Sample can be loaded from the project center or file menu.
- Schematic and board views show matching components and connections.
- Tests load the sample and verify expected component, net, footprint, trace, and layer counts.

**Implementation Dev Notes:**
- Owned paths: `samples/**`, `src/DragonCAD.App/ProjectCenter/**`, `tests/DragonCAD.App.Tests/Samples/**`.
- Do not modify importer code.
- Prefer native DragonCAD project/library format for the sample.
- Validate with sample loading tests and manual app load.

**Agent Boundary:** Samples and project-center loading only.

---

## Wave 3 Plan And Status

Wave 3 focuses on closing the next editor-to-output workflow gaps without broad refactors. These slices should remain independently testable and should not overlap owned paths unless shell integration explicitly coordinates the command surface.

| Slice | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| Pin selection | Implemented | Schematic pins can be hit-tested, highlighted, and used as reliable wire start/end anchors with a larger practical click target. Closed by [EDIT-022 #69](https://github.com/tmassey1979/DragonCAD/issues/69). | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Via insertion into trace | Implemented | PCB routing can insert a via into the active or selected trace, split/continue the route, and switch layers deterministically. Closed by [EDIT-023 #70](https://github.com/tmassey1979/DragonCAD/issues/70). | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Package preview switching | Implemented | Component browser/editor can switch between available packages and update the preview/active placement package without mutating catalog data. Closed by [CMP-006 #61](https://github.com/tmassey1979/DragonCAD/issues/61). | `src/DragonCAD.App/ComponentManager/**`, matching component manager tests. |
| BOM CSV | Implemented | Fabrication output can export deterministic BOM rows to CSV using the normalized BOM aggregation model. Closed by [FAB-007 #72](https://github.com/tmassey1979/DragonCAD/issues/72). | `src/DragonCAD.Fabrication/**`, `tests/DragonCAD.Fabrication.Tests/**`. |
| Quote ladder comparison | Implemented | Sourcing can compare normalized vendor price breaks for requested build quantities and sort best options consistently. Closed by [SRC-007 #73](https://github.com/tmassey1979/DragonCAD/issues/73). | `src/DragonCAD.Sourcing/**`, `tests/DragonCAD.Sourcing.Tests/**`. |
| Shell/UI integration | Planned | The app shell exposes the new editor, component, fabrication, and sourcing actions through visible toolbar/panel commands. | `src/DragonCAD.App/Shell/**`, focused app shell tests. |

### Wave 3 Coordination Notes

- Run the shell/UI integration slice after the editor and service slices expose stable view-model/service commands.
- Keep CSV, quote, and component package work headless-testable first; UI should bind to those outputs instead of duplicating logic.
- Pin selection and via insertion should prioritize editor feel: grid snapping, visible hover/selection feedback, and deterministic undo-ready state changes.
- Each slice should update this section from `Planned` to `In progress` or `Implemented` only after its focused tests pass.

---

## Wave 4 Plan And Status

Wave 4 turns the next set of editor and output primitives into user-visible workflows. These slices should remain small enough for parallel agents, but shell integration must wait until the backing commands and models are stable.

| Slice | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| Net labels | Implemented | Schematic wires and named nets can show, edit, and persist visible net labels that are usable as connection intent and board-sync input. Closed by [EDIT-026 #79](https://github.com/tmassey1979/DragonCAD/issues/79). | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Trace width editing | Planned | PCB traces expose editable width properties, render with the selected width, and preserve width through route edits and layer changes. | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Package text filtering | Implemented | Component/package selection can filter by package name, footprint type, value text, and package metadata without losing the active package choice. Closed by [CMP-007 #71](https://github.com/tmassey1979/DragonCAD/issues/71). | `src/DragonCAD.App/ComponentManager/**`, matching component manager tests. |
| Pick/place CSV | Implemented | Fabrication output can generate deterministic pick-and-place CSV rows with reference, package, position, rotation, and board side. Closed by [MFG-002 #18](https://github.com/tmassey1979/DragonCAD/issues/18). | `src/DragonCAD.Fabrication/**`, `tests/DragonCAD.Fabrication.Tests/**`. |
| BOM sourcing cost estimate | Planned | BOM lines can combine sourcing quote ladders into a deterministic build-cost estimate for a requested production quantity. | `src/DragonCAD.Sourcing/**`, `src/DragonCAD.Fabrication/**`, matching tests. |
| Shell/UI integration | Planned | The app shell exposes net label editing, trace width editing, package filtering, pick/place export, and BOM cost estimate actions through visible panels/toolbars. | `src/DragonCAD.App/Shell/**`, focused app shell tests. |

### Wave 4 Coordination Notes

- Build net labels and trace width editing as editor-owned behavior first; shell controls should bind to view-model commands instead of mutating canvas state directly.
- Keep pick/place CSV and BOM cost estimation deterministic and headless-testable before exposing export buttons.
- Package text filtering must preserve current package selection when the selected option remains in the filtered result set.
- Shell/UI integration should be the final Wave 4 slice and should include a screenshot of each changed visible screen after launch.

---

## Wave 5 Plan And Status

Wave 5 promotes recently added model/service behavior into visible editor workflows. UI-facing slices must include a changed-screen screenshot after launch; backend-only slices should state why no screenshot applies.

| Slice | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| Net label rendering | Implemented | Schematic net labels render on the sheet, can be selected/moved with the mouse, and display net names clearly at grid-snapped positions. Closed by [EDIT-015 #59](https://github.com/tmassey1979/DragonCAD/issues/59) and expanded by [EDIT-026 #79](https://github.com/tmassey1979/DragonCAD/issues/79). | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Via size editing | Implemented | Selected PCB vias expose editable diameter and drill size, validate positive dimensions, render at the configured size, and preserve layer transition metadata. Closed by [EDIT-016 #60](https://github.com/tmassey1979/DragonCAD/issues/60). | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Selected package summary | Implemented | Component selection shows the active package, footprint preview, variant metadata, and available package count in a readable summary panel. Closed by [CMP-006 #61](https://github.com/tmassey1979/DragonCAD/issues/61). | `src/DragonCAD.App/ComponentManager/**`, `src/DragonCAD.App/Shell/**`, focused component manager/shell tests. |
| Gerber job manifest summary | Implemented | Fabrication output surfaces a deterministic manufacturing manifest summary for Gerber, drill, paste, BOM, pick/place, and assembly files. Closed by [FAB-006 #62](https://github.com/tmassey1979/DragonCAD/issues/62). | `src/DragonCAD.Fabrication/**`, optional `src/DragonCAD.App/Fabrication/**`, matching tests. |
| Sourcing provider descriptors | Implemented | Vendor integrations can expose local provider descriptors for Digi-Key, Mouser, Jameco, SparkFun, and Adafruit without performing network calls. Closed by [SRC-006 #63](https://github.com/tmassey1979/DragonCAD/issues/63). | `src/DragonCAD.Sourcing/**`, matching sourcing tests. |
| Shell/UI trace-width integration | Planned | The PCB shell/property pane exposes selected trace width editing and applies changes through board view-model commands. | `src/DragonCAD.App/Shell/**`, `src/DragonCAD.App/BoardEditor/**`, focused app tests. |

### Wave 5 Coordination Notes

- Net label rendering depends on the schematic label model being stable; do not change label identity or net naming semantics in the rendering slice.
- Via size editing should stay separate from routing behavior and should not modify trace route geometry.
- Selected package summary should bind to component-manager state instead of duplicating package selection logic in the shell.
- Gerber job manifest summary and sourcing provider descriptors should remain deterministic and offline-first until explicit provider API stories are scheduled.
- Shell/UI trace-width integration should run after trace width model behavior is verified, and it must capture a PCB tab screenshot showing the changed property control.

---

## Marketplace Wave 7 Editor Integration Plan And Status

Marketplace Wave 7 lives primarily in `docs/marketplace-library-epic.md`, but it affects the editor shell because component discovery, datasheet review, BOM planning, and fabrication handoff need visible entry points. Editor-facing work should consume marketplace, sourcing, fabrication, and datasheet-review view models instead of creating duplicate shell-only models.

| Story | Status | Editor-Facing Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-016 - Canonical Merge Service | Planned | Component browser can later show canonical/duplicate status from core merge suggestions. | No editor edits in this story. |
| MKT-017 - Vendor Request Plan And Credential Boundaries | Planned | Marketplace panels can later explain missing credentials and offline request plans without exposing secrets. | No editor edits in this story. |
| MKT-018 - BOM Order Planning Workspace Model | Planned | BOM planner panel can bind to build quantities, vendor choices, cost totals, and review diagnostics. | No editor edits in this story. |
| MKT-019 - Fabrication Handoff UI View Models | Planned | Prototype and production handoff panels can bind to OSH Park and PCBCart package state. | `src/DragonCAD.App/Fabrication/**`, matching app tests. |
| MKT-020 - Datasheet Review Queue UI View Models | Planned | Datasheet-generated symbols, footprints, and 3D proposals can appear in a review queue before promotion. | `src/DragonCAD.App/Marketplace/**`, matching app tests. |
| MKT-021 - Marketplace Shell Integration | Planned | Main shell exposes Marketplace, BOM Planner, Datasheet Review, Prototype Handoff, and Production Handoff tabs or panels. | `src/DragonCAD.App/Shell/**`, focused shell tests. |

### Wave 7 Editor Coordination Notes

- `MKT-021` should be the only Wave 7 story editing shell navigation or main-window tab/panel placement.
- Component editor and schematic placement tools must continue to place only verified placeable components; catalog-only and datasheet-draft records require review first.
- Marketplace UI must clearly distinguish offline catalog/request-plan data from live vendor accounts, live carts, and live ordering.
- Intentionally deferred from editor integration: live credentials, live carts, live order placement, browser checkout automation, and automated component approval.
- Any later editor story that places marketplace components should use the canonical component/review state from the marketplace epic rather than bypassing review.

---

## Marketplace Wave 8 Editor Integration Plan And Status

Marketplace Wave 8 continues in `docs/marketplace-library-epic.md` and focuses on reviewable actions: cart handoff, datasheet promotion planning, fabrication action planning, vendor sync status, component provenance, and shell integration. Editor-facing work must consume marketplace services and view models rather than creating shell-only business rules.

| Story | Status | Editor-Facing Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-022 - Marketplace Cart Workspace Model | Planned | BOM/cart panels can later show vendor-grouped cart rows, stale-price warnings, and disabled handoff states. | No editor edits in this story. |
| MKT-023 - Datasheet Promotion Plan Service | Planned | Component/library UI can later show review-required promotion plans for generated symbols, footprints, and 3D proposals. | No editor edits in this story. |
| MKT-024 - Fabrication Handoff Action Planner | Planned | Prototype and production handoff panels can bind to clear next-step actions without submitting orders. | No editor edits in this story. |
| MKT-025 - Vendor Sync Status Model | Planned | Marketplace panels can show provider freshness, credential state, and stale catalog warnings. | No editor edits in this story. |
| MKT-026 - Component Provenance And Audit Trail | Planned | Component inspector and library editor can later show source, merge, override, and promotion history. | No editor edits in this story. |
| MKT-027 - Marketplace Action Shell Integration | Planned | Main shell exposes cart, sync, promotion, fabrication action, and provenance panels from offline/sample data. | `src/DragonCAD.App/Shell/**`, focused shell tests. |

### Wave 8 Editor Coordination Notes

- `MKT-027` should be the only Wave 8 story editing shell navigation, tabs, or main-window panel placement.
- Schematic, board, and component editors should keep using verified placeable components only; datasheet-generated components require a reviewed promotion plan first.
- BOM/cart and fabrication controls must clearly distinguish reviewable handoff from live checkout or live order placement.
- Vendor sync panels must not imply credentials are configured or live catalog refresh has occurred unless a later live-provider story supplies that state.
- Component provenance should be displayed as audit information, not used as a shortcut to bypass review, dedupe, or placement rules.
- Explicitly deferred from editor integration: live checkout, live order placement, browser checkout automation, payment/shipping storage, and unreviewed AI-generated part promotion.

---

## Marketplace Wave 9 Editor Integration Plan And Status

Marketplace Wave 9 continues in `docs/marketplace-library-epic.md` and focuses on command-ready marketplace actions. Editor-facing work must bind to cart, sync, datasheet promotion, fabrication readiness, and audit view models without duplicating sourcing or fabrication business rules in the shell.

| Story | Status | Editor-Facing Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-028 - Cart Quantity Commands | Planned | BOM/cart panels can expose increment, decrement, set quantity, remove, and clear commands with live diagnostics from offline state. | `src/DragonCAD.App/Marketplace/Cart/**`, matching app tests. |
| MKT-029 - Vendor Sync Run Planner | Planned | Marketplace sync panels can show planned refresh operations, missing credentials, rate-limit warnings, and manual-feed instructions. | No editor edits in this story. |
| MKT-030 - Datasheet Promotion Commands | Planned | Datasheet review panels can show approve, reject, link-existing, and request-more-data command states. | `src/DragonCAD.App/Datasheets/Promotion/**`, matching app tests. |
| MKT-031 - Fabrication Readiness Commands | Planned | Prototype and production handoff panels can expose readiness validation, warning acceptance, manifest export, and open-handoff actions. | `src/DragonCAD.App/Fabrication/Handoff/**`, matching app tests. |
| MKT-032 - Component Audit Timeline UI Model | Planned | Component inspector and library panels can show source, merge, promotion, rejection, and override history as a filterable timeline. | `src/DragonCAD.App/Marketplace/Audit/**`, matching app tests. |
| MKT-033 - Marketplace Command Shell Integration | Planned | Main shell wires cart, sync, promotion, fabrication readiness, and audit commands into visible workspace panels. | `src/DragonCAD.App/Shell/**`, focused shell tests. |

### Wave 9 Editor Coordination Notes

- `MKT-033` should be the only Wave 9 story editing shell navigation, top-level tabs, or main-window panel placement.
- `MKT-029` must stay headless in sourcing; the shell may bind to its results only after the planner output is stable.
- Cart quantity and fabrication readiness commands may update in-memory/sample UI state, but must not submit carts, upload manufacturing files, place orders, or automate checkout.
- Datasheet promotion commands must not write directly into the permanent component library; generated parts remain review-required until a later persistence story explicitly owns mutation.
- Audit timeline UI is read-only and must not be used to bypass review, canonical dedupe, placement validation, or component approval.
- Explicitly deferred from editor integration: live vendor HTTP sync, live credentials, live carts, live checkout, order placement, manufacturing upload automation, credential/payment/shipping storage, automatic library mutation, and automated approval of AI-generated components.
