# DragonCAD Marketplace Library Epic

DragonCAD's component library should become a permanent, searchable engineering catalog rather than a static folder of symbols. The long-term target is a marketplace-grade library that combines native DragonCAD components, imported Eagle libraries, vendor product catalogs, datasheets, BOM sourcing, and manufacturing handoff.

This epic is intentionally split into vertical stories so agents can work independently without overlapping editor, importer, sourcing, fabrication, or AI boundaries.

## Official API Reality

- Digi-Key: API-backed product information is available through the Digi-Key Developer Portal, including product search and product detail workflows.
- Mouser: API-backed search is available and includes product data, availability, datasheet URL, image URL, lifecycle, compliance, MOQ, lead time, and pricing breaks.
- Adafruit: product lookup is available through the public Adafruit product API endpoint.
- SparkFun: public open hardware repositories and Eagle libraries are a strong source of symbols, footprints, example designs, and product metadata; treat product catalog enrichment as repository/product-page ingestion unless a formal catalog API is added later.
- OSH Park: supports upload-link/API handoff for board files, but preview, warning, and account attachment are not exposed through that upload path. DragonCAD should generate a validated upload package and hand off to OSH Park.
- PCBCart: supports web quote/order workflows for PCB and assembly production. Treat this as a quote/order handoff unless a formal partner API is available.
- Jameco: no formal public catalog API/feed is assumed. Support manual catalog import, negotiated feed ingestion, or curated CSV/catalog workflows until a formal API/feed exists.

These assumptions describe integration boundaries, not a promise that DragonCAD is currently performing live provider calls or order submission. Live API use must remain opt-in, credential-backed, and covered by provider-specific stories. Order, upload, cart, and checkout behavior remains reviewable handoff unless a later story explicitly owns live submission.

## Marketplace Architecture Principles

- Keep vendor credentials out of project files.
- Keep vendor catalogs separate from verified native components.
- Use canonical component identity to deduplicate common parts across vendors.
- Never silently overwrite verified symbol, footprint, pinout, package, or 3D geometry.
- Datasheet-generated components must start as draft components and require review before promotion.
- BOM purchasing must preserve alternates, lifecycle, stock, lead time, price breaks, preferred vendor, and do-not-substitute rules.
- Manufacturing handoff must generate reviewable artifacts first, then open vendor handoff flows.
- Legal and terms-of-service limits must be represented as provider capabilities, not hidden implementation details.

## Implementation Order

1. Marketplace contracts and vendor credential boundary.
2. Canonical component identity and dedupe model.
3. Permanent library import and source provenance.
4. Vendor catalog ingestion for API-backed providers.
5. Manual/feed ingestion fallback for vendors without public APIs.
6. Datasheet-backed component draft generation.
7. Human review and promotion of generated components.
8. BOM planning and vendor quote comparison.
9. Order cart handoff.
10. OSH Park prototype package handoff.
11. PCBCart production quote/order package handoff.
12. Marketplace legal, attribution, and terms guardrails.

## Epic Status And Sequencing

This table is the coordination index for parallel marketplace agents. Story-level sections remain the source of acceptance criteria and owned paths; completion notes later in this file record delivered implementation waves. Status here uses documentation status only so it does not overstate live provider, ordering, or library-mutation behavior.

| Phase | Stories | Coordination status | Parallelization boundary |
| --- | --- | --- | --- |
| Foundation contracts | `MKT-001`, `MKT-002`, `MKT-014` | Sequenced before provider, order, and fabrication work. Later provider waves may consume these contracts but must not store secrets or hide terms limits. | Sourcing contracts, credential abstractions, compliance metadata. |
| Component identity and trusted library | `MKT-003`, `MKT-004`, `MKT-008`, `MKT-016`, `MKT-023`, `MKT-026`, `MKT-032` | Library maturity gate for editor placement. Generated, imported, and vendor-linked parts stay review-required until promotion records make them trusted/placeable. | Core component identity, permanent library, review/promotion, audit trail. |
| Catalog ingestion and provider sync | `MKT-005`, `MKT-006`, `MKT-017`, `MKT-025`, `MKT-029` | API-backed and manual/feed providers stay behind adapters, fixtures, request plans, freshness state, and credential diagnostics. | Sourcing provider adapters, source manifests, sync planning, status models. |
| Datasheet draft workflow | `MKT-007`, `MKT-020`, `MKT-030` | Datasheet output is draft/review-only until explicit promotion. UI can surface queues and commands but must not call AI or mutate the trusted library directly. | Component-intelligence contracts, app review view models, promotion commands. |
| BOM and cart planning | `MKT-010`, `MKT-011`, `MKT-018`, `MKT-022`, `MKT-028` | BOM/order state is derived from placed components and reviewable plans. Cart commands may update local state but must not submit orders. | Sourcing BOM planning, cart workspace, quantity commands. |
| Fabrication handoff | `MKT-012`, `MKT-013`, `MKT-019`, `MKT-024`, `MKT-031` | OSH Park and PCBCart flows are package validation and handoff planning unless a provider-specific live story exists. | Fabrication providers, app handoff view models, readiness commands. |
| Shell/editor integration | `MKT-009`, `MKT-021`, `MKT-027`, `MKT-033` | Shell work runs after backing models expose stable view-model APIs. Editor placement must still require trusted placeable components. | Component browser, marketplace panels, shell navigation and command wiring. |
| Documentation coordination | `MKT-015` | Implemented by this epic/status document and matching editor backlog links. Future waves should update this section only when sequencing or boundaries change. | `docs/marketplace-library-epic.md`, `docs/editor-interaction-backlog.md`. |

### Six-Agent Wave Rules

- One agent may own each backend domain in a wave: core library/identity, sourcing providers, BOM/cart planning, fabrication handoff, datasheet/review, and app shell/editor integration.
- App shell/editor integration runs after domain-owned view models or services are stable; it should bind to those APIs rather than duplicating business rules.
- Provider work must use deterministic fixtures or explicit opt-in live smoke paths; normal tests must not require live credentials or network access.
- Marketplace rows, imported candidates, and datasheet drafts are not placeable editor components until review/promotion marks them trusted.
- Ordering and fabrication stories must use reviewable records, exports, or handoff actions and must not imply live checkout, payment, shipping, upload, or provider order submission.

---

## MKT-001 - Marketplace Provider Contracts

**As a** library maintainer, **I want** stable provider contracts for component vendors and manufacturers, **so that** catalog, sourcing, and fabrication integrations can evolve independently.

