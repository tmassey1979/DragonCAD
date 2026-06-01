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
- Component editor has tool modes for symbol line, symbol pin, text, package pad, SMD pad, outline, and mapping.
- User can create a new component with at least one symbol, footprint, and device mapping.
- Properties panel edits pin names/numbers, pad numbers, package name, and attributes.
- Save persists the created component in the native library format.
- Tests cover new component creation, pin/pad mapping, property edits, and save/reload.

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
- Help opens as a docked document tab or panel.
- Markdown renders as formatted content, not raw markdown text.
- Tool help pages exist for schematic placement, wiring, PCB routing, layers, grids, and component editing.
- The active tool can open its relevant help page.
- Tests cover help topic lookup and markdown loading fallback.

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
| Pin selection | Planned | Schematic pins can be hit-tested, highlighted, and used as reliable wire start/end anchors with a larger practical click target. | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Via insertion into trace | Planned | PCB routing can insert a via into the active or selected trace, split/continue the route, and switch layers deterministically. | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Package preview switching | In progress | Component browser/editor can switch between available packages and update the preview/active placement package without mutating catalog data. | `src/DragonCAD.App/ComponentManager/**`, matching component manager tests. |
| BOM CSV | Planned | Fabrication output can export deterministic BOM rows to CSV using the normalized BOM aggregation model. | `src/DragonCAD.Fabrication/**`, `tests/DragonCAD.Fabrication.Tests/**`. |
| Quote ladder comparison | In progress | Sourcing can compare normalized vendor price breaks for requested build quantities and sort best options consistently. | `src/DragonCAD.Sourcing/**`, `tests/DragonCAD.Sourcing.Tests/**`. |
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
| Net labels | Planned | Schematic wires and named nets can show, edit, and persist visible net labels that are usable as connection intent and board-sync input. | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Trace width editing | Planned | PCB traces expose editable width properties, render with the selected width, and preserve width through route edits and layer changes. | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Package text filtering | Planned | Component/package selection can filter by package name, footprint type, value text, and package metadata without losing the active package choice. | `src/DragonCAD.App/ComponentManager/**`, matching component manager tests. |
| Pick/place CSV | Planned | Fabrication output can generate deterministic pick-and-place CSV rows with reference, package, position, rotation, and board side. | `src/DragonCAD.Fabrication/**`, `tests/DragonCAD.Fabrication.Tests/**`. |
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
| Net label rendering | Planned | Schematic net labels render on the sheet, can be selected/moved with the mouse, and display net names clearly at grid-snapped positions. | `src/DragonCAD.App/SchematicEditor/**`, matching schematic editor tests. |
| Via size editing | Planned | Selected PCB vias expose editable diameter and drill size, validate positive dimensions, render at the configured size, and preserve layer transition metadata. | `src/DragonCAD.App/BoardEditor/**`, matching board editor tests. |
| Selected package summary | Planned | Component selection shows the active package, footprint preview, variant metadata, and available package count in a readable summary panel. | `src/DragonCAD.App/ComponentManager/**`, `src/DragonCAD.App/Shell/**`, focused component manager/shell tests. |
| Gerber job manifest summary | Planned | Fabrication output surfaces a deterministic manufacturing manifest summary for Gerber, drill, paste, BOM, pick/place, and assembly files. | `src/DragonCAD.Fabrication/**`, optional `src/DragonCAD.App/Fabrication/**`, matching tests. |
| Sourcing provider descriptors | Planned | Vendor integrations can expose local provider descriptors for Digi-Key, Mouser, Jameco, SparkFun, and Adafruit without performing network calls. | `src/DragonCAD.Sourcing/**`, matching sourcing tests. |
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
