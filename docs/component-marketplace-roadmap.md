# Component Marketplace Roadmap

This document tracks the remaining component library and marketplace work as vertical stories. It is intentionally scoped to roadmap guidance only; implementation agents should treat each story as a separately assignable slice with its own tests, screenshots where applicable, and narrow file ownership.

The current marketplace surface already supports unified library/marketplace rows, review-only BOM/order previews, checkout readiness, local order records, order history, saved filters, quality badges, and fabrication checklist previews. The remaining work should keep that review-first posture: no generated component, vendor catalog match, BOM draft, order record, or fabrication package should mutate trusted project state or place a live external order without an explicit review step.

## Roadmap Principles

- Keep vendor credentials, payment data, shipping data, and API tokens outside project files.
- Keep verified DragonCAD library components separate from catalog-only, imported, and generated candidates until a review step promotes or links them.
- Preserve provenance for every datasheet, catalog row, vendor offer, order draft, fabrication artifact, and reviewer decision.
- Prefer deterministic planning, preview, and export artifacts before live integration with vendors or manufacturers.
- Do not silently overwrite verified symbol, footprint, pinout, package, 3D geometry, pricing locks, or do-not-substitute rules.
- Treat live vendor checkout, live fabrication upload, and live order placement as blocked until provider-specific credentials, terms, and confirmation flows are implemented.

---

## CMP-001 - Datasheet Import Intake

Status: Implemented as Wave 41 in `docs/marketplace-library-epic.md`.

**As a** component librarian, **I want** to import datasheets into a controlled intake queue, **so that** missing components can be drafted without immediately changing the trusted library.

**AC:**
- The intake flow accepts a local PDF path or source URL plus optional manufacturer part number, vendor product ID, package, and source notes.
- Each intake item records source type, source identifier, retrieval or file timestamp, submitted actor, and review-required status.
- Invalid files, unsupported formats, missing source identifiers, and duplicate intake requests produce reviewable diagnostics.
- Intake items can be listed, filtered, and selected without launching an AI provider or mutating component library storage.
- Tests cover local PDF intake, URL intake, duplicate request detection, unsupported format diagnostics, and required provenance fields.

**Implementation Notes:**
- Build on the existing datasheet review and promotion concepts before adding new UI surfaces.
- Keep parsing/extraction behind interfaces so tests can use deterministic fakes.
- Store intake records separately from verified components and catalog offers.
- Any eventual PDF download or external fetch must be provider-gated and disabled in unit tests.

**Agent Boundary:** Datasheet intake records, validation, and queue plumbing only. Do not generate component geometry, promote candidates, call AI providers, or edit marketplace shell layout.

---

## CMP-002 - Datasheet Candidate Linking

Status: Implemented as Wave 42 in `docs/marketplace-library-epic.md`.

**As a** component librarian, **I want** datasheet-derived drafts linked to existing canonical components or catalog rows, **so that** generated metadata can be reviewed in context instead of creating duplicate parts.

**AC:**
- A datasheet candidate can link to an existing canonical component, vendor catalog row, imported component candidate, or a new-candidate placeholder.
- Link suggestions include match basis, confidence, conflicting fields, missing fields, and source provenance.
- Reviewers can accept a link, reject a link, or request more data without changing verified component geometry.
- Accepted links produce deterministic decision records that can be audited later.
- Tests cover exact MPN match, package conflict, vendor-row-only match, no-match candidate, rejected link, and accepted-link decision output.

**Implementation Notes:**
- Reuse canonical identity and component promotion models where they already exist.
- Keep link suggestions separate from promotion commands; linking context is not library approval.
- Conflicts should preserve both source values and never prefer generated values automatically.
- The UI can surface decisions later, but this story should first stabilize the model and command behavior.

**Agent Boundary:** Datasheet-to-component/catalog linking model and deterministic decision commands only. Do not persist trusted library changes, call vendors, or modify fabrication/order flows.

---

## CMP-003 - Vendor Catalog Match Review

**As a** sourcing user, **I want** vendor catalog rows matched against DragonCAD components with visible confidence and conflicts, **so that** BOM and placement decisions can use vendor data without confusing catalog-only parts with verified components.

**AC:**
- Vendor rows can be matched by manufacturer part number, normalized value, package, footprint class, lifecycle, and vendor-specific SKU.
- Match output distinguishes exact component match, likely alternate, package variant, value variant, catalog-only row, and conflict.
- Review state records accepted match, rejected match, ignored offer, and reviewer notes.
- Catalog-only rows remain unavailable for direct placement unless linked to or promoted as verified components.
- Tests cover API-backed vendor row matching, manual/feed row matching, duplicate vendor offers, package conflicts, obsolete lifecycle warnings, and rejected match state.

**Implementation Notes:**
- Keep vendor catalog data separate from verified components and imported library assets.
- Do not add live vendor sync in this story; use fixtures or existing catalog records.
- Matching should emit stable explanations so the shell can show why a part is blocked or recommended.
- Preserve vendor capability and terms metadata with each match decision.

**Agent Boundary:** Vendor catalog matching and review state only. Do not implement provider HTTP clients, credential storage, checkout, or placement commands.

---

## CMP-004 - BOM Order Review

**As a** hardware developer, **I want** to review a BOM-derived order plan before checkout, **so that** quantities, alternates, vendor choices, blockers, and local order records are correct before any external action.

