# Editor Acceptance Checklist

Use this checklist before considering a DragonCAD editor feature complete. Each section links to at least one current GitHub issue or milestone so future stories can trace the completion bar back to active product planning.

Primary tracking: [DOC-007 issue](https://github.com/tmassey1979/DragonCAD/issues/32) and [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7).

## 1. EAGLE-Plus Classification

Links: [DOC-007 issue](https://github.com/tmassey1979/DragonCAD/issues/32), [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7)

- The story states which EAGLE behavior it preserves, modernizes, or intentionally replaces.
- The implementation notes explain why that classification is appropriate for the target workflow.
- Any intentional difference from EAGLE is visible in UI copy, diagnostics, help, or issue notes.
- The feature does not claim EAGLE parity when it only supports a DragonCAD subset.

## 2. Speed And Responsiveness

Links: [PCB-003 issue](https://github.com/tmassey1979/DragonCAD/issues/11), [Epic 3 milestone](https://github.com/tmassey1979/DragonCAD/milestone/3)

- High-frequency editor actions respond immediately enough for repeated placement, routing, selection, movement, and viewport control.
- Slow or deferred operations expose progress, disabled states, diagnostics, or review queues instead of blocking silently.
- Repeated commands do not require unnecessary modal reopen cycles.
- Performance-sensitive code paths have focused tests, profiling notes, or manual validation evidence appropriate to the risk.

## 3. Keyboard Workflow

Links: [DOC-006 issue](https://github.com/tmassey1979/DragonCAD/issues/31), [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7)

- The primary workflow is reachable by keyboard shortcut, command-line command, menu, or documented focus path.
- Escape, Enter, repeat action, and cancel behavior are defined where the tool has modal state.
- Shortcut conflicts, disabled states, and invalid command arguments produce explainable feedback.
- Keyboard and pointer flows call the same command/view-model behavior.

## 4. Toolbar And Command Surface

Links: [DOC-006 issue](https://github.com/tmassey1979/DragonCAD/issues/31), [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7)

- Toolbar, context toolbar, menu, shortcut, and command-line entry points share one command contract.
- The active tool, active layer, snap/grid state, and selected command are visible where relevant.
- Icon-only controls have accessible names or tooltips once exposed in UI.
- Disabled commands explain missing selection, missing project state, unreviewed data, or unsupported editor context.

## 5. Viewport Controls

Links: [PCB-003 issue](https://github.com/tmassey1979/DragonCAD/issues/11), [Epic 2 milestone](https://github.com/tmassey1979/DragonCAD/milestone/2)

- Pan, zoom, fit, grid, snap, selection, hover, and active-tool previews behave consistently in the affected editor.
- Hit testing uses real rendered/editor geometry where available, not only broad placeholder rectangles.
- Coordinates, snap targets, active layer, and route/wire previews are visible when they affect user decisions.
- Viewport state survives normal tool changes and is persisted when the story claims persistence.

## 6. Docking And Workspace Context

Links: [DOC-007 issue](https://github.com/tmassey1979/DragonCAD/issues/32), [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7)

- Properties, help, marketplace, fabrication, project, or history context appears without forcing unnecessary canvas mode loss.
- Docked panels read selection and active tool state without mutating project data until a command is applied.
- Opening a panel does not duplicate equivalent workspace surfaces or hide the primary editor action.
- The feature has a clear owner for shell integration if docking is deferred from the current story.

## 7. Local-First Project Behavior

Links: [PRJ-004 issue](https://github.com/tmassey1979/DragonCAD/issues/16), [Epic 4 milestone](https://github.com/tmassey1979/DragonCAD/milestone/4)

- The feature works from local project files and does not require cloud state for core editing.
- Project mutations are deterministic, reloadable, and compatible with source control review.
- Imports, generated artifacts, caches, and external references are recorded with enough local evidence to review later.
- Offline, missing-credential, and missing-file states have explicit diagnostics.

## 8. Review-First AI

Links: [IDE-003 issue](https://github.com/tmassey1979/DragonCAD/issues/23), [Epic 6 milestone](https://github.com/tmassey1979/DragonCAD/milestone/6)

- AI-generated or AI-suggested changes remain proposals until the user reviews and accepts them.
- The proposal shows source evidence, affected design objects, confidence or uncertainty, and expected output.
- Rejecting or ignoring a proposal leaves trusted project data unchanged.
- Accepted AI changes create or participate in project history/provenance.

## 9. Review-First Marketplace

Links: [MKT-001 issue](https://github.com/tmassey1979/DragonCAD/issues/19), [Epic 5 milestone](https://github.com/tmassey1979/DragonCAD/milestone/5)

- Trusted placeable components, vendor catalog matches, imported candidates, generated drafts, BOM rows, and order/handoff records are visually distinct.
- Catalog, datasheet, or vendor data cannot become trusted library data without explicit review and promotion.
- BOM carts, fabrication handoffs, and vendor actions clearly separate local review artifacts from live checkout, upload, payment, or order placement.
- Provider freshness, credential, rate-limit, and stale-price states are visible before the user relies on external data.

## 10. Project History And Provenance

Links: [IDE-004 issue](https://github.com/tmassey1979/DragonCAD/issues/24), [Epic 6 milestone](https://github.com/tmassey1979/DragonCAD/milestone/6)

- Design-intent changes produce a history entry, audit event, local diff, or documented provenance record.
- The record identifies the command, source evidence, affected objects, review decision, and generated artifacts when those concepts apply.
- Undo/recovery expectations are defined for the feature's mutation boundary.
- Generated manufacturing, BOM, import, marketplace, and AI artifacts include stable hashes, paths, IDs, or review references where practical.

## 11. Help And Documentation

Links: [DOC-006 issue](https://github.com/tmassey1979/DragonCAD/issues/31), [DOC-008 issue](https://github.com/tmassey1979/DragonCAD/issues/33)

- User-facing behavior is reflected in local help, command reference, release notes, or story documentation before the feature is marked complete.
- Help text distinguishes implemented behavior from planned EAGLE parity.
- Keyboard shortcuts, command-line forms, toolbar location, and review constraints are documented when exposed.
- Any intentionally replaced EAGLE behavior links back to the standard or migration guidance explaining the replacement.

## 12. Verification Evidence

Links: [DOC-007 issue](https://github.com/tmassey1979/DragonCAD/issues/32), [Epic 7 milestone](https://github.com/tmassey1979/DragonCAD/milestone/7)

- Focused automated tests cover command/view-model behavior, persistence, rendering mapping, or validation logic at the right boundary.
- Manual validation covers visible editor behavior when the feature changes canvas, toolbar, docking, or viewport UX.
- Documentation validation passes for any changed docs or command references.
- Completion notes list the exact commands run, results, changed files, and remaining known limitations.
