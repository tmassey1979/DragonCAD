# DragonCAD Remaining Implementation Roadmap

This roadmap breaks the current gaps into vertical slices that can be implemented and tested independently. Each slice should follow test-first development, update the relevant backlog document, and avoid mutating trusted component/library/project state without an explicit review command.

## Epic A - Component Library And Datasheet Intake

1. CMP-001 - Datasheet Import Intake
   - Add a controlled intake queue for local PDF paths and datasheet URLs.
   - Record source type, source identifier, submitted actor, submitted timestamp, optional manufacturer part number, vendor product id, package, and source notes.
   - Emit diagnostics for unsupported file types, missing identifiers, missing local files, and duplicate intake requests.
   - Keep the queue separate from trusted components and generated candidates.

2. CMP-002 - Datasheet Candidate Linking
   - Link intake/candidate records to existing canonical components, vendor rows, imported candidates, or new-candidate placeholders.
   - Record match basis, confidence, conflicts, accepted/rejected state, and reviewer notes.

3. CMP-003 - Datasheet Extraction Provider Boundary
   - Add deterministic provider interfaces for pin/package/fact extraction.
   - Keep AI/Ollama/Codex providers behind disabled provider contracts until explicit credentials/config are present.

4. CMP-004 - Trusted Library Writer
   - Convert staged candidate artifacts into a reviewed library patch.
   - Require simulation, candidate staging, reviewer approval, and deterministic diff output before any trusted library write.

5. CMP-005 - Component Editor Creation Tools
   - Build first-class symbol, footprint, pin, pad, package, and mapping editors.
   - Save/reload native component drafts and promote them through the same review path.

## Epic B - Marketplace And Sourcing

1. MKT-001 - Vendor Catalog Match Review
   - Match vendor rows to DragonCAD components by MPN, normalized value, package, lifecycle, SKU, and provenance.

2. MKT-002 - BOM Order Review
   - Group BOM lines by canonical component, selected offer, quantity, stock, price break, lifecycle, alternates, and blockers.

3. MKT-003 - Order Export And Handoff
   - Produce deterministic CSV/JSON handoff files for purchasing review without live checkout.

4. MKT-004 - Provider Credential Boundaries
   - Add secure local provider configuration for Digi-Key, Mouser, Jameco, SparkFun, and Adafruit.

5. MKT-005 - Live Vendor Sync
   - Add provider-specific HTTP clients only after credentials and terms flows exist.

## Epic C - Fabrication

1. FAB-001 - Manufacturing Output Generation
   - Generate Gerber, drill, BOM, pick-and-place, paste, and board outline artifacts from board geometry.

2. FAB-002 - Cricut/Vinyl Export
   - Export selected copper/paste/outline layers as cutter-friendly vector files.

3. FAB-003 - Fabrication Review And Handoff
   - Build OSH Park and PCBCart review packages with blockers, warnings, hashes, and manual handoff actions.

4. FAB-004 - Live Fabrication Provider Integration
   - Add upload/order APIs only after provider-specific confirmation and credentials flows exist.

## Epic D - Editors

1. EDIT-002R - Schematic Wire Vertex Handles
   - Add visible handles and drag behavior for vertices and segments.

2. EDIT-005R - Symbol Geometry Fidelity
   - Expand symbol primitives, text handling, labels, arcs, and pin geometry fixture coverage.

3. EDIT-006R - Footprint Geometry Fidelity
   - Expand pads, SMDs, holes, keepouts, text, arcs, layers, and geometry hit testing.

4. EDIT-007R - PCB Routing Completion
   - Add pad-start/pad-finish, airwire retirement, 45-degree mode, and route constraints.

5. EDIT-010 - Component Editor Creation Tools
   - Add a dedicated component editor tab and tool rail.

6. EDIT-013 - Markdown Help Viewer
   - Render local markdown help and command documentation inside the app.

## Epic E - Project System

1. PRJ-001 - Project Center
   - Add recent projects, tree organization, create/open project flows, and sample project loading.

2. PRJ-002 - Project Persistence
   - Persist schematic, board, library links, datasheet intake, promotion artifacts, and fabrication metadata.

3. PRJ-003 - Import Project Assembly
   - When importing Eagle files, detect sibling `.sch`, `.brd`, and `.lbr` files and assemble them into one DragonCAD project.

## Execution Rule

The next slice is CMP-001 because datasheet intake is the upstream control point for AI-generated components, vendor linking, trusted-library promotion, and marketplace review.
