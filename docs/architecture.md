# DragonCAD Architecture Map

DragonCAD is a native C#/.NET Hardware IDE built around bounded projects. The app should feel like a fast, keyboard-friendly ECAD workstation, while the domain model stays deterministic, testable, and local-first.

This map defines project ownership. New work should land in the smallest project that owns the responsibility and should not pull app, vendor, fabrication, or importer concerns across boundaries.

## Runtime Projects

| Project | Owns | Must not own |
| --- | --- | --- |
| `src/DragonCAD.App` | Avalonia desktop shell, view models, editor canvases, project center surfaces, marketplace panels, help UI, app diagnostics, and user commands. | Core CAD invariants, file-format parsing rules, vendor HTTP clients, fabrication package algorithms, plugin contract design, or test-only fixtures. |
| `src/DragonCAD.Core` | Headless domain models for projects, components, schematics, geometry, firmware workspace metadata, timelines, capsules, component definitions, and deterministic domain services. | Avalonia controls, UI state, HTTP calls, vendor-specific catalog behavior, manufacturing provider policies, EAGLE parser details, or local machine secrets. |
| `src/DragonCAD.ComponentIntelligence` | Datasheet intake, candidate linking, AI action planning contracts, and component-intelligence boundaries. | Trusted-library writes without review, UI workflow orchestration, vendor ordering, fabrication outputs, or direct provider credentials. |
| `src/DragonCAD.Sourcing` | Vendor catalog descriptors, catalog normalization, vendor search clients, BOM cost rollups, quote comparison, provider credential planning, and sourcing order planning. | UI panels, schematic/board editing, fabrication provider submission rules, project persistence, or core component identity definitions. |
| `src/DragonCAD.Fabrication` | BOM aggregation, manufacturing output manifests, Gerber/drill metadata, pick-and-place formatting, fabrication readiness, review packets, and provider handoff planning. | UI command surfaces, vendor catalog search, component marketplace merge policy, EAGLE import assembly, or schematic editing behavior. |
| `src/DragonCAD.Import.Eagle` | EAGLE import planning, sibling `.sch`/`.brd`/`.lbr` assembly, import diagnostics, and translation boundaries from legacy EAGLE assets into DragonCAD models. | Native DragonCAD project persistence, live editor behavior, fabrication ordering, marketplace sourcing, or generic core geometry ownership. |
| `src/DragonCAD.Rendering` | Rendering and viewport infrastructure that can be shared without depending on app-specific workflow state. | Domain mutation, app commands, vendor logic, importer parsing, or fabrication decisions. |
| `src/DragonCAD.Plugins` | Plugin manifest and extension foundations for future integration points. | Built-in app features, core component models, UI implementation details, vendor credentials, or provider-specific business logic. |
| `src/DragonCAD.Scripting` | Scripting boundary foundations for future automation and command execution. | UI interaction state, core domain ownership, provider clients, manufacturing algorithms, or plugin packaging rules. |

## Test Projects

Each `tests/*` project should test the matching production boundary and only use cross-project references that the production code already allows.

| Project | Owns | Must not own |
| --- | --- | --- |
| `tests/DragonCAD.App.Tests` | App shell, view model, editor viewport, marketplace UI, help, and command tests. | Domain-only behavior better covered in `DragonCAD.Core.Tests` or provider algorithms better covered in provider-specific tests. |
| `tests/DragonCAD.Core.Tests` | Deterministic core models, geometry, project store, components, capsules, firmware workspace, graph, timeline, and ERC domain tests. | Avalonia UI tests, live vendor tests, or fabrication provider tests. |
| `tests/DragonCAD.ComponentIntelligence.Tests` | Datasheet intake, candidate linking, AI action planning, and component-intelligence contract tests. | UI workflow tests, sourcing provider tests, or trusted-library writes outside reviewed flows. |
| `tests/DragonCAD.Sourcing.Tests` | Vendor catalog clients, normalization, matching, credentials, BOM cost and ordering, sourcing providers, and optional smoke harness tests. | UI marketplace panels, core component identity definitions, fabrication output generation, or app help validation. |
| `tests/DragonCAD.Fabrication.Tests` | BOM aggregation, Gerber/drill manifests, pick-and-place, manufacturing output, fabrication readiness, ordering, and handoff tests. | Vendor catalog search, UI panels, EAGLE import parsing, or core schematic behavior. |
| `tests/DragonCAD.Import.Eagle.Tests` | EAGLE import and sibling assembly planning tests. | Native editor behavior, vendor sourcing, fabrication ordering, or unrelated project persistence features. |
| `tests/DragonCAD.Rendering.Tests` | Rendering assembly and shared rendering infrastructure tests. | App-specific command workflows or domain mutation behavior. |
| `tests/DragonCAD.Plugins.Tests` | Plugin manifest and extension boundary tests. | Built-in feature behavior or provider-specific business rules. |
| `tests/DragonCAD.Scripting.Tests` | Scripting assembly and automation boundary tests. | UI control behavior, provider clients, or core CAD model ownership. |

## Supporting Documentation Tool

`src/DragonCAD.Tools.Documentation` is a local executable for validating, exporting, and dry-running help/wiki synchronization. It is not listed in `DragonCAD.slnx` today, but it depends on `DragonCAD.App` help registry code and should remain a tool boundary. It must not become an app runtime dependency or a place for product behavior.

Run it with:

```powershell
dotnet run --project src\DragonCAD.Tools.Documentation\DragonCAD.Tools.Documentation.csproj -- validate .
```

## Dependency Direction

Keep dependencies moving from outer workflow layers into stable inner contracts. Today, `DragonCAD.Core` is the shared domain center for specialized headless projects, and `DragonCAD.App` composes the UI around the core, sourcing, and component-intelligence boundaries.

```text
DragonCAD.App
    -> DragonCAD.Core
    -> DragonCAD.Sourcing
    -> DragonCAD.ComponentIntelligence

DragonCAD.Sourcing              -> DragonCAD.Core
DragonCAD.Fabrication           -> DragonCAD.Core
DragonCAD.ComponentIntelligence -> DragonCAD.Core
DragonCAD.Import.Eagle          -> DragonCAD.Core

DragonCAD.Rendering
DragonCAD.Plugins
DragonCAD.Scripting
```

The exact project references can evolve, but the rule stays constant: UI and provider projects may orchestrate; core projects should model deterministic behavior; import, sourcing, fabrication, scripting, plugin, and intelligence projects should keep their specialized concerns out of each other.

## Boundary Rules

- Domain invariants belong in `DragonCAD.Core` unless they are specific to sourcing, fabrication, import, scripting, plugins, or component intelligence.
- UI-only state belongs in `DragonCAD.App`; headless workflows should not require Avalonia.
- Provider-specific HTTP, credentials, rate limits, and catalog rules belong in `DragonCAD.Sourcing`.
- Manufacturing package rules and fabrication provider handoff belong in `DragonCAD.Fabrication`.
- Legacy EAGLE compatibility belongs in `DragonCAD.Import.Eagle`; native DragonCAD project behavior belongs in core/app project surfaces.
- Documentation and help validation should stay in docs or documentation tooling and should not change runtime behavior.
