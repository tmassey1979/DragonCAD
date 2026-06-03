# DragonCAD Public Roadmap

This roadmap is the public index for DragonCAD's long-term Hardware IDE execution. It maps the GitHub project board, epic milestones, issue ranges, and current execution waves so stakeholders can see what is shipped, active, and queued without reading every story issue.

## Public Tracking

- Project board: [DragonCAD Iterative Development](https://github.com/users/tmassey1979/projects/5)
- Repository: [tmassey1979/DragonCAD](https://github.com/tmassey1979/DragonCAD)
- Issue tracker: [DragonCAD issues](https://github.com/tmassey1979/DragonCAD/issues)
- Release note template: [docs/release-notes-template.md](release-notes-template.md)

## Milestone Map

| Epic | Milestone | Public issue range | Current public state | Hardware IDE outcome |
| --- | --- | --- | --- | --- |
| 1 | [Component Core and Trusted Library](https://github.com/tmassey1979/DragonCAD/milestone/1) | [#1](https://github.com/tmassey1979/DragonCAD/issues/1)-[#4](https://github.com/tmassey1979/DragonCAD/issues/4) | Initial component intake, draft assets, editor commands, and trusted promotion simulation are closed. | Component-first CAD data with reviewed promotion before trusted-library mutation. |
| 2 | [Schematic Editor Completion](https://github.com/tmassey1979/DragonCAD/milestone/2) | [#5](https://github.com/tmassey1979/DragonCAD/issues/5)-[#8](https://github.com/tmassey1979/DragonCAD/issues/8) | Wire handles, net labels, symbol fidelity, and ERC diagnostics are closed. | EAGLE-like schematic workflows with modern diagnostics. |
| 3 | [PCB Editor and Routing Completion](https://github.com/tmassey1979/DragonCAD/milestone/3) | [#9](https://github.com/tmassey1979/DragonCAD/issues/9)-[#12](https://github.com/tmassey1979/DragonCAD/issues/12) | Pad route start/finish, airwire retirement, and footprint fidelity are closed; 45-degree routing remains open. | Board layout that can move from visible connectivity to real routing and DRC-ready geometry. |
| 4 | [Project System and Eagle Import Assembly](https://github.com/tmassey1979/DragonCAD/milestone/4) | [#13](https://github.com/tmassey1979/DragonCAD/issues/13)-[#16](https://github.com/tmassey1979/DragonCAD/issues/16) | Project center, persistence, shell save/open commands, and EAGLE sibling import assembly are closed. | Local-first project workspaces that can preserve and assemble legacy EAGLE assets. |
| 5 | [Marketplace Sourcing and Fabrication Handoff](https://github.com/tmassey1979/DragonCAD/milestone/5) | [#17](https://github.com/tmassey1979/DragonCAD/issues/17)-[#20](https://github.com/tmassey1979/DragonCAD/issues/20) | Gerber/drill, BOM/pick-and-place, vendor match review, and OSH Park/PCBCart handoff planning are closed. | Review-first sourcing and fabrication artifacts before live purchasing or manufacturing integration. |
| 6 | [Hardware IDE Long-Term Platform](https://github.com/tmassey1979/DragonCAD/milestone/6) | [#21](https://github.com/tmassey1979/DragonCAD/issues/21)-[#25](https://github.com/tmassey1979/DragonCAD/issues/25) | Firmware workspace, capsules, AI action boundaries, revision timeline, and the long-term execution map are closed. | The foundation for a broader Hardware IDE beyond schematic and PCB editing. |
| 7 | [Documentation Help and Eagle-Plus UX](https://github.com/tmassey1979/DragonCAD/milestone/7) | [#26](https://github.com/tmassey1979/DragonCAD/issues/26)-[#33](https://github.com/tmassey1979/DragonCAD/issues/33) | Contributor docs, help wiki, markdown help, migration content, tutorials, and parity docs are closed or active; public roadmap/release notes are tracked by [#33](https://github.com/tmassey1979/DragonCAD/issues/33). | Public visibility, contributor guidance, in-app help, and EAGLE-plus usability standards. |

## Current Execution Waves

DragonCAD work is organized for parallel, independent execution. Each wave owns a narrow vertical slice and should leave release notes that identify shipped behavior, validation evidence, screenshots or artifacts, known gaps, and the next story.

| Wave | Active area | Primary milestone | Representative stories | Execution focus |
| --- | --- | --- | --- | --- |
| 1 | Component Core and Trusted Library | [Epic 1](https://github.com/tmassey1979/DragonCAD/milestone/1) | [CMP-001](https://github.com/tmassey1979/DragonCAD/issues/1)-[CMP-004](https://github.com/tmassey1979/DragonCAD/issues/4) | Keep component identity, drafts, datasheet intake, and trusted-library promotion reviewable and deterministic. |
| 2 | Schematic Editor Completion | [Epic 2](https://github.com/tmassey1979/DragonCAD/milestone/2) | [SCH-001](https://github.com/tmassey1979/DragonCAD/issues/5)-[SCH-004](https://github.com/tmassey1979/DragonCAD/issues/8) | Preserve fast editor interaction while adding net association, symbol fidelity, and diagnostics. |
| 3 | PCB Editor and Routing Completion | [Epic 3](https://github.com/tmassey1979/DragonCAD/milestone/3) | [PCB-001](https://github.com/tmassey1979/DragonCAD/issues/9)-[PCB-004](https://github.com/tmassey1979/DragonCAD/issues/12) | Move board editing from visual primitives to routable, constraint-aware PCB geometry. |
| 4 | Project System and EAGLE Import Assembly | [Epic 4](https://github.com/tmassey1979/DragonCAD/milestone/4) | [PRJ-001](https://github.com/tmassey1979/DragonCAD/issues/13)-[PRJ-004](https://github.com/tmassey1979/DragonCAD/issues/16) | Stabilize local project folders, recent projects, persistence, and sibling EAGLE import assembly. |
| 5 | Marketplace Sourcing and Fabrication Handoff | [Epic 5](https://github.com/tmassey1979/DragonCAD/milestone/5) | [MFG-001](https://github.com/tmassey1979/DragonCAD/issues/17)-[MFG-003](https://github.com/tmassey1979/DragonCAD/issues/20) | Produce reviewable sourcing, BOM, manufacturing, and handoff artifacts without live external actions. |
| 6 | Hardware IDE Long-Term Platform | [Epic 6](https://github.com/tmassey1979/DragonCAD/milestone/6) | [IDE-001](https://github.com/tmassey1979/DragonCAD/issues/21)-[ROADMAP-001](https://github.com/tmassey1979/DragonCAD/issues/25) | Expand toward firmware, capsules, AI-assisted review, timeline history, simulation boundaries, and plugins. |
| 7 | Documentation Help and EAGLE-Plus UX | [Epic 7](https://github.com/tmassey1979/DragonCAD/milestone/7) | [DOC-001](https://github.com/tmassey1979/DragonCAD/issues/26)-[DOC-008](https://github.com/tmassey1979/DragonCAD/issues/33) | Make the work visible and usable through contributor docs, help content, migration guidance, tutorials, and public release notes. |

## Roadmap Update Rules

- Update this roadmap when a new epic milestone, public issue range, or execution wave is added.
- Keep public links pointed at the GitHub project, milestones, and issue tracker instead of private planning notes.
- Record releases with [docs/release-notes-template.md](release-notes-template.md) before closing a public delivery wave.
- Validate roadmap and release-note structure with:

```powershell
powershell -ExecutionPolicy Bypass -File docs\documentation-test\Validate-RoadmapReleaseNotes.ps1
```