**AC:**
- The review surface groups BOM lines by canonical component, selected vendor offer, quantity, price break, lifecycle, stock, lead time, and do-not-substitute status.
- Blockers are visible for missing vendor match, obsolete lifecycle, insufficient stock, unresolved alternate, missing credentials, missing shipping profile, and missing payment method.
- Users can choose preferred offer, lock a line, mark do-not-substitute, accept an alternate, or defer a line as local review decisions.
- The order plan can create or update a local review record without placing a live order or mutating a vendor cart.
- Tests cover clean order plan, missing offer blocker, insufficient stock blocker, alternate accepted, do-not-substitute lock, local record update, and no-live-order behavior.

**Implementation Notes:**
- Build on the existing local order draft, checkout readiness, and order history work.
- Keep pricing and stock values as snapshots with retrieval timestamps.
- Do not calculate tax, freight, or final vendor confirmation unless a provider-specific story owns that contract.
- Review decisions should be serializable for later export and audit.

**Agent Boundary:** BOM order review model, commands, and app-facing view models only. Do not call live vendor APIs, capture payment, submit carts, or write provider order confirmations.

---

## CMP-005 - Order Export And Vendor Handoff

**As a** purchasing reviewer, **I want** deterministic order export and vendor handoff artifacts, **so that** reviewed BOM/order data can be transferred manually or through future provider-specific integrations.

**AC:**
- Export artifacts include reviewed BOM lines, vendor SKUs, quantities, alternates, locked lines, omitted lines, blocker summary, provenance, and generated timestamp.
- CSV and JSON export previews are deterministic and can be written only through an explicit export command.
- Provider handoff actions describe the next manual or provider-specific step without claiming an order was placed.
- Export records link back to the local order review record and preserve no-live-submission status.
- Tests cover CSV preview, JSON preview, explicit export command, blocked export diagnostics, provider handoff action text, and no-order-placement guarantees.

**Implementation Notes:**
- Use the existing BOM CSV preparation and local order record concepts as inputs.
- Separate preview generation from file writing so command tests stay deterministic.
- Redact credentials and exclude payment/shipping secrets from every export.
- Keep provider-specific checkout automation out of scope until credentials and terms stories exist.

**Agent Boundary:** Reviewable order export artifacts and handoff action descriptors only. Do not automate checkout, mutate vendor carts, or place purchases.

---

## CMP-006 - Fabrication Ordering Review

**As a** board designer, **I want** prototype and production fabrication packages reviewed before leaving DragonCAD, **so that** OSH Park and PCBCart handoffs are complete, explainable, and blocked when required artifacts are missing.

**AC:**
- Prototype review checks Gerbers, drill files, outline, layer mapping, board dimensions, warning acceptance, and OSH Park package readiness.
- Production review checks Gerbers, drill files, BOM, pick-and-place, stackup, quantity, assembly side, warning acceptance, and PCBCart package readiness.
- Review output separates ready, warning-accepted, blocked, and unsupported-provider states.
- Users can accept or revoke warnings, export a package manifest, and open a handoff action only after required review state is satisfied.
- Tests cover ready prototype package, missing drill blocker, accepted warning, ready production assembly package, missing BOM blocker, manifest export, and disabled handoff action.

**Implementation Notes:**
- Build on the existing fabrication checklist preview and readiness command direction.
- Treat OSH Park and PCBCart as handoff providers unless formal provider APIs are explicitly introduced.
- The handoff action should return a safe action description and target, not upload files or launch a process in tests.
- Preserve artifact hashes or stable identifiers so a reviewed package can be tied to later manufacturing records.

**Agent Boundary:** Fabrication ordering review state, package manifest, warning decisions, and handoff action planning only. Do not generate fabrication files, upload packages, scrape quote pages, or place manufacturing orders.

---

## CMP-007 - Marketplace App Shell Integration

**As a** DragonCAD user, **I want** component marketplace actions integrated into the main workspace shell, **so that** library search, datasheet intake, vendor matching, BOM review, order review, and fabrication review are reachable from one coherent surface.

**AC:**
- The shell exposes visible entry points for component library search, datasheet intake queue, datasheet linking review, vendor catalog match review, BOM order review, order export/handoff, and fabrication ordering review.
- Disabled states and diagnostics are visible for missing credentials, missing datasheets, unresolved component links, blocked order reviews, blocked exports, and missing fabrication artifacts.
- Shell labels use review-first language and do not claim live sync, checkout, upload, ordering, automatic placement, or automatic library promotion.
- The shell preserves existing marketplace order history, quality badges, saved filters, and checkout readiness surfaces.
- Tests cover tab/panel creation, command binding, disabled states, diagnostic text, no-live-action labels, and at least one launched-app screenshot after implementation.

**Implementation Notes:**
- Coordinate this story after CMP-001 through CMP-006 expose stable app-facing APIs.
- Keep shell code thin; domain behavior belongs to datasheet, catalog matching, order review, export, and fabrication view models.
- Avoid duplicating marketplace state in the shell. Bind to existing services or view models.
- This should be the final integration pass for a wave, not the first story in the wave.

**Agent Boundary:** Main shell wiring, navigation, command binding, and visible diagnostics only. Do not implement datasheet extraction, vendor matching logic, order export generation, or fabrication readiness logic inside shell code.

---

## Suggested Execution Order

1. CMP-001 - Datasheet Import Intake.
2. CMP-002 - Datasheet Candidate Linking.
3. CMP-003 - Vendor Catalog Match Review.
4. CMP-004 - BOM Order Review.
5. CMP-005 - Order Export And Vendor Handoff.
6. CMP-006 - Fabrication Ordering Review.
7. CMP-007 - Marketplace App Shell Integration.

This order keeps core review models ahead of shell work and avoids adding UI entry points before the underlying commands can provide deterministic state, blockers, and audit output.
