# DragonCAD

DragonCAD is a native, cross-platform Hardware IDE for electronics development. The goal is to preserve the speed and keyboard-driven feel of classic pre-Autodesk EAGLE CAD while building a modern engineering workstation that connects schematics, PCB layout, component libraries, datasheets, sourcing, fabrication outputs, firmware, documentation, and AI-assisted review.

DragonCAD is not an Electron or browser-shell CAD app. The desktop application is written in C# with Avalonia UI and custom canvas rendering.

## Product Direction

DragonCAD is intended to become more than an EAGLE replacement:

- A fast schematic and PCB editor with EAGLE-like command workflows.
- A component-first ECAD system where symbols, footprints, packages, datasheets, sourcing, and 3D metadata stay linked.
- A local-first project format that works well with Git.
- A review-first marketplace and fabrication workflow for BOM planning, vendor handoff, Gerbers, drill files, pick-and-place, paste, and prototype/production packages.
- A Hardware IDE that can eventually include firmware, reusable hardware capsules, simulation boundaries, AI suggestions, and revision timelines.

## Current Architecture

The solution is split into bounded projects so agents can work in parallel without stepping on each other:

- `src/DragonCAD.App` - Avalonia desktop shell, workbench view models, editor canvases, component manager, marketplace panels, and local help surfaces.
- `src/DragonCAD.Core` - headless CAD/project/component models and deterministic domain services.
- `src/DragonCAD.ComponentIntelligence` - datasheet, AI-assistance, and component-intelligence boundaries.
- `src/DragonCAD.Sourcing` - vendor catalog clients, sync planning, BOM rollups, and sourcing models.
- `src/DragonCAD.Fabrication` - fabrication readiness, handoff planning, and manufacturing output foundations.
- `src/DragonCAD.Import.Eagle` - EAGLE import planning and parsing.
- `src/DragonCAD.Rendering` - rendering and viewport infrastructure.
- `src/DragonCAD.Plugins` - plugin manifest and extension foundations.

Matching test projects live under `tests/`.

See [docs/architecture.md](docs/architecture.md) for the project-by-project ownership map, including what each project must not own.

## Contributor Map

Start here before changing code:

- [Architecture map](docs/architecture.md) - project responsibilities, boundaries, and forbidden ownership.
- [Contributor guide](docs/contributing.md) - test-first workflow, Agent Boundary rules, and six-agent wave execution.
- [Implementation roadmap](docs/remaining-implementation-roadmap.md) - current epic backlog and next vertical slices.
- [Editor interaction backlog](docs/editor-interaction-backlog.md) - editor-specific interaction gaps and follow-up work.
- [Component marketplace roadmap](docs/component-marketplace-roadmap.md) - marketplace, component, sourcing, and trusted-library direction.
- [Local help: workspace basics](docs/help/getting-started/workspace.md) - in-app help source for opening and inspecting workspaces.
- [Local help: project folders](docs/help/project-system/project-folders.md) - project folder conventions.
- [Local help: schematic wires](docs/help/schematic-editing/placing-wires.md) - schematic editing help source.
- [Local help: board basics](docs/help/editing/board-basics.md) - board editing help source.
- [Generated wiki home](docs/wiki/Home.md) - exported help table of contents.

## Build, Test, Run

Requirements:

- .NET 10 SDK
- Windows, Linux, or macOS for development
- PowerShell examples below assume Windows from `C:\code\HawkCAD`

Build the app:

```powershell
dotnet build src\DragonCAD.App\DragonCAD.App.csproj -p:UseSharedCompilation=false -v:minimal
```

Run the app:

```powershell
Start-Process .\src\DragonCAD.App\bin\Debug\net10.0\DragonCAD.App.exe -WorkingDirectory .\src\DragonCAD.App\bin\Debug\net10.0
```

Run the full verification suite:

```powershell
dotnet build DragonCAD.slnx --no-restore -p:UseSharedCompilation=false -v:minimal
dotnet test DragonCAD.slnx --no-build -p:UseSharedCompilation=false --logger "console;verbosity=minimal"
```

Validate contributor documentation links:

```powershell
powershell -ExecutionPolicy Bypass -File docs\documentation-test\Validate-ReadmeLinks.ps1
```

## Development Workflow

DragonCAD is managed as vertical stories in GitHub:

- Project board: [DragonCAD Iterative Development](https://github.com/users/tmassey1979/projects/5)
- Repository: [tmassey1979/DragonCAD](https://github.com/tmassey1979/DragonCAD)
- Issue tracker: [DragonCAD issues](https://github.com/tmassey1979/DragonCAD/issues)

Every implementation story should:

- Use the format: `As a [role], I want [capability], so that [outcome]`.
- Include concrete acceptance criteria and tests.
- Stay inside its declared Agent Boundary.
- Commit independently.
- Avoid broad refactors outside the owned slice.

The project is organized for six-agent execution:

1. Component Core and Trusted Library.
2. Schematic Editor Completion.
3. PCB Editor and Routing Completion.
4. Project System and EAGLE Import Assembly.
5. Marketplace Sourcing and Fabrication Handoff.
6. Hardware IDE Long-Term Platform.

## Roadmap

Near-term focus:

1. Component draft model and component editor tools.
2. Schematic wire handles, net labels, symbol fidelity, and ERC diagnostics.
3. PCB pad routing, airwire retirement, 45-degree routing, and footprint fidelity.
4. Project center, deterministic save/load, and EAGLE sibling import assembly.
5. Fabrication outputs, BOM/pick-and-place export, sourcing review, and OSH Park/PCBCart handoff planning.

Long-term direction:

- Firmware workspace
- Hardware capsules
- AI action plans
- Revision timeline
- Simulation provider boundaries
- Plugin ecosystem
- Rich markdown help/wiki and migration documentation

## EAGLE Compatibility Philosophy

DragonCAD should support legacy EAGLE assets without cloning EAGLE internals. Importers translate EAGLE files into DragonCAD's internal model:

```text
EAGLE .sch/.brd/.lbr
        |
        v
DragonCAD import plan
        |
        v
DragonCAD project model
```

The goal is high logical and geometric fidelity while using modern, testable, deterministic architecture.

## Archive Note

This workspace was reset on 2026-05-30. Archived HawkCAD code is preserved at:

```text
C:\code\HawkCAD-archive-20260530-100015
```

Generated build folders and temporary artifacts were excluded from the archive; source, docs, tests, solution files, and `.git` metadata were preserved.
