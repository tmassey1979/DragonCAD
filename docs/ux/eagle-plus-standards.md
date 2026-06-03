# Eagle-Plus UX Standards

DragonCAD must feel immediately familiar to a fast EAGLE user while removing the parts of EAGLE that made designs fragile, opaque, or hard to review. These standards define the long-term experience target for editor stories. A feature can be compatible with EAGLE without copying every EAGLE implementation detail.

Use this document with the [editor acceptance checklist](editor-acceptance-checklist.md), [EAGLE command parity matrix](../eagle-command-parity.md), and [DOC-007 issue](https://github.com/tmassey1979/DragonCAD/issues/32).

## EAGLE Behavior Classification

Every editor feature that overlaps with an EAGLE workflow must explicitly choose one of these classifications in its story, implementation notes, or acceptance evidence.

| Classification | Meaning | Examples |
| --- | --- | --- |
| Preserve | Keep the EAGLE mental model because it is fast, predictable, and worth carrying forward. | Keyboard-first commands, repeat placement, grid-snapped drawing, quick route/ripup loops. |
| Modernize | Keep the user intent but make the workflow visible, testable, local-first, and easier to recover from. | Toolbars with current state, typed command metadata, richer viewport controls, dockable inspectors. |
| Replace | Intentionally choose a DragonCAD workflow because the EAGLE behavior is unsafe, hidden, or too limited for a modern Hardware IDE. | Arbitrary ULP execution, unreviewed library mutation, live ordering without review, opaque project side effects. |

## Speed

**Target:** Editor work must remain instant enough that users stay in flow while placing, wiring, moving, routing, and inspecting designs.

- Preserve EAGLE's low-latency feel for command invocation, grid snapping, selection, pan/zoom, and repeat actions.
- Modernize performance expectations by making slow operations visible, cancellable when practical, and scoped to the affected editor surface.
- Replace hidden long-running side effects with explicit background tasks, review queues, or diagnostics.
- Editor stories should define the interaction that must stay responsive, not just the data operation that must complete.

## Keyboard Workflows

**Target:** A proficient user can drive the schematic and board editors primarily from the keyboard without losing access to discoverable UI.

- Preserve short command entry, mnemonic shortcuts, Escape cancel, Enter apply, repeat last/active command behavior, and command continuity after placement.
- Modernize keyboard workflows with command names, shortcut discoverability, conflict handling, focus rules, and visible active-tool state.
- Replace brittle script replay or implicit modal state with typed commands that can be tested, documented, and surfaced through menus, toolbars, and command line.
- Keyboard behavior must never depend on canvas-only state that the view model cannot expose or test.

## Toolbars

**Target:** Toolbars make active CAD state scannable without slowing down keyboard users.

- Preserve EAGLE's compact access to high-frequency tools such as add, move, route, ripup, via, grid, layer, rotate, mirror, and delete.
- Modernize with icon rails, context toolbars, disabled states, tooltips, selected-tool highlighting, and workspace-specific command groups.
- Replace overloaded mystery buttons with explicit command names, state labels, and diagnostics where a tool cannot run.
- A toolbar button is complete only when it invokes the same command path as its keyboard and command-line equivalents.

## Command Line

**Target:** The command line is a first-class command surface, not an afterthought.

- Preserve EAGLE's fast typed command habit for users who know what they want.
- Modernize with autocomplete, command previews, argument validation, command history, explainable errors, and links to relevant help topics.
- Replace arbitrary EAGLE script or ULP execution with DragonCAD commands, future reviewed plugin actions, and deterministic reports.
- Command-line features must use the same undo, validation, diagnostics, and review boundaries as toolbar and shortcut actions.

## Viewport Controls

**Target:** Schematic, board, component, and preview viewports are predictable work surfaces for repeated precision work.

- Preserve EAGLE's quick pan, zoom, fit, grid, snap, layer visibility, and route feedback loops.
- Modernize with stable zoom anchors, visible cursor/grid coordinates, explicit snap modes, hover/selection affordances, high-DPI clarity, and per-editor persistence.
- Replace ambiguous hit targets with geometry-aware selection, larger practical click areas, and diagnostics when an item cannot be acted on.
- Viewport behavior must be covered by view-model, mapper, or focused rendering tests where practical.

## Docking

**Target:** DragonCAD should behave like a Hardware IDE, with dockable context panels that keep design, component, sourcing, fabrication, history, and help context available without modal churn.

- Preserve the EAGLE value of keeping command work close to the canvas.
- Modernize with dockable inspectors, command panels, component previews, marketplace review panels, fabrication readiness, help, and project history.
- Replace blocking dialogs for ongoing work with docked or transient surfaces that preserve editor context.
- Docked panels must reflect selection and active tool state without mutating editor data until the user applies a command.

## Local-First Behavior

**Target:** A DragonCAD project remains understandable, portable, and reviewable from local files.

- Preserve EAGLE's ability to work with local design assets without requiring cloud state.
- Modernize with deterministic project folders, Git-friendly persistence, local help, local caches, explicit generated artifacts, and reproducible import plans.
- Replace hidden external dependency assumptions with local records, credential diagnostics, manual feed paths, or disabled live actions.
- Network-backed features must have an offline explanation and must not block basic editing, viewing, or project history inspection.

## Review-First AI

**Target:** AI helps draft, explain, and check engineering work but does not silently author trusted design data.

- Preserve the user's direct control over schematic, board, library, and fabrication decisions.
- Modernize with AI action plans, review queues, provenance, confidence, alternatives, and explicit approve/reject commands.
- Replace automatic mutation with review-required proposals for symbols, footprints, datasheets, attributes, routing suggestions, and diagnostics.
- AI output is complete only when the user can inspect the source evidence, proposed change, affected project objects, and resulting history entry before accepting it.

## Review-First Marketplace

**Target:** Marketplace and sourcing workflows make parts easier to find and price without letting unreviewed vendor data enter trusted libraries or live orders.

- Preserve EAGLE's practical path from design to BOM/export.
- Modernize with vendor match review, freshness state, provenance, duplicate detection, BOM rollups, cart planning, and fabrication handoff packets.
- Replace one-click live checkout, unreviewed catalog promotion, and hidden vendor account assumptions with local review artifacts and disabled live actions until provider stories explicitly enable them.
- Marketplace rows must clearly distinguish trusted placeable components, vendor catalog matches, imported candidates, datasheet-generated drafts, BOM order lines, and fabrication handoff records.

## Project History

**Target:** Users can understand how a design changed, why it changed, and what evidence supported the change.

- Preserve the practical value of file-based project history that works with local version control.
- Modernize with DragonCAD revision timeline entries, command provenance, import diagnostics, marketplace review history, AI proposal records, and manufacturing artifact hashes.
- Replace hidden state changes with explicit history events for accepted editor commands, generated artifacts, library promotions, review decisions, and external handoff packets.
- A feature that changes design intent should state what history evidence it creates or intentionally leaves to Git and local file diffs.

## Feature Completion Rule

An editor feature is not Eagle-Plus complete until it satisfies the [editor acceptance checklist](editor-acceptance-checklist.md). The checklist decides completion evidence; this standards document decides the experience target.