**AC:**
- Contracts exist for catalog search, catalog product detail, pricing, stock, lifecycle, datasheet links, image links, and vendor capability metadata.
- Manufacturing provider contracts exist for prototype board handoff, production quote handoff, required artifact lists, and unsupported capability diagnostics.
- Provider responses include source vendor, source URL or ID, retrieval timestamp, and terms/capability flags.
- Tests cover provider capability serialization and validation.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Marketplace/**`, `tests/DragonCAD.Sourcing.Tests/Marketplace/**`.
- Keep contracts independent from Avalonia UI.
- Do not implement real HTTP clients in this story.
- Do not store API keys in project files or logs.
- Validate with `dotnet test DragonCAD.slnx --filter Marketplace`.

**Agent Boundary:** Sourcing marketplace contracts only.

---

## MKT-002 - Vendor Credential Boundary

**As a** DragonCAD user, **I want** vendor API credentials stored outside project files, **so that** projects can be shared without leaking accounts or keys.

**AC:**
- Credential abstractions support Digi-Key, Mouser, Adafruit, SparkFun, Jameco, OSH Park, and PCBCart provider identifiers.
- Project save output contains provider IDs and user choices but no secrets.
- Missing credential diagnostics are actionable and provider-specific.
- Tests prove credentials are redacted from diagnostics, logs, and project serialization.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Credentials/**`, `tests/DragonCAD.Sourcing.Tests/Credentials/**`.
- Use an interface boundary so platform-specific secure storage can be added later.
- Do not wire native keychain, Windows Credential Manager, or macOS Keychain yet unless a separate platform story owns it.
- Validate with `dotnet test DragonCAD.slnx --filter Credential`.

**Agent Boundary:** Credential model and redaction only.

---

## MKT-003 - Canonical Component Identity

**As a** component librarian, **I want** equivalent vendor parts linked to one canonical component, **so that** common parts like 7805 regulators, NE555 timers, ESP32 boards, resistors, and capacitors do not fragment the library.

**AC:**
- Canonical identity supports manufacturer part number, normalized generic family, electrical value, tolerance, voltage/current ratings, package, pin count, footprint class, lifecycle, and source confidence.
- Vendor offers can attach to a canonical component without replacing verified geometry.
- Dedupe suggestions classify exact match, likely alternate, value variant, package variant, and conflict.
- Tests cover 7805, NE555, ESP32 dev board, resistor value variants, capacitor value variants, and package conflicts.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Components/Identity/**`, `tests/DragonCAD.Core.Tests/Components/Identity/**`.
- Do not change schematic or board editor placement behavior.
- Do not call vendor APIs directly.
- Contract changes under `src/DragonCAD.Core/Contracts/**` require a separate coordination story.
- Validate with `dotnet test DragonCAD.slnx --filter ComponentIdentity`.

**Agent Boundary:** Core component identity and dedupe rules only.

---

## MKT-004 - Permanent Library Import

**As a** hardware designer, **I want** imported components to become permanent library assets, **so that** every imported Eagle, SparkFun, Adafruit, or user library improves future projects.

**AC:**
- Imported components are written to a permanent DragonCAD library store with source provenance.
- Re-importing the same source links to existing canonical components where possible.
- Conflicts are preserved as review items instead of overwriting existing verified assets.
- Tests cover SparkFun-style Eagle import, user library import, duplicate import, and conflict import.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Libraries/Permanent/**`, `tests/DragonCAD.Core.Tests/Libraries/Permanent/**`.
- May consume existing importer outputs but must not edit importer parsing code.
- Do not fetch external repositories in tests.
- Validate with deterministic fixture imports.

**Agent Boundary:** Permanent library persistence and provenance only.

---

## MKT-005 - API-Backed Catalog Ingestion

**As a** sourcing user, **I want** DragonCAD to ingest product data from API-backed vendors, **so that** library search and BOM planning use current vendor data.

**AC:**
- Digi-Key and Mouser adapters map product search/detail responses into DragonCAD catalog records.
- Adafruit product API records map into DragonCAD catalog records.
- Provider adapters expose rate-limit, credential, and terms diagnostics.
- Tests use recorded fixtures and do not call live APIs.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Vendors/ApiBacked/**`, `tests/DragonCAD.Sourcing.Tests/Vendors/ApiBacked/**`.
- Live network calls must stay behind provider interfaces.
- Do not place real API keys in repo fixtures.
- Do not normalize into canonical components here; emit candidate catalog records only.

**Agent Boundary:** API-backed vendor ingestion only.

---

## MKT-006 - Open Hardware And Manual Catalog Ingestion

**As a** library maintainer, **I want** SparkFun, Adafruit repository assets, and Jameco/manual catalog feeds imported through controlled pipelines, **so that** useful parts are available even when a formal API is missing.

**AC:**
- SparkFun open hardware repositories and Eagle libraries can be represented as catalog/library source manifests.
- Adafruit open hardware/library sources can be represented as catalog/library source manifests.
- Jameco supports curated CSV/manual-feed ingestion with provenance and refresh metadata.
- Unsupported scraping is blocked unless a provider explicitly allows it.
- Tests cover manifest validation, manual CSV parsing, duplicate source rows, and blocked unsupported source modes.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Vendors/OpenHardware/**`, `tests/DragonCAD.Sourcing.Tests/Vendors/OpenHardware/**`.
- Do not add repository downloads to app startup.
- Do not imply Jameco has a public API until one is explicitly configured.
- Keep legal/terms flags visible in provider metadata.

**Agent Boundary:** Open hardware and manual/feed source ingestion only.

---

## MKT-007 - Datasheet-Backed Component Draft Generation

**As a** component librarian, **I want** DragonCAD to create draft components from datasheets, **so that** missing symbols, footprints, and metadata can be built faster.

**AC:**
- A datasheet ingestion request can include PDF path, source URL, vendor product ID, manufacturer part number, and target package.
- Output is a draft component with extracted pins, pin names, electrical types, package hints, footprint candidates, metadata, confidence scores, and warnings.
- Draft output never becomes verified automatically.
- Tests cover extraction request validation, draft component shape, unsupported package diagnostics, and warning propagation.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.ComponentIntelligence/Datasheets/**`, `tests/DragonCAD.ComponentIntelligence.Tests/Datasheets/**`.
- Keep Codex/Ollama/provider calls behind an interface; tests must use deterministic fakes.
- Do not generate board editor geometry directly in this story.
- Include a review-required flag on all generated output.

**Agent Boundary:** Datasheet draft-generation contracts and deterministic pipeline only.

---

## MKT-008 - Component Review And Promotion

**As a** library maintainer, **I want** to review and promote generated or imported component candidates, **so that** only verified geometry enters the trusted library.

**AC:**
- Review items show source, extracted metadata, symbol preview status, footprint preview status, conflicts, and warnings.
- Promotion can link to an existing canonical component, create a new canonical component, or reject a candidate.
- Promotion records reviewer, timestamp, source, and changed fields.
- Tests cover promote-new, link-existing, reject, and conflict-required-review flows.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Libraries/Review/**`, `tests/DragonCAD.Core.Tests/Libraries/Review/**`.
- UI review panels are out of scope unless a separate app story owns them.
- Do not call AI providers or vendor APIs.
- Preserve immutable source provenance.

**Agent Boundary:** Library review workflow model only.

---

## MKT-009 - Marketplace Component Browser

**As a** schematic designer, **I want** one component browser for native, imported, generated, and vendor catalog parts, **so that** I can find and place parts without switching tools.

**AC:**
- Browser filters by part type, value, package, vendor availability, lifecycle, verified status, and source.
- Search results separate verified placeable components from catalog-only and draft components.
- Selecting a component shows symbol, footprint, package options, datasheet link, vendor offers, and warnings.
- Only verified placeable components can be placed without a review step.
- View model tests cover filtering, source grouping, verified/draft state, and package selection.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/ComponentManager/**`, `tests/DragonCAD.App.Tests/ComponentManager/**`.
- Do not implement vendor HTTP calls in the UI.
- Use existing placement commands for schematic insertion.
- Do not edit low-level canvas rendering.

**Agent Boundary:** Component browser UI/view model only.

---

## MKT-010 - BOM Planning And Vendor Quote Comparison

**As a** project owner, **I want** DragonCAD to plan BOM purchasing across vendors, **so that** I can estimate build cost, stock risk, and order quantities before fabrication.

**AC:**
- BOM planner groups design components by canonical identity and selected value/package.
- Planner supports alternates, preferred vendors, do-not-substitute, MOQ, order multiples, stock, lead time, lifecycle, and price breaks.
- Quote comparison calculates per-build, prototype batch, and production batch totals.
- Export includes BOM CSV and reviewable order plan JSON.
- Tests cover 1-off, 10-board, 100-board, MOQ, price break, missing stock, and alternate substitution cases.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/BomPlanning/**`, `tests/DragonCAD.Sourcing.Tests/BomPlanning/**`.
- Do not place orders automatically.
- Do not mutate project components during quote comparison.
- Keep currency and timestamp explicit.

**Agent Boundary:** BOM planning and quote calculations only.

---

## MKT-011 - Vendor Cart Handoff

**As a** buyer, **I want** DragonCAD to build reviewable vendor carts or order handoff files, **so that** I can move from BOM planning to purchasing without retyping part numbers.

**AC:**
- Handoff records include vendor part number, quantity, source offer, price snapshot, timestamp, and user review status.
- Providers can declare direct cart support, CSV upload support, copy/paste support, or unsupported mode.
- User-visible diagnostics explain missing credentials, missing vendor part numbers, and stale quotes.
- Tests cover cart-capable, CSV-only, manual handoff, stale quote, and missing offer cases.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Orders/**`, `tests/DragonCAD.Sourcing.Tests/Orders/**`.
- Do not submit real orders.
- Do not automate browser checkout.
- Do not store payment or shipping data in project files.

**Agent Boundary:** Purchasing handoff artifacts only.

---

## MKT-012 - OSH Park Prototype Handoff

**As a** hardware developer, **I want** DragonCAD to prepare an OSH Park prototype board handoff, **so that** I can order prototypes from validated Gerbers quickly.

**AC:**
- Handoff package includes Gerbers, drill files, board outline, layer mapping, board dimensions, manifest, and preflight diagnostics.
- OSH Park upload link/handoff is generated only after manufacturing preflight passes or the user explicitly accepts warnings.
- Diagnostics state that OSH Park upload handoff cannot currently fetch previews/warnings or attach the project to the user's account through the upload path.
- Tests cover valid package, missing drill file, missing outline, layer mismatch, and warning-accepted package.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Fabrication/OshPark/**`, `tests/DragonCAD.Fabrication.Tests/OshPark/**`.
- Do not call OSH Park live services in tests.
- Do not implement Gerber generation here unless the fabrication exporter story already exists.
- Keep this as package validation and handoff generation.

**Agent Boundary:** OSH Park prototype handoff only.

---

## MKT-013 - PCBCart Production Handoff

**As a** product builder, **I want** DragonCAD to prepare a PCBCart production quote/order handoff, **so that** production boards and assembly can be quoted from complete manufacturing data.

**AC:**
- Handoff package includes Gerbers, drill files, pick-and-place, BOM, board stackup summary, finish, quantity, assembly side, and notes.
- Provider capability metadata represents PCBCart as quote/order handoff unless a formal API is configured.
- User-visible diagnostics identify missing assembly artifacts, missing manufacturer part numbers, and missing placement data.
- Tests cover bare-board quote package, assembly quote package, missing BOM, missing pick-and-place, and missing board stackup.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Fabrication/PcbCart/**`, `tests/DragonCAD.Fabrication.Tests/PcbCart/**`.
- Do not submit production orders automatically.
- Do not scrape account pages.
- Keep quote package artifacts deterministic and reviewable.

**Agent Boundary:** PCBCart production handoff only.

---

## MKT-014 - Legal And Terms Guardrails

**As a** project maintainer, **I want** marketplace integrations to expose licensing and terms constraints, **so that** DragonCAD does not silently misuse catalog data, datasheets, or open hardware assets.

**AC:**
- Provider metadata includes allowed source modes, attribution requirements, redistribution flags, cache limits, and blocked automation modes.
- Imported open hardware assets preserve source repository, license text/link, and attribution notes where available.
- Datasheet-derived components preserve datasheet source and review warnings.
- Tests cover attribution-required source, no-redistribution source, API-cache-limited source, and blocked scraping source.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Compliance/**`, `tests/DragonCAD.Sourcing.Tests/Compliance/**`.
- Do not provide legal advice in product text.
- Do not bypass vendor API terms with scraping.
- Do not remove source provenance during dedupe.

**Agent Boundary:** Marketplace compliance metadata and validation only.

---

## MKT-015 - Marketplace Story Documentation And Status

**As a** technical lead, **I want** the marketplace epic documented with status and sequencing, **so that** six-agent implementation waves can run without overlap.

**AC:**
- The marketplace epic lists stories, implementation order, boundaries, and API reality assumptions.
- The editor interaction backlog links to marketplace/library work where editor placement depends on library maturity.
- Docs are readable as markdown and avoid claiming unsupported live ordering/API behavior.

**Implementation Dev Notes:**
- Owned paths: `docs/marketplace-library-epic.md`, `docs/editor-interaction-backlog.md`.
- Docs-only; no source or tests.
- Validate by reading the changed markdown files.

**Agent Boundary:** Documentation only.

---

## Wave 7 Plan And Status

Wave 7 turns the marketplace/library foundation into coordinated service and shell-facing slices. These stories are intentionally scoped so six agents can run in parallel without touching the same paths.

| Story | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-016 - Canonical Merge Service | Planned | Reviewable merge suggestions combine duplicate vendor/imported parts into one canonical component without overwriting verified geometry. | `src/DragonCAD.Core/Components/Marketplace/**`, `tests/DragonCAD.Core.Tests/Components/Marketplace/**` |
| MKT-017 - Vendor Request Plan And Credential Boundaries | Planned | Vendor operations produce request plans and credential diagnostics without live secrets or network calls in tests. | `src/DragonCAD.Sourcing/Catalog/**`, `src/DragonCAD.Sourcing/Credentials/**`, matching sourcing tests |
| MKT-018 - BOM Order Planning Workspace Model | Planned | BOM lines become build-quantity order plans with alternates, stock, MOQ, price breaks, and review status. | `src/DragonCAD.Sourcing/BomPlanning/**`, `tests/DragonCAD.Sourcing.Tests/BomPlanning/**` |
| MKT-019 - Fabrication Handoff UI View Models | Planned | OSH Park prototype and PCBCart production packages are exposed through reviewable app view models. | `src/DragonCAD.App/Fabrication/**`, `tests/DragonCAD.App.Tests/Fabrication/**` |
| MKT-020 - Datasheet Review Queue UI View Models | Planned | Datasheet-generated component drafts appear in a review queue before promotion to trusted libraries. | `src/DragonCAD.App/Marketplace/**`, `tests/DragonCAD.App.Tests/Marketplace/**` |
| MKT-021 - Marketplace Shell Integration | Planned | The shell exposes Marketplace, BOM, datasheet review, and fabrication handoff panels from one visible workflow. | `src/DragonCAD.App/Shell/**`, focused app shell tests |

### Wave 7 Coordination Notes

- Run `MKT-021` after `MKT-018`, `MKT-019`, and `MKT-020` expose stable view-model state.
- `MKT-016` and `MKT-017` are backend-only and must not edit Avalonia shell files.
- `MKT-019` may consume fabrication provider descriptors and quote packages, but must not change Gerber, BOM, or pick-and-place formatting.
- `MKT-020` may consume datasheet generation records, but must not call Codex, Ollama, or vendor APIs directly.
- Intentionally deferred from Wave 7: live credentials, live carts, live order placement, live vendor checkout automation, and automated component approval.

---

## MKT-016 - Canonical Merge Service

**As a** component librarian, **I want** reviewable canonical merge suggestions, **so that** duplicate vendor, imported, and datasheet-generated parts can settle on one trusted component without losing source provenance.

**AC:**
- Merge service accepts canonical components, vendor offers, imported library components, and datasheet draft candidates.
- Suggestions classify exact duplicate, same electrical part, value override, package variant, footprint conflict, pinout conflict, and unsafe merge.
- Verified symbol, footprint, pinout, package, and 3D geometry are never overwritten automatically.
- Merge output preserves all vendor offers, datasheet links, source IDs, attribution, and review warnings.
- Tests cover 7805 multi-vendor offers, NE555 aliases, ESP32 devkit board variants, resistor/capacitor value overrides, package conflicts, and pinout conflicts.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Components/Marketplace/**`, `tests/DragonCAD.Core.Tests/Components/Marketplace/**`.
- Build on canonical marketplace component records instead of changing schematic, board, or library editor behavior.
- Do not edit `src/DragonCAD.Core/Contracts/**` without a separate coordination story.
- Do not call vendor APIs, AI providers, or file importers.
- Validate with `dotnet test tests\DragonCAD.Core.Tests\DragonCAD.Core.Tests.csproj --filter Marketplace`.

**Agent Boundary:** Core marketplace merge logic only.

---

## MKT-017 - Vendor Request Plan And Credential Boundaries

**As a** sourcing user, **I want** DragonCAD to plan vendor catalog requests without exposing credentials, **so that** Digi-Key, Mouser, Adafruit, SparkFun, and Jameco integrations can be wired safely later.

**AC:**
- Request planner emits provider-specific request plans for product search, product detail, price/stock refresh, datasheet lookup, and catalog import.
- Plans include provider ID, request purpose, required credential scope, rate-limit category, cache policy, and whether live network access is required.
- Missing credential diagnostics are provider-specific and redact all secret values.
- Provider capabilities represent API, open-hardware repository, manual feed, upload handoff, quote handoff, and scrape-restricted modes.
- Tests cover Digi-Key API request planning, Mouser API request planning, Adafruit product lookup, SparkFun repository/library source planning, Jameco manual/feed fallback, and redacted diagnostics.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Catalog/**`, `src/DragonCAD.Sourcing/Credentials/**`, `tests/DragonCAD.Sourcing.Tests/Catalog/**`, `tests/DragonCAD.Sourcing.Tests/Credentials/**`.
- Do not implement live HTTP clients in this story.
- Do not store API keys, OAuth tokens, cookies, payment data, or account IDs in project files.
- Do not scrape vendor pages unless an explicit provider capability and later implementation story allows it.
- Validate with `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter "Catalog|Credential"`.

**Agent Boundary:** Sourcing request planning and credential redaction only.

---

## MKT-018 - BOM Order Planning Workspace Model

**As a** project owner, **I want** a reviewable BOM order plan, **so that** I can choose vendors, quantities, alternates, and build costs before purchasing parts.

**AC:**
- Order planner groups project BOM entries by canonical component, selected value, package, and do-not-substitute policy.
- Planner calculates prototype and production quantities using build count, attrition/spares, MOQ, order multiples, price breaks, stock, lifecycle, and lead time.
- Alternates are ranked by preferred vendor, availability, lifecycle, unit cost, and compatibility warnings.
- Output includes per-vendor subtotals, total projected cost, missing-offer diagnostics, stale quote diagnostics, and review status.
- Tests cover one-board, ten-board, one-hundred-board, MOQ expansion, price break selection, out-of-stock fallback, do-not-substitute rejection, and stale quote warnings.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/BomPlanning/**`, `tests/DragonCAD.Sourcing.Tests/BomPlanning/**`.
- Consume canonical component/vendor offer records and normalized catalog listings.
- Do not mutate schematic, board, or component library state while planning orders.
- Do not submit carts or orders.
- Validate with `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter BomPlanning`.

**Agent Boundary:** BOM order planning calculations only.

---

## MKT-019 - Fabrication Handoff UI View Models

**As a** hardware developer, **I want** prototype and production fabrication handoffs visible in the app, **so that** OSH Park and PCBCart packages can be reviewed before leaving DragonCAD.

**AC:**
- View models expose OSH Park prototype package status, required files, diagnostics, accepted warnings, and handoff action text.
- View models expose PCBCart production/assembly package status, required Gerbers, drill files, BOM, pick-and-place, stackup summary, quantity, and diagnostics.
- Handoff action remains disabled until required artifacts exist or warnings are explicitly accepted.
- User-visible text clearly distinguishes package handoff from live order placement.
- Tests cover valid prototype package, missing prototype artifacts, accepted warning, valid production package, missing assembly artifacts, and disabled handoff state.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Fabrication/**`, `tests/DragonCAD.App.Tests/Fabrication/**`.
- Use existing fabrication provider descriptors and quote package validators.
- Do not implement Gerber generation, live upload, checkout automation, or account login.
- Do not edit shell XAML unless coordinating with `MKT-021`.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Fabrication`.

**Agent Boundary:** Fabrication handoff app view models only.

---

## MKT-020 - Datasheet Review Queue UI View Models

**As a** library maintainer, **I want** datasheet-generated component drafts in a review queue, **so that** AI-assisted symbols, footprints, and 3D proposals are checked before becoming trusted components.

**AC:**
- Queue lists draft components with source datasheet, manufacturer part number, package, confidence, warnings, and generated asset summaries.
- Selecting a draft exposes symbol proposal, footprint proposal, 3D model proposal, extracted pins, package dimensions, and unsupported-feature notes.
- Actions support promote-to-new, link-to-existing, reject, and needs-more-data states as view-model commands.
- Automated approval is unavailable; every generated component stays review-required until a user action records the decision.
- Tests cover queue filtering, selection details, warning display, promote command state, link command state, reject command state, and no-auto-approval behavior.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Marketplace/**`, `tests/DragonCAD.App.Tests/Marketplace/**`.
- Consume deterministic datasheet generation models and library review records.
- Do not call Codex, Ollama, web search, vendor APIs, or file importers from the UI view model.
- Do not promote directly into permanent storage until a core review service story owns that behavior.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Marketplace`.

**Agent Boundary:** Datasheet review queue app view models only.

---

## MKT-021 - Marketplace Shell Integration

**As a** DragonCAD user, **I want** marketplace, BOM, datasheet review, and fabrication handoff workflows available from the main shell, **so that** component discovery through ordering feels like one integrated hardware IDE workflow.

**AC:**
- Shell exposes Marketplace, BOM Planner, Datasheet Review, Prototype Handoff, and Production Handoff panels or tabs.
- Marketplace tab can show provider filters for SparkFun, Adafruit, Digi-Key, Mouser, and Jameco using offline view-model data.
- BOM Planner tab can display order-plan summaries and disabled/enabled handoff actions from view models.
- Fabrication tabs clearly label OSH Park as prototype handoff and PCBCart as production handoff.
- Shell text and command states do not claim live carts, live credentials, live ordering, or automated component approval.
- Tests cover tab/panel creation, command binding, offline empty state, populated marketplace state, BOM summary binding, and fabrication handoff binding.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `tests/DragonCAD.App.Tests/Shell/**`.
- Coordinate after `MKT-018`, `MKT-019`, and `MKT-020` expose stable view-model APIs.
- Do not implement vendor HTTP clients, credential storage, live carts, live order placement, or AI-provider calls.
- Keep shell integration thin; business rules stay in sourcing, fabrication, marketplace, and review view models.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Shell` and a launched-app screenshot.

**Agent Boundary:** Shell wiring only.

---

## Wave 8 Plan And Status

Wave 8 moves the marketplace foundation toward reviewable user actions without crossing into live checkout, live order placement, or unreviewed AI-generated part approval. These stories are scoped for parallel agents and should remain offline-first until provider credentials, legal review, and explicit live-integration stories exist.

| Story | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-022 - Marketplace Cart Workspace Model | Planned | Vendor cart handoff rows group BOM order lines into reviewable carts with stale-price, missing-offer, and unavailable-stock diagnostics. | `src/DragonCAD.Sourcing/Orders/**`, `tests/DragonCAD.Sourcing.Tests/Orders/**` |
| MKT-023 - Datasheet Promotion Plan Service | Planned | Datasheet-generated symbol, footprint, and 3D proposals become reviewable promotion plans instead of trusted components. | `src/DragonCAD.Core/Libraries/Review/**`, `tests/DragonCAD.Core.Tests/Libraries/Review/**` |
| MKT-024 - Fabrication Handoff Action Planner | Planned | OSH Park and PCBCart handoff actions expose required files, warnings, and next-step instructions without submitting orders. | `src/DragonCAD.Fabrication/Ordering/**`, `tests/DragonCAD.Fabrication.Tests/Ordering/**` |
| MKT-025 - Vendor Sync Status Model | Planned | Catalog providers expose last sync, next refresh, capability, credential, and cache-status state for marketplace UI. | `src/DragonCAD.Sourcing/Sync/**`, `tests/DragonCAD.Sourcing.Tests/Sync/**` |
| MKT-026 - Component Provenance And Audit Trail | Planned | Canonical components preserve source, merge, review, promotion, and override history for every marketplace/library decision. | `src/DragonCAD.Core/Components/Provenance/**`, `tests/DragonCAD.Core.Tests/Components/Provenance/**` |
| MKT-027 - Marketplace Action Shell Integration | Planned | The app shell surfaces cart, sync, datasheet promotion, fabrication handoff, and provenance states from existing view models. | `src/DragonCAD.App/Shell/**`, `tests/DragonCAD.App.Tests/Shell/**` |

### Wave 8 Coordination Notes

- `MKT-027` should run after `MKT-022`, `MKT-024`, and `MKT-025` expose stable view-model or service outputs.
- `MKT-023` and `MKT-026` are core-library review/audit slices and must not edit Avalonia UI.
- `MKT-022` may consume BOM order plans and vendor offers, but must not submit carts, automate checkout, or store payment/shipping details.
- `MKT-024` may produce OSH Park and PCBCart action plans, but must not upload files, log in to vendor accounts, or place prototype/production orders.
- `MKT-025` must report provider state without making live network calls in tests.
- Explicitly deferred from Wave 8: live checkout, live order placement, browser checkout automation, payment/shipping storage, and promotion of AI-generated parts without human review.

---

## MKT-022 - Marketplace Cart Workspace Model

**As a** project buyer, **I want** vendor cart handoff rows generated from the BOM order plan, **so that** I can review quantities, pricing, stock, and vendor-specific ordering issues before leaving DragonCAD.

**AC:**
- Cart model groups order lines by vendor and includes vendor SKU, manufacturer part number, canonical component ID, requested quantity, purchasable quantity, unit price snapshot, extended price, stock state, and quote timestamp.
- Diagnostics identify missing offers, stale quote data, MOQ/order-multiple changes, insufficient stock, lifecycle risk, and unavailable direct-cart support.
- Providers can declare direct cart, CSV upload, copy/paste, or manual handoff modes.
- Output is deterministic and reviewable as JSON or structured text.
- Tests cover Digi-Key direct-capable handoff, Mouser CSV-capable handoff, Jameco manual handoff, stale quotes, insufficient stock, and missing vendor SKU.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Orders/**`, `tests/DragonCAD.Sourcing.Tests/Orders/**`.
- Consume BOM order planning outputs and normalized vendor offers.
- Do not submit live carts, automate browser checkout, place orders, or store payment/shipping data.
- Do not mutate schematic, board, library, or project files.
- Validate with `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter Orders`.

**Agent Boundary:** Sourcing cart handoff models only.

---

## MKT-023 - Datasheet Promotion Plan Service

**As a** component librarian, **I want** datasheet-generated component drafts converted into explicit promotion plans, **so that** AI-assisted symbols, footprints, and 3D models are reviewed before entering the trusted library.

**AC:**
- Promotion plan links a datasheet draft to a target canonical component or a proposed new component.
- Plan includes proposed symbol changes, footprint changes, 3D model proposal, pin mapping, package dimensions, metadata overrides, warnings, confidence, and required reviewer actions.
- Plans are blocked when critical warnings, missing pins, missing package dimensions, or pin-count mismatches exist.
- Approved plans record reviewer, timestamp, source datasheet, and exact promoted fields.
- Tests cover promote-new, link-existing, blocked-critical-warning, blocked-pin-mismatch, metadata-only override, and rejected draft.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Libraries/Review/**`, `tests/DragonCAD.Core.Tests/Libraries/Review/**`.
- Consume deterministic datasheet generation records; do not call Codex, Ollama, web search, vendor APIs, or importers.
- Do not automatically promote AI-generated or datasheet-generated parts.
- Do not overwrite verified symbol, footprint, pinout, or 3D geometry without an explicit approved plan.
- Validate with `dotnet test tests\DragonCAD.Core.Tests\DragonCAD.Core.Tests.csproj --filter Review`.

**Agent Boundary:** Core datasheet promotion planning only.

---

## MKT-024 - Fabrication Handoff Action Planner

**As a** hardware developer, **I want** OSH Park and PCBCart handoff actions planned from manufacturing packages, **so that** prototype and production ordering steps are clear without DragonCAD submitting orders automatically.

**AC:**
- Action planner emits provider name, handoff mode, required artifact checklist, package readiness, blocking diagnostics, warning diagnostics, and user-facing next action text.
- OSH Park action plans cover prototype Gerber/drill upload handoff, board outline, layer mapping, dimensions, and warning acceptance state.
- PCBCart action plans cover production bare-board quote and assembly quote handoff with Gerbers, drill files, BOM, pick-and-place, stackup, quantity, and assembly side.
- Disabled action state explains missing files or unaccepted warnings.
- Tests cover ready OSH Park package, missing OSH Park drill file, accepted OSH Park warning, ready PCBCart assembly package, missing PCBCart BOM, and disabled action text.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Fabrication/Ordering/**`, `tests/DragonCAD.Fabrication.Tests/Ordering/**`.
- Build on existing fabrication provider descriptors and quote package validation.
- Do not generate Gerbers, upload to vendors, log in to vendor accounts, scrape quote pages, or place prototype/production orders.
- Keep action output deterministic for shell binding.
- Validate with `dotnet test tests\DragonCAD.Fabrication.Tests\DragonCAD.Fabrication.Tests.csproj --filter Ordering`.

**Agent Boundary:** Fabrication handoff action planning only.

---

## MKT-025 - Vendor Sync Status Model

**As a** marketplace user, **I want** vendor catalog sync status visible and understandable, **so that** I know whether pricing, stock, datasheets, and catalog metadata are current.

**AC:**
- Sync status model includes provider, access mode, credential state, last successful sync, last attempted sync, next suggested refresh, cache age, rate-limit state, and last diagnostic.
- Status distinguishes online API providers, open-hardware repository sources, manual/feed providers, and scrape-restricted providers.
- Stale, blocked, missing-credential, and offline states produce clear user-facing summaries.
- Tests cover Digi-Key API credential missing, Mouser stale cache, SparkFun repository source ready, Jameco manual feed stale, and scrape-restricted blocked state.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Sync/**`, `tests/DragonCAD.Sourcing.Tests/Sync/**`.
- Consume provider descriptors, request plans, and capability metadata.
- Do not run live network syncs in tests.
- Do not store API secrets in sync status records.
- Validate with `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter Sync`.

**Agent Boundary:** Sourcing vendor sync status only.

---

## MKT-026 - Component Provenance And Audit Trail

**As a** component librarian, **I want** every component source, merge, override, and promotion recorded, **so that** marketplace library decisions are traceable and reversible.

**AC:**
- Audit trail records source import, vendor offer link, datasheet draft creation, canonical merge suggestion, accepted merge, rejected merge, value override, package override, promotion, rejection, and manual edit events.
- Events include actor, timestamp, source system, source ID/URL when available, affected fields, previous value, new value, confidence, and warnings.
- Audit entries serialize deterministically and never store vendor credentials or payment/shipping data.
- Tests cover imported SparkFun component provenance, Digi-Key offer link, datasheet promotion approval, rejected AI draft, manual value override, and deterministic event ordering.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Core/Components/Provenance/**`, `tests/DragonCAD.Core.Tests/Components/Provenance/**`.
- Keep audit records independent of Avalonia UI and vendor HTTP clients.
- Do not change core contracts without a separate coordination story.
- Do not delete source provenance during canonical dedupe.
- Validate with `dotnet test tests\DragonCAD.Core.Tests\DragonCAD.Core.Tests.csproj --filter Provenance`.

**Agent Boundary:** Core component provenance/audit model only.

---

## MKT-027 - Marketplace Action Shell Integration

**As a** DragonCAD user, **I want** marketplace actions surfaced in the main shell, **so that** catalog sync, BOM carts, datasheet promotions, fabrication handoffs, and provenance can be reviewed from one hardware IDE workspace.

**AC:**
- Shell exposes visible panels or tabs for vendor sync status, BOM carts, datasheet promotion review, fabrication handoff actions, and component provenance.
- UI binds to offline/sample view-model data and clearly labels unavailable live actions.
- Cart and fabrication action buttons stay disabled when the underlying action plan is blocked.
- Datasheet promotion actions state that generated parts require human review.
- Tests cover tab/panel creation, empty state, populated cart state, blocked fabrication action state, sync status binding, datasheet promotion blocked state, and provenance summary binding.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `tests/DragonCAD.App.Tests/Shell/**`.
- Coordinate after `MKT-022`, `MKT-024`, and `MKT-025` expose stable service or view-model outputs.
- Do not implement live credentials, live vendor sync, live carts, live checkout, order placement, vendor login, or AI-provider calls.
- Keep shell logic thin and delegate business rules to sourcing, fabrication, review, and provenance models.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Shell` and a launched-app screenshot if UI changes are included.

**Agent Boundary:** Marketplace action shell wiring only.

---

## Wave 9 Plan And Status

Wave 9 turns marketplace review models into explicit offline commands that the shell can bind to without performing live network calls, checkout automation, order placement, or unreviewed library mutation. These stories are intended for parallel execution after Wave 8 view models and planners stabilize.

| Story | Status | Primary Outcome | Agent Boundary |
| --- | --- | --- | --- |
| MKT-028 - Cart Quantity Commands | Planned | Marketplace cart rows expose add, remove, set quantity, and clear commands with deterministic totals and diagnostics. | `src/DragonCAD.App/Marketplace/Cart/**`, `tests/DragonCAD.App.Tests/Marketplace/Cart/**` |
| MKT-029 - Vendor Sync Run Planner | Planned | Vendor sync requests become reviewable run plans with credential, rate-limit, freshness, and manual-feed warnings. | `src/DragonCAD.Sourcing/Sync/**`, `tests/DragonCAD.Sourcing.Tests/Sync/**` |
| MKT-030 - Datasheet Promotion Commands | Planned | Datasheet promotion review exposes approve, reject, link-existing, and request-more-data commands without mutating the trusted library. | `src/DragonCAD.App/Datasheets/Promotion/**`, `tests/DragonCAD.App.Tests/Datasheets/Promotion/**` |
| MKT-031 - Fabrication Readiness Commands | Planned | OSH Park and PCBCart handoff panels expose readiness, warning acceptance, export package, and open-handoff commands safely. | `src/DragonCAD.App/Fabrication/Handoff/**`, `tests/DragonCAD.App.Tests/Fabrication/Handoff/**` |
| MKT-032 - Component Audit Timeline UI Model | Planned | Component provenance events render as a filterable timeline grouped by source, actor, date, and affected field. | `src/DragonCAD.App/Marketplace/Audit/**`, `tests/DragonCAD.App.Tests/Marketplace/Audit/**` |
| MKT-033 - Marketplace Command Shell Integration | Planned | The shell wires cart, sync, promotion, fabrication readiness, and audit commands into visible workspace panels. | `src/DragonCAD.App/Shell/**`, `tests/DragonCAD.App.Tests/Shell/**` |

### Wave 9 Coordination Notes

- `MKT-033` should run after `MKT-028`, `MKT-030`, `MKT-031`, and `MKT-032` expose stable command/view-model APIs.
- `MKT-029` stays in sourcing and must not edit Avalonia shell or app command surfaces.
- Command stories may update in-memory/sample state for UI feedback, but must not submit vendor carts, place orders, upload manufacturing files, open checkout automation, or store payment/shipping data.
- Datasheet promotion commands must not write directly into the permanent library; they produce review decisions or promotion requests for a later persistence story.
- Vendor sync run planning must not execute HTTP calls in tests and must not serialize API secrets.
- Explicitly deferred from Wave 9: live vendor HTTP sync, live carts, order placement, automatic library mutation, credential storage, browser checkout automation, and automated approval of AI-generated components.

---

## MKT-028 - Cart Quantity Commands

**As a** project buyer, **I want** cart quantity commands for marketplace BOM rows, **so that** I can tune build quantities and vendor line items before creating external orders.

**AC:**
- Cart row commands support increment quantity, decrement quantity, set quantity, remove line, and clear vendor cart.
- Quantity changes recalculate line totals, vendor totals, grand total, stock diagnostics, MOQ diagnostics, and order-multiple diagnostics.
- Commands reject zero, negative, non-numeric, unavailable, and over-stock quantities with deterministic user-visible diagnostics.
- Duplicate canonical/vendor rows remain merged after quantity changes.
- Tests cover increment, decrement, set quantity, remove, clear vendor cart, MOQ rounding, insufficient stock, unavailable row, and deterministic total updates.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Marketplace/Cart/**`, `tests/DragonCAD.App.Tests/Marketplace/Cart/**`.
- Consume existing marketplace cart and BOM order planning view models.
- Keep behavior offline and deterministic; do not submit carts, call vendor APIs, automate checkout, or store payment/shipping data.
- Do not edit sourcing planner internals unless a separate coordination story owns that change.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter MarketplaceCart`.

**Agent Boundary:** Marketplace cart command view models only.

---

## MKT-029 - Vendor Sync Run Planner

**As a** marketplace maintainer, **I want** vendor catalog sync runs planned before execution, **so that** credentials, rate limits, freshness, and manual-feed requirements are visible and reviewable.

**AC:**
- Planner emits provider, access mode, requested operation, credential requirements, missing credential diagnostics, rate-limit state, cache freshness, manual-feed instructions, and expected catalog fields.
- Digi-Key and Mouser API plans require credentials and expose rate-limit metadata without revealing secrets.
- SparkFun and Adafruit plans distinguish repository/API-style sources from manual import sources.
- Jameco plans support manual/feed fallback and scrape-restricted warnings.
- Tests cover ready API provider, missing credentials, stale cache refresh, manual feed required, scrape-restricted blocked provider, and log-safe output.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.Sourcing/Sync/**`, `tests/DragonCAD.Sourcing.Tests/Sync/**`.
- Consume provider profiles, request plans, sync status, and capability metadata.
- Do not execute HTTP requests, scrape websites, store credentials, or write catalog cache files.
- Do not edit Avalonia UI or shell bindings in this story.
- Validate with `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter Sync`.

**Agent Boundary:** Sourcing sync run planning only.

---

## MKT-030 - Datasheet Promotion Commands

**As a** component librarian, **I want** explicit commands for datasheet-generated component decisions, **so that** generated symbols, footprints, and 3D proposals can be approved, rejected, linked, or sent back for more data without bypassing review.

**AC:**
- Commands support approve promotion plan, reject with reason, link to existing canonical component, request more data, and reset decision.
- Approve remains disabled when critical warnings, missing package dimensions, missing pins, pin-count mismatches, or missing reviewer identity exist.
- Command results include reviewer, timestamp supplied by caller, decision, affected draft ID, target component ID when linked, warnings, and immutable summary text.
- Commands do not mutate the trusted library; they emit deterministic decision records for later persistence.
- Tests cover approve-new, link-existing, reject, request-more-data, blocked-critical-warning, missing reviewer, and no-library-mutation behavior.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Datasheets/Promotion/**`, `tests/DragonCAD.App.Tests/Datasheets/Promotion/**`.
- Build on existing datasheet review queue and promotion planner models.
- Do not call Codex, Ollama, web search, vendor APIs, importers, or library storage.
- Do not overwrite verified symbol, footprint, pinout, or 3D geometry.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter DatasheetPromotion`.

**Agent Boundary:** Datasheet promotion command view models only.

---

## MKT-031 - Fabrication Readiness Commands

**As a** hardware developer, **I want** fabrication readiness commands for OSH Park and PCBCart packages, **so that** prototype and production handoffs are checked before I leave DragonCAD.

**AC:**
- Commands support validate readiness, accept warning, revoke warning acceptance, export package manifest, and open handoff action.
- OSH Park readiness checks Gerbers, drill files, outline, layer mapping, board dimensions, and warning acceptance.
- PCBCart readiness checks Gerbers, drill files, BOM, pick-and-place, stackup, quantity, assembly side, and warning acceptance.
- Open handoff command returns a safe action description and URL/action target but does not launch a browser or upload files in tests.
- Tests cover ready prototype, blocked prototype missing drill, accepted warning, ready production assembly, blocked production missing BOM, export manifest, and open-handoff disabled state.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Fabrication/Handoff/**`, `tests/DragonCAD.App.Tests/Fabrication/Handoff/**`.
- Consume existing fabrication provider descriptors, package validators, and handoff action planner.
- Do not generate Gerbers, upload manufacturing files, open a browser/process, log in to vendors, scrape quote pages, or place orders.
- Keep command output deterministic and shell-bindable.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter FabricationHandoff`.

**Agent Boundary:** Fabrication handoff command view models only.

---

## MKT-032 - Component Audit Timeline UI Model

**As a** component librarian, **I want** component provenance shown as a filterable audit timeline, **so that** vendor imports, datasheet decisions, merges, overrides, and manual edits are explainable.

**AC:**
- Timeline rows include event type, actor, timestamp, source system, source ID or URL, affected fields, previous value, new value, confidence, warnings, and summary text.
- Filters support event type, source system, actor, warning presence, date range, and affected field.
- Timeline groups events deterministically by component and timestamp while preserving stable ordering for equal timestamps.
- Sensitive data such as API credentials, payment details, shipping details, and vendor tokens are never displayed.
- Tests cover imported vendor event, datasheet approval event, rejected draft event, canonical merge event, manual override event, warning filter, actor filter, and deterministic ordering.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Marketplace/Audit/**`, `tests/DragonCAD.App.Tests/Marketplace/Audit/**`.
- Consume core component provenance/audit records.
- Do not edit core provenance models unless a separate coordination story owns the contract change.
- Do not use audit timeline as a shortcut to approve generated parts or bypass canonical merge review.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Audit`.

**Agent Boundary:** Marketplace audit timeline view models only.

---

## MKT-033 - Marketplace Command Shell Integration

**As a** DragonCAD user, **I want** marketplace action commands wired into the main shell, **so that** cart quantities, vendor sync planning, datasheet promotion, fabrication readiness, and component audit history are available from one workspace.

**AC:**
- Shell exposes visible command surfaces for marketplace cart quantities, vendor sync run planning, datasheet promotion decisions, fabrication readiness actions, and component audit timeline filters.
- Disabled command states and diagnostics are visible for blocked carts, missing credentials, blocked datasheet promotions, missing fabrication artifacts, and unavailable audit data.
- Shell text clearly labels offline review actions and does not claim live sync, checkout, upload, ordering, or automatic library promotion.
- Tests cover tab/panel creation, command binding, disabled blocked cart command, missing credential sync plan, blocked datasheet promotion command, blocked fabrication readiness command, audit timeline binding, and offline empty states.
- A launched-app screenshot captures at least one changed marketplace action panel.

**Implementation Dev Notes:**
- Owned paths: `src/DragonCAD.App/Shell/**`, `tests/DragonCAD.App.Tests/Shell/**`.
- Coordinate after `MKT-028`, `MKT-030`, `MKT-031`, and `MKT-032` expose stable APIs.
- Keep shell logic thin; command behavior belongs to marketplace cart, sourcing sync, datasheet promotion, fabrication handoff, and audit view models.
- Do not implement live credentials, live vendor sync, live carts, live checkout, order placement, vendor login, upload automation, or AI-provider calls.
- Validate with `dotnet test tests\DragonCAD.App.Tests\DragonCAD.App.Tests.csproj --filter Shell` and a launched-app screenshot.

**Agent Boundary:** Marketplace command shell wiring only.

---

## Wave 10 Completion Notes

Completed implementation slices:
- `MKT-034` Core promotion candidate model in `src/DragonCAD.Core/Components/Promotion/**`.
- `MKT-035` Marketplace BOM export preview in `src/DragonCAD.App/Marketplace/Cart/Export/**`.
- `MKT-036` Fabrication checklist export preview in `src/DragonCAD.App/Fabrication/Export/**`.
- `MKT-037` Marketplace saved filter presets in `src/DragonCAD.App/Marketplace/Filters/**`.
- `MKT-038` Marketplace quality/risk badges in `src/DragonCAD.App/Marketplace/Quality/**`.

Deferred by design:
- No real file writing for BOM/checklist exports.
- No live vendor sync, checkout, upload, or ordering.
- No automatic trusted-library mutation from generated datasheet assets.
- No credential storage or vendor-token display.

---

## Wave 11/12 Completion Notes

Completed implementation slices:
- Marketplace shell wiring now exposes the marketplace BOM export preview, quality badges, saved filter presets, fabrication checklist preview, order plan provider actions, and CSV BOM preview lines from the main workspace.
- Quality/risk badge output is visible in the marketplace surface so catalog rows can communicate review state, lifecycle/stock concerns, and sourcing readiness without claiming live vendor ordering.
- Saved filter presets are available in the marketplace shell so common library and sourcing views can be recalled without mutating catalog data.
- Fabrication checklist preview is surfaced as a reviewable handoff artifact before any OSH Park or PCBCart workflow leaves DragonCAD.
- Order plan provider actions are visible as disabled or review-only actions based on provider capability and missing-artifact diagnostics.
- CSV BOM preview lines are rendered for inspection before any future file export writes to disk.

Verification evidence:
- Full app verification passed with 244 app tests.
- Screenshot captured: `C:\code\HawkCAD\.tmp\screenshots\wave11-marketplace-shell-window.png`.
- Screenshot captured: `C:\code\HawkCAD\.tmp\screenshots\wave12-marketplace-order-plan-window.png`.

Deferred by design:
- No live vendor sync, checkout, upload, order placement, browser automation, or credential storage.
- No automatic trusted-library promotion from imported, generated, or datasheet-derived component candidates.
- No real BOM/order/checklist export writes beyond reviewable preview data.

## Next Planned Slices

- Order export: turn previewed BOM/order plan data into deterministic export artifacts while keeping ordering manual and review-first.
- Datasheet ingestion: add the offline ingestion pipeline for datasheet-backed draft components with confidence, warnings, and review-required status.
- Vendor catalog sync credentials: wire credential availability and redacted diagnostics into catalog sync planning without storing secrets in projects.
- Library promotion: persist reviewed component promotion decisions into the trusted library with provenance and rollback-friendly audit records.

---

## Wave 14 Completion Notes

Completed implementation slices:
- Component library and marketplace are now represented as one unified source surface in the workspace.
- The unified source rows combine built-in HawkCAD library components with vendor marketplace rows and expose a combined source summary.
- The marketplace shell now has an in-app order draft command that snapshots BOM cart lines into provider-specific draft orders for review inside DragonCAD.
- BOM CSV preparation now materializes deterministic CSV text and a prepared line count for the shell.

Verification evidence:
- Full app verification passed with 248 app tests.
- Screenshot captured: `C:\code\HawkCAD\.tmp\screenshots\wave14-unified-library-marketplace-order-draft-window.png`.

Deferred by design:
- Order drafts are local review artifacts only; no live payment, vendor login, vendor cart mutation, or purchase placement is performed.
- Vendor checkout APIs, credential storage, shipping profiles, and tax/freight calculation still require explicit provider-specific stories.

---

## Wave 15 Completion Notes

Completed implementation slices:
- In-app checkout readiness now validates local order drafts before any future provider order placement.
- Checkout readiness blocks order placement until a shipping profile, payment method, and provider credentials exist.
- The Marketplace shell surfaces readiness status, primary action text, and blocker messages instead of showing a fake live purchase button.
- Checkout setup commands now let the local shell mark shipping profile, payment method, and provider credential availability for draft-order readiness testing.

Verification evidence:
- Full app verification passed with 252 app tests.

Deferred by design:
- No provider credentials are stored yet.
- No shipping, payment, tax, freight, or live vendor API calls are performed.
- The future live order-placement command must remain blocked until provider-specific credential and terms-of-service stories are implemented.

---

## Wave 16 Completion Notes

Completed implementation slices:
- Guarded local order-record creation is now available after checkout readiness is green.
- The order record snapshots draft id, provider orders, totals, and provider submission status while explicitly stating that no live vendor order was placed.
- The shell exposes a `Create Local Order Record` action and local order summary fields.
- Blocked checkout readiness prevents local order-record creation and reports the setup blocker count.

Verification evidence:
- Full app verification passed with 256 app tests.

Deferred by design:
- Local order records are not vendor submissions.
- No live vendor API calls, payment captures, shipping labels, cart mutation, or purchase confirmation numbers are produced.

---

## Wave 17 Completion Notes

Completed implementation slices:
- Local marketplace order records now remain available through a visible order history collection instead of only the active order summary.
- The marketplace inspector is scrollable so checkout controls, readiness blockers, order history, quality badges, saved filters, BOM cart, and vendor sync details remain reachable in the docked shell.
- The shell has regression coverage for the order-history binding and scrollable inspector, plus view-model coverage for retained local records.
- Order history now exposes a deterministic local-record count in the inspector so review-only checkout activity is visible without implying live vendor submission.

Verification evidence:
- Focused app tests passed with 2 focused shell/order-history tests.

Deferred by design:
- Order history still records local review artifacts only.
- Live vendor purchase history, provider confirmation numbers, credential storage, and order-status polling remain future provider-specific stories.

---

## Wave 18 Completion Notes

Completed implementation slices:
- The marketplace BOM cart now has shell commands for incrementing, decrementing, and removing cart lines through the existing cart command service.
- The Marketplace shell exposes per-line cart controls instead of a read-only BOM preview list.
- Sourcing now has a local vendor checkout planner with review-only order mode, provider readiness, and blocker modeling.
- Sourcing now has vendor catalog match scoring for exact, duplicate, weak, and no-match dedupe decisions.
- Component intelligence now has a datasheet-to-existing-component link planner for link, duplicate, and needs-new-component decisions without AI or network calls.
- Fabrication now has a manual-review-only order packet model for prototype and production provider review.
- Core now has deterministic marketplace component audit event formatting for vendor import, datasheet-generated, manual override, and local order record events.
- A dedicated component marketplace roadmap now captures the remaining vertical stories, AC, implementation notes, and agent boundaries.

Verification evidence:
- Focused shell cart-control tests passed with 3 focused app tests.
- Worker focused suites passed for sourcing, component intelligence, fabrication, and core slices.

Deferred by design:
- No live vendor checkout, provider API submission, payment capture, manufacturing upload, AI generation call, or automatic trusted-library mutation was added.

---

## Wave 19 Completion Notes

Completed implementation slices:
- The Marketplace shell now exposes the read-only marketplace audit timeline so provenance is visible in the main workspace.
- A compact audit summary is visible near the top of the Marketplace inspector so provenance state appears without scrolling.
- Audit rows show source type, review state, component key, and reviewer note.
- The audit panel includes source and review-state filters bound to the existing audit timeline view model.
- Seeded audit records include vendor import, datasheet-generated pending review, and approved manual override examples.

Verification evidence:
- Focused app shell tests passed with 2 focused marketplace audit tests.

Deferred by design:
- Audit timeline is read-only.
- No audit entry edits, trusted-library mutation, live vendor calls, AI generation, or automatic approval was added.

---

## Wave 20 Completion Notes

Completed implementation slices:
- The Datasheets workspace now surfaces datasheet-to-component link review plans instead of keeping link decisions hidden in backend tests.
- The app layer reuses the Component Intelligence link planner to show whether a datasheet should link to an existing component, needs a new component, or requires duplicate review.
- Link rows expose component name, manufacturer part number, target component id, match basis, and review warnings.
- Seeded review data covers an LM7805 datasheet linking to the trusted `dragon:lm7805` component and an ESP32 module datasheet requiring review before it becomes trusted library content.

Verification evidence:
- Focused app tests passed with 2 focused datasheet link review tests.

Deferred by design:
- Link review remains read-only.
- No automatic trusted-library promotion, AI generation call, network datasheet fetch, or permanent library mutation was added.

---

## Wave 21 Completion Notes

Completed implementation slices:
- Datasheet link review rows now have local approve and reject actions in the Datasheets workspace.
- Each link row exposes review state and review notes so approved and rejected decisions are visible before trusted-library promotion.
- The approve/reject commands are intentionally local state changes; they do not mutate the permanent component library.

Verification evidence:
- Focused app tests passed with 2 focused datasheet link action tests.

Deferred by design:
- Approved link rows are not yet persisted as trusted-library promotion records.
- No vendor sync, AI generation, network datasheet lookup, or automatic library mutation was added.

---

## Wave 22 Completion Notes

Completed implementation slices:
- Approved datasheet link reviews now flow into a visible promotion queue in the Datasheets workspace.
- Rejected link reviews stay out of the promotion queue.
- Promotion queue rows show component name, target component id, decision, and readiness status.
- The promotion queue summary updates when link-review approval state changes.

Verification evidence:
- Focused app tests passed with 2 focused datasheet promotion queue tests.

Deferred by design:
- The promotion queue is still an in-memory review surface.
- No permanent trusted-library write, vendor sync, AI generation, network datasheet lookup, or automatic asset promotion was added.

---

## Wave 23 Completion Notes

Completed implementation slices:
- The Datasheets workspace now has a local `Create Promotion Record` action for approved link-review queue rows.
- Promotion records snapshot the approved queue into a local auditable record with an id, status, and row summary.
- Promotion record history is retained in shell state for the current session.
- The status bar reports the created promotion record id and explicitly states that the trusted-library write is still pending.

Verification evidence:
- Focused app tests passed with 2 focused datasheet promotion record tests.

Deferred by design:
- Promotion records are local review artifacts only.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 24 Completion Notes

Completed implementation slices:
- Local datasheet promotion records now expose a deterministic JSON export preview.
- Promotion records provide a stable export filename, line count, and preview body for later project artifact persistence.
- The preview explicitly marks `trustedLibraryWrite` as `pending`.
- The Datasheets workspace surfaces the export preview under Promotion Records.

Verification evidence:
- Focused app tests passed with 2 focused datasheet promotion export tests.

Deferred by design:
- The export preview is not written to disk yet.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 25 Completion Notes

Completed implementation slices:
- The Datasheets workspace now has an `Approve Safe Links` action.
- Safe batch approval only approves clean `Link Existing Component` rows with no review warnings and a real target component id.
- New-component or generated-component candidates remain pending for manual review.
- Approved safe links flow into the existing promotion queue.

Verification evidence:
- Focused app tests passed with 2 focused safe-link approval tests.

Deferred by design:
- Safe approval is still local review state only.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 26 Completion Notes

Completed implementation slices:
- The Datasheets workspace now has a `Stage Safe Links` action.
- Staging safe links approves clean existing-component links and immediately creates a local promotion record with deterministic JSON preview.
- Generated/new-component candidates remain pending for manual review.
- The staged record remains a local review artifact and explicitly does not write the trusted library.

Verification evidence:
- Focused app tests passed with 2 focused safe-link staging tests.

Deferred by design:
- Staged promotion records are not persisted to disk yet.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 27 Completion Notes

Completed implementation slices:
- Datasheet link rows now enter a `Staged for Promotion` state after a local promotion record snapshots them.
- Staged rows leave the pending promotion queue, preventing duplicate local promotion records for the same reviewed link.
- Re-running `Stage Safe Links` after all safe links are staged reports that no safe links are ready instead of creating another record.

Verification evidence:
- Focused app tests passed with 4 focused datasheet staging regression tests.

Deferred by design:
- Staged promotion records remain local review artifacts.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 28 Completion Notes

Completed implementation slices:
- Local datasheet promotion records now expose a trusted-library readiness checklist.
- The checklist makes the remaining blockers explicit: promotion JSON artifact is preview-only, trusted-library write is pending, and audit entry creation is pending.
- The Datasheets workspace shows the readiness status and checklist under Promotion Records.

Verification evidence:
- Focused app tests passed with 2 focused promotion readiness checklist tests.

Deferred by design:
- The readiness checklist is informational only.
- No trusted-library persistence, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 29 Completion Notes

Completed implementation slices:
- Promotion record previews can now be saved as deterministic local JSON artifacts.
- The shell exposes a `Save Preview Artifact` action and reports the saved artifact path.
- Tests inject a temp artifact directory to verify the write without touching user project files.

Verification evidence:
- Focused app tests passed with 2 focused promotion artifact save tests.

Deferred by design:
- Saving a preview artifact still does not mutate the trusted component library.
- No vendor sync, AI generation, network datasheet lookup, audit persistence, or automatic component asset mutation was added.

---

## Wave 30 Completion Notes

Completed implementation slices:
- Saving a datasheet promotion preview now also writes a deterministic manifest artifact.
- The manifest names the promotion JSON file, row count, pending trusted-library write, and pending audit entry.
- The Datasheets workspace displays the manifest file name and saved manifest path as part of the local review handoff.

Verification evidence:
- Focused app tests passed with 3 promotion artifact and shell binding tests.

Deferred by design:
- The manifest is still a local handoff artifact only.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 31 Completion Notes

Completed implementation slices:
- Saving a datasheet promotion preview now writes a deterministic audit artifact.
- The audit artifact records the local save event, promotion artifact, manifest artifact, reviewed row count, and explicitly states that no trusted-library mutation was performed.
- The Datasheets workspace displays the audit file name and saved audit path alongside the promotion JSON and manifest paths.

Verification evidence:
- Focused app tests passed with 3 audit/manifest/shell binding tests.

Deferred by design:
- Audit entries are file artifacts only and are not yet appended to a project timeline or trusted-library ledger.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 32 Completion Notes

Completed implementation slices:
- Added a one-click `Stage + Save Safe Links` command for the Datasheets workspace.
- The command approves clean existing-component links, stages them into a local promotion record, and writes the promotion JSON, manifest, and audit artifacts in one workflow.
- Generated/new-component candidates remain pending for human review and are not automatically promoted.

Verification evidence:
- Focused app tests passed with 2 stage-and-save shell workflow tests.

Deferred by design:
- The generated package is still a local review package only.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 33 Completion Notes

Completed implementation slices:
- Datasheet promotion manifests now include SHA-256 hashes for the promotion JSON and audit JSON artifacts.
- Hashes are generated from the exact deterministic JSON preview strings that are written to disk.
- This gives local promotion packages an integrity check before any future trusted-library write step.

Verification evidence:
- Focused app tests passed with 2 manifest hash regression tests.

Deferred by design:
- Hashes are recorded but not yet verified by a separate package validation command.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 34 Completion Notes

Completed implementation slices:
- Added a local `Validate Package` command for saved datasheet promotion packages.
- Validation checks that the promotion JSON, manifest, and audit artifacts exist.
- Validation compares the saved promotion and audit files against the SHA-256 hashes recorded in the manifest.
- Tampered package content is reported as invalid before any trusted-library write can be considered.

Verification evidence:
- Focused app tests passed with 3 package validation and shell binding tests.

Deferred by design:
- Validation still operates only on the current local package paths held by the shell.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 35 Completion Notes

Completed implementation slices:
- Added a local `Record Ledger Entry` command for validated datasheet promotion packages.
- The command is gated by successful package validation and refuses to write if the package has not been validated.
- Validated packages append a deterministic JSONL ledger entry with record id, artifact names, artifact hashes, validation status, and an explicit `trustedLibraryMutation` value of `not-performed`.
- The Datasheets workspace displays the saved ledger path.

Verification evidence:
- Focused app tests passed with 3 ledger command and shell binding tests.

Deferred by design:
- The ledger is a local audit artifact and does not mutate the trusted component library.
- No vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 36 Completion Notes

Completed implementation slices:
- Datasheet promotion ledger writes are now idempotent by promotion record id.
- Re-running `Record Ledger Entry` for the same validated package leaves the ledger with one entry and reports that the record already exists.
- The check is deterministic and scans the JSONL ledger for the exact record id before appending.

Verification evidence:
- Focused app tests passed with 2 ledger idempotency regression tests.

Deferred by design:
- Ledger validation still uses local artifact paths and does not yet load arbitrary ledger files from project history.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 37 Completion Notes

Completed implementation slices:
- Added a trusted-library gate status to the Datasheets workspace.
- The gate starts blocked until a promotion package is saved, validated, and recorded in the local ledger.
- Once those local prerequisites are satisfied, the gate reports that the package is ready for a future explicit trusted-library write implementation.

Verification evidence:
- Focused app tests passed with 2 gate-status and shell binding tests.

Deferred by design:
- The ready gate is informational only and does not perform the trusted-library write.
- No trusted-library mutation, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 38 Completion Notes

Completed implementation slices:
- Added a gated `Save Trusted-Library Plan` command to the Datasheets workspace.
- The command refuses to run until a promotion package has been saved, validated, and recorded in the local ledger.
- Once the gate is ready, DragonCAD writes a deterministic `trusted-library-write-plan-PROMO-0001.json` artifact that lists intended link operations and explicitly records `trustedLibraryMutation` as `not-performed`.
- The Datasheets workspace displays the saved trusted-library write plan path.

Verification evidence:
- Focused app tests passed with 3 trusted-library write plan and shell binding tests.

Deferred by design:
- The write plan is a non-mutating handoff artifact only.
- No trusted-library writer, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 39 Completion Notes

Completed implementation slices:
- Added a `Simulate Trusted-Library Write` command to the Datasheets workspace.
- The command requires a saved trusted-library write plan and refuses to run without that local handoff artifact.
- Once a plan exists, DragonCAD writes a deterministic `trusted-library-write-simulation-PROMO-0001.json` dry-run artifact.
- The dry-run artifact records the source plan, intended link operations, target component ids, and `mutationApplied: false`.

Verification evidence:
- Focused app tests passed with 3 trusted-library simulation and shell binding tests.

Deferred by design:
- The simulation is a dry-run diff artifact only.
- No trusted-library writer, vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 40 Completion Notes

Completed implementation slices:
- Added a `Stage Trusted-Library Candidate` command to the Datasheets workspace.
- The command requires a completed trusted-library write simulation and refuses to run before the dry-run artifact exists.
- Once simulation exists, DragonCAD writes a deterministic `trusted-library-candidate-PROMO-0001.json` staged review artifact.
- The candidate artifact records the source simulation, promoted component links, review-required status, and `trustedLibraryMutation: not-performed`.

Verification evidence:
- Focused app tests passed with 3 trusted-library candidate and shell binding tests.

Deferred by design:
- The candidate is a staged review artifact only and does not replace or mutate the shipped core library.
- No vendor sync, AI generation, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 41 Completion Notes

Completed implementation slices:
- Added a controlled Datasheet Intake queue for local PDF paths and HTTP/HTTPS datasheet URLs.
- Intake records source type, source identifier, submitted actor, submitted timestamp, optional manufacturer part number, vendor product id, package, source notes, and review-required state.
- Intake validation reports deterministic diagnostics for missing source identifiers, unsupported source types, missing local PDF files, and duplicate intake requests.
- The Datasheets workspace now exposes a Datasheet Intake panel and a sample intake command for local review without starting AI generation or mutating trusted library content.

Verification evidence:
- Focused app tests passed with 5 datasheet intake and shell binding tests.

Deferred by design:
- Intake items are queue records only and do not extract PDF facts, call AI providers, fetch remote URLs, or promote generated component assets.
- No vendor sync, trusted-library writer, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 42 Completion Notes

Completed implementation slices:
- Added a Datasheet Candidate Linking model for reviewable links between datasheet-derived candidates and canonical components, vendor catalog rows, imported candidates, or new-candidate placeholders.
- Link suggestions now show target type, match basis, confidence, package conflicts, review state, and no-mutation status.
- Accept/reject actions create auditable decision records with reviewer notes while explicitly leaving trusted library content unchanged.
- The Datasheets workspace now displays a Candidate Linking panel with link targets, match basis, conflicts, and review state.

Verification evidence:
- Focused app tests passed with 5 candidate-linking and shell binding tests.

Deferred by design:
- Candidate linking records context and review decisions only; it does not promote candidates or write trusted library files.
- No vendor sync, AI extraction, network datasheet lookup, or automatic component asset mutation was added.

---

## Wave 43 Completion Notes

Completed implementation slices:
- Added safe provider-ingestion foundations for Adafruit, SparkFun, and Jameco without live network calls.
- Adafruit catalog fixtures can be normalized into provider listings with pricing, stock, product URLs, datasheet URLs, provenance timestamps, and deterministic diagnostics.
- SparkFun source manifests can describe local/cache open-hardware repositories and validate missing repository/cache information, duplicate source ids, and stale retrieval timestamps.
- Jameco manual CSV feeds can be converted into catalog candidates while preserving the scrape-restricted policy boundary.
- Added secure provider credential planning for future Digi-Key and Mouser API clients, including required keys, storage expectations, missing-key diagnostics, readiness state, and redacted log-safe summaries.

Verification evidence:
- `dotnet build src\DragonCAD.Sourcing\DragonCAD.Sourcing.csproj -v:minimal` passed.
- Focused provider-ingestion tests passed with 19 tests in the shared sourcing test project.
- Full solution build and test verification passed after integration.

Deferred by design:
- No provider runner, scheduled sync, live HTTP client, scraping, vendor ordering, trusted-library mutation, or marketplace UI wiring was added in this wave.
- Provider rows remain catalog candidates until candidate linking, review, and trusted-library promotion gates explicitly accept them.

---

## Wave 44 Completion Notes

Completed implementation slices:
- Added a Digi-Key OAuth client for client-credentials token requests using the official token endpoint.
- Added a Digi-Key Product Information V4 keyword-search client that posts OAuth-authenticated requests, applies locale headers, and maps product results into normalized catalog listings.
- Added a Mouser Search API client for part-number and keyword searches using an API-key query parameter.
- Added environment-backed options for `DRAGONCAD_DIGIKEY_CLIENT_ID`, `DRAGONCAD_DIGIKEY_CLIENT_SECRET`, and `DRAGONCAD_MOUSER_API_KEY`.
- Updated provider credential planning so Digi-Key stores `client_id` and `client_secret` while OAuth access tokens remain runtime values.
- API failures and missing credentials return diagnostics without leaking client IDs, client secrets, API keys, or access tokens.

Verification evidence:
- Focused credentialed-client tests passed with 23 tests.
- Full solution build and test verification passed after integration.

Deferred by design:
- No UI sync trigger, persisted token cache, live-account smoke test, scheduled refresh, ordering workflow, or trusted-library promotion was added in this wave.
- Live API calls are now possible through the clients but remain opt-in through whatever provider runner/UI slice calls them.

---

## Wave 45 Completion Notes

Completed implementation slices:
- Added a provider-neutral catalog sync runner that executes configured search providers and returns normalized catalog candidates plus diagnostics.
- Added Digi-Key and Mouser search-provider adapters behind the common runner interface.
- Runner diagnostics now block blank queries and unavailable providers deterministically.
- Added app-side sync result rows for vendor SKU, manufacturer part number, manufacturer, stock/price, package, datasheet, product URL, and diagnostics.
- The Marketplace Vendor Sync panel now includes an API Sync Results section bound to `VendorCatalogSyncResult.ResultRows` and `VendorCatalogSyncResult.Diagnostics`.

Verification evidence:
- Focused sync runner and UI result binding tests passed with 5 tests.

Deferred by design:
- The UI shows seeded sample sync results until a live command is connected.
- No live API trigger button, persisted token cache, background schedule, marketplace promotion, vendor ordering, or trusted-library mutation was added in this wave.

---

## Wave 46 Completion Notes

Completed implementation slices:
- Added an app-level catalog sync search service that wraps the provider-neutral runner.
- Added environment-backed app service creation for Digi-Key and Mouser API sync.
- Added Marketplace Vendor Sync controls for provider selection, part/keyword entry, `Run API Sync`, and status output.
- Added view-model command handling that validates blank queries, calls the sync service, replaces API sync results, and reports completed or blocked status.

Verification evidence:
- Focused marketplace API sync command tests passed with 3 tests.

Deferred by design:
- No token cache, retry/backoff policy, live smoke test harness, scheduled background refresh, result promotion, BOM merge, or trusted-library mutation was added in this wave.

---

## Wave 47 Completion Notes

Completed implementation slices:
- Added an in-memory Digi-Key OAuth token cache behind the same token-source interface as the OAuth client.
- Cached tokens are reused until they are inside the configured expiry skew, defaulting to five minutes.
- Failed OAuth responses are not cached, so the next search can retry after credentials or connectivity are fixed.
- The app's environment-backed Digi-Key provider now uses the token cache before creating the product-search client.

Verification evidence:
- Focused Digi-Key OAuth/token/search tests passed with 12 tests.

Deferred by design:
- No persisted token storage, cross-process sharing, retry/backoff, live smoke test harness, or credential-management UI was added in this wave.

---

## Wave 48 Completion Notes

Completed implementation slices:
- Updated DragonCAD credential loading to read process environment variables first and user-scoped environment variables second.
- This fixes local Digi-Key app credentials created through Windows user environment variables without requiring a terminal/app restart to copy them into the current process.
- Added bounded retry/backoff support for transient vendor HTTP responses such as HTTP 429, 408, 502, 503, and 504.
- Wired Digi-Key and Mouser search clients through the retry policy while leaving authentication failures non-retryable.
- Increased the default HawkCAD bundled-library preload from a small 250-device window to all importable bundled devices. In the current bundled library this loads 11,670 importable components from 12,253 indexed devices.

Verification evidence:
- Focused sourcing tests for credential fallback, Digi-Key/Mouser clients, and retry behavior passed with 14 tests.
- Focused app tests for full-library preload and marketplace sync command behavior passed with 7 tests.

Deferred by design:
- No persisted credential vault UI, live vendor smoke test, provider-specific rate budget display, or trusted-library promotion was added in this wave.

---

## Wave 49 Completion Notes

Completed implementation slices:
- Added an in-use vendor sync planner that derives Digi-Key and Mouser refresh requests from schematic components already placed in the active design.
- The planner deduplicates repeated reference designators for the same component and skips unsourced library rows that do not have a manufacturer part number.
- `MainWindowViewModel` now exposes `InUseVendorCatalogSyncQueue` and `InUseVendorCatalogSyncSummary`, refreshing them whenever schematic placement/removal changes the board sync.
- Added a `Run In-Use Sync` command that executes the queued manufacturer part number searches through the existing vendor sync service and aggregates the results in the API Sync Results panel.
- The Marketplace Vendor Sync panel now shows an `In-Use Vendor Refresh` section so parts actively used in the design are visible before BOM/order planning.

Verification evidence:
- Focused in-use planner, shell integration, XAML surface, and command tests passed with 5 tests.

Deferred by design:
- The command is user-triggered to avoid surprise vendor API calls, rate-limit usage, and network work while editing.
- No background scheduler, persisted sync cache, trusted-library mutation, or vendor match promotion was added in this wave.

---

## Wave 50 Completion Notes

Completed implementation slices:
- Added per-component/provider/query sync state for the in-use vendor refresh queue.
- Queue rows now show whether a placed part has never synced or was recently refreshed, including candidate and warning counts.
- `Run In-Use Sync` now skips fresh requests inside the current freshness window instead of repeating Digi-Key/Mouser calls immediately.
- The Marketplace panel binds the sync-state label so the user can see why a request will run or be skipped.

Verification evidence:
- Focused planner, command, and XAML surface tests passed with 8 tests.

Deferred by design:
- Sync state remains in-memory for the current app session.
- No persisted vendor sync cache, background timer, or trusted-library merge from refreshed results was added in this wave.

---

## Wave 51 Completion Notes

Completed implementation slices:
- Added a deterministic JSON store for in-use vendor sync state.
- The store saves component/provider/query freshness records sorted by component, provider, and query for review-friendly diffs.
- `MainWindowViewModel` now loads in-use sync state from the app artifact path at startup and saves it after successful in-use sync runs.
- A new app session with the same artifact directory now recognizes recently refreshed in-use parts and skips repeated Digi-Key/Mouser calls.

Verification evidence:
- Focused state-store and restart persistence tests passed with 3 tests.

Deferred by design:
- Sync state is still scoped to the app artifact directory, not yet to a formal DragonCAD project folder.
- No background timer, manual cache invalidation UI, provider-specific TTL policy, or trusted-library merge from refreshed results was added in this wave.

---

## Wave 52 Completion Notes

Completed implementation slices:
- Added a separate forced in-use vendor sync command for intentional stock/price refreshes.
- Normal `Run In-Use Sync` still skips fresh requests, while `Force Refresh` re-runs all runnable in-use Digi-Key/Mouser requests.
- The Marketplace `In-Use Vendor Refresh` panel now exposes both the safe sync command and the explicit force-refresh command.
- Forced refreshes update the same persisted sync state after completion.

Verification evidence:
- Focused force-refresh command and XAML surface tests passed with 2 tests.

Deferred by design:
- No automatic background timer or provider-specific TTL policy was added.
- No trusted-library merge from refreshed vendor results was added in this wave.

---

## Wave 53 Completion Notes

Completed implementation slices:
- Added provider-specific freshness policy support for in-use vendor sync.
- Digi-Key and Mouser can now age out on different schedules instead of sharing one hard-coded TTL.
- Current default policy marks Digi-Key fresh for 12 hours and Mouser fresh for 24 hours.
- The Marketplace panel now displays the active freshness policy summary next to the in-use sync controls.

Verification evidence:
- Focused provider freshness planner and XAML surface tests passed with 2 tests.

Deferred by design:
- No editable settings UI for freshness windows was added.
- No project-scoped settings file or background timer was added in this wave.

---

## Wave 54 Completion Notes

Completed implementation slices:
- Added a deterministic JSON store for in-use vendor freshness policy.
- The app now loads configured Digi-Key/Mouser freshness windows from the artifact path and saves updates automatically.
- The Marketplace `In-Use Vendor Refresh` panel now exposes editable freshness-hour fields for Digi-Key and Mouser.
- Changing the freshness window persists across app sessions and immediately refreshes the in-use queue state.

Verification evidence:
- Focused freshness-policy store, shell persistence, and XAML surface tests passed with 4 tests.

Deferred by design:
- Freshness policy is still app-artifact scoped, not yet DragonCAD project scoped.
- No advanced settings page, validation adorners, or background refresh scheduler was added in this wave.

---

## Wave 55 Completion Notes

Completed implementation slices:
- Added a reset-to-defaults command for in-use vendor freshness policy.
- The Marketplace panel now exposes `Reset Defaults` for Digi-Key/Mouser freshness windows.
- Resetting restores Digi-Key to 12 hours and Mouser to 24 hours, saves the policy, and refreshes queue bindings immediately.

Verification evidence:
- Focused reset command and XAML surface tests passed with 2 tests.

Deferred by design:
- No advanced settings page or inline validation adorners was added.
- No background refresh scheduler was added in this wave.

---

## Wave 56 Completion Notes

Completed implementation slices:
- Added local validation feedback for in-use vendor freshness hour edits.
- Invalid non-numeric or non-positive freshness values now leave the current policy unchanged and do not persist to disk.
- The Marketplace panel now displays `InUseVendorFreshnessValidationStatus` near the freshness controls.

Verification evidence:
- Focused invalid-input command and XAML surface tests passed with 2 tests.

Deferred by design:
- No full settings page or styled field-level validation adorners was added.
- No background refresh scheduler was added in this wave.

---

## Wave 57 Completion Notes

Completed implementation slices:
- Added a clear-state command for persisted in-use vendor sync state.
- The Marketplace panel now exposes `Clear Sync State` next to the freshness controls.
- Clearing sync state removes saved component/provider/query freshness records, refreshes the in-use queue immediately, and marks placed sourced parts as `Never synced` again.
- Freshness-window settings remain untouched so this is a safe queue reset, not a provider policy reset.

Verification evidence:
- Focused clear-state command and XAML surface tests passed with 2 tests.

Deferred by design:
- No background refresh scheduler was added.
- No project-scoped migration for artifact-stored sync state was added in this wave.

---

## Wave 58 Completion Notes

Completed implementation slices:
- Six-agent marketplace/provider foundation pass completed across disjoint slices.
- Added opt-in vendor live-smoke infrastructure for Digi-Key OAuth/search and Mouser keyword search behind `DRAGONCAD_VENDOR_LIVE_SMOKE`.
- Added trusted-library vendor match promotion plans/records for reviewed catalog matches without writing directly into the core library.
- Added BOM cost rollup primitives that select provider offers and price breaks, total estimated cost, and surface missing-source diagnostics.
- Added fabrication ordering provider profiles for OSH Park and PCB Cart with readiness checks for Gerbers, BOM, pick-and-place, quantity, and layer constraints.
- Added component marketplace deduplication for canonical candidates using normalized MPNs, aliases, package/value signals, and disagreement warnings.
- Added structured vendor partnership/API documentation for Digi-Key, Mouser, SparkFun, Adafruit, Jameco, OSH Park, and PCB Cart.
- Configured Git upstream `origin` as `https://github.com/tmassey1979/DragonCAD.git`.

Verification evidence:
- `dotnet build DragonCAD.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- `dotnet test DragonCAD.slnx --no-build --logger "console;verbosity=minimal"` passed with 543 tests.

Deferred by design:
- Live vendor smoke was not executed because it is intentionally opt-in and should only run when `DRAGONCAD_VENDOR_LIVE_SMOKE=1` is set for a local check.
- No real vendor ordering or checkout submission was added.
- No app UI was added yet for BOM rollups, deduplication review, or trusted-library promotion queues.

---

## Wave 59 Completion Notes

Completed implementation slices:
- Added app-side view models for BOM cost rollups, component deduplication review, trusted-library promotion, fabrication ordering readiness, vendor live-smoke status, and marketplace integration status.
- Wired the new marketplace panels into the Marketplace inspector so backend sourcing foundations are visible from the workbench.
- Wired fabrication ordering readiness into the Fabrication handoff panel, including provider readiness and disabled checkout explanations.
- Added shell coverage that verifies the new properties and XAML bindings are present.

Verification evidence:
- Focused shell integration tests passed with 2 tests.
- Full app test project passed with 331 tests.

Deferred by design:
- The new panels are seeded from deterministic sample data; live cart/project-derived rollups remain a follow-up.
- Review commands are local in-memory state changes only.
- Live vendor smoke remains disabled by default and requires explicit `DRAGONCAD_VENDOR_LIVE_SMOKE=1`.
