# EAGLE Migration Guide

DragonCAD is intended to preserve the fast, keyboard-driven feel of classic EAGLE while translating EAGLE projects into DragonCAD's own project, component, schematic, board, sourcing, and fabrication model. The goal is compatibility at the design-intent layer, not a clone of EAGLE's file format, command parser, ULP runtime, or CAM processor.

Use this guide with the [EAGLE command parity matrix](eagle-command-parity.md) when deciding whether a project can move today, needs manual review after import, or should wait for a planned DragonCAD story.

## Migration Scope

Current DragonCAD EAGLE migration support is centered on local file discovery and predictable project assembly:

- `.sch` files: accepted as EAGLE schematic import inputs and paired with a sibling `.brd` when the names match.
- `.brd` files: accepted as EAGLE board import inputs and paired with a sibling `.sch` when the names match.
- `.lbr` files: accepted as EAGLE library inputs, either as the primary source file, a sibling library with the same base name, or a nearby library under `libraries`, `library`, or `lib`.
- Project assembly: the importer planning layer records the primary schematic, primary board, discovered libraries, missing sibling warnings, and ambiguous library-folder warnings before any downstream migration step consumes the plan.
- Unsupported EAGLE assets: scripts, CAM jobs, design rules, autorouter settings, arbitrary project metadata, and ULP automation are not executed during import.

## Before Import

1. Put the EAGLE design files in one local folder.
2. Keep matching schematic and board files next to each other with the same base name, for example `radio.sch` and `radio.brd`.
3. Keep project-specific libraries next to the design as `radio.lbr`, or place shared libraries under one nearby folder named `libraries`, `library`, or `lib`.
4. If more than one nearby library folder exists, pick the intended libraries manually before relying on an import plan.
5. Keep the original EAGLE files unchanged in source control so review findings can be compared against the source assets.

## Importing Schematic Files

Start from `.sch` when schematic connectivity is the authority for the design. DragonCAD's import assembly planner looks for a same-base-name `.brd` and nearby `.lbr` files, then records any missing board or library ambiguity diagnostics.

After import planning, review:

- Component references, values, and package choices.
- Net names and schematic-to-board connection intent.
- Symbol geometry that may need fidelity follow-up for EAGLE primitives not yet represented in DragonCAD.
- Any library candidates that should remain review-required before promotion into a trusted DragonCAD component library.

## Importing Board Files

Start from `.brd` when PCB layout is the most reliable source or when the schematic is missing. DragonCAD looks for the matching `.sch` file and discovered libraries so board geometry can be reviewed in context.

After import planning, review:

- Board dimensions, layer mapping, footprints, pads, SMDs, vias, holes, and silkscreen.
- Airwire and route state, especially where DragonCAD has partial routing behavior.
- Fabrication readiness separately from import. Importing a board does not mean Gerber, drill, BOM, or pick-and-place outputs are complete.

## Importing Libraries

Start from `.lbr` when the migration task is a component-library review rather than a full design migration. DragonCAD records the selected `.lbr` as the library input and reports missing `.sch` and `.brd` siblings when no matching design files exist.

Imported library content should be treated as candidate component data until reviewed. DragonCAD's library direction is review-first: imported symbols, footprints, packages, datasheets, sourcing records, and generated candidates should not overwrite trusted components without an explicit promotion workflow.

## Project Assembly

DragonCAD's EAGLE project assembly behavior is deterministic:

- A source file must end in `.sch`, `.brd`, or `.lbr`.
- Matching schematic and board files are discovered by base name in the same folder.
- A sibling library named with the same base name is included when present.
- A single nearby library folder named `libraries`, `library`, or `lib` is included when it contains `.lbr` files.
- Multiple nearby library folders are reported as ambiguous instead of guessed.
- Missing schematic or board siblings are warnings, not silent failures.

This planning step is intentionally separate from editor behavior. It does not implement routing, symbol editing, board editing, fabrication export, or source mutation.

## ULP Migration Direction

EAGLE ULPs should be migrated by intent, not executed directly inside DragonCAD. Treat each ULP as one of these categories:

- Reporting ULPs: migrate to deterministic DragonCAD reports, BOM exports, fabrication manifests, or docs tooling.
- Library-generation ULPs: migrate to component draft, datasheet intake, and reviewed trusted-library promotion flows.
- CAM/export ULPs: migrate to DragonCAD fabrication output and handoff stories.
- Editor automation ULPs: migrate to explicit DragonCAD commands or future plugin actions with testable boundaries.
- Vendor/order ULPs: migrate to review-first sourcing, BOM, cart, and handoff flows. Live checkout and live ordering remain out of scope until provider-specific stories define credentials, terms, and confirmation behavior.

Do not assume a ULP can be ported one-to-one. DragonCAD should expose durable workflows, typed commands, and auditable review records instead of replaying arbitrary EAGLE scripting side effects.

## Known Limitations

- EAGLE import planning exists, but migration fidelity still depends on downstream editor, library, project persistence, and fabrication stories.
- Symbol and footprint geometry are partial where DragonCAD does not yet cover every EAGLE primitive, text behavior, arc, layer, pad, SMD, hole, or keepout detail.
- Schematic net labels, board route completion, airwire retirement, 45-degree routing, trace widths, and via sizes have planned or partial DragonCAD coverage.
- Project center flows, full save/open shell commands, and richer project persistence are not complete for every migration workflow.
- EAGLE ULPs, CAM jobs, autorouter state, live vendor checkout, live fabrication upload, and payment/shipping automation are intentionally not imported or executed.
- Imported catalog or datasheet data must remain review-required until promoted through trusted DragonCAD library workflows.

## Recommended Review Checklist

After each migration, confirm:

- The expected `.sch`, `.brd`, and `.lbr` files were discovered.
- Missing sibling or ambiguous library diagnostics were resolved or accepted.
- Symbols, footprints, package selections, and component references match the source design intent.
- Nets, airwires, and board routes still describe the same electrical intent.
- Fabrication outputs are regenerated and reviewed in DragonCAD instead of copied blindly from EAGLE CAM output.
- Any old ULP workflow has an explicit DragonCAD replacement, planned issue, or documented manual step.
