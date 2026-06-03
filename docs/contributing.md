# Contributing To DragonCAD

DragonCAD work is organized as small vertical stories with explicit Agent Boundaries. Before changing code, read the current issue, confirm the owned paths, and inspect nearby tests and docs.

## Test-First Development

Use test-first development for behavior changes:

1. Add or update the narrowest failing test that describes the acceptance criterion.
2. Run that focused test and confirm it fails for the expected reason.
3. Implement the smallest change that makes the test pass.
4. Run the focused test again.
5. Run the broader affected suite before committing.

Documentation-only changes may use documentation validation instead of product tests when no runtime behavior changes. For README link coverage, run:

```powershell
powershell -ExecutionPolicy Bypass -File docs\documentation-test\Validate-ReadmeLinks.ps1
```

For help/wiki validation, run:

```powershell
dotnet run --project src\DragonCAD.Tools.Documentation\DragonCAD.Tools.Documentation.csproj -- validate .
```

## Standard Commands

From the repository root:

```powershell
dotnet restore DragonCAD.slnx
dotnet build DragonCAD.slnx -p:UseSharedCompilation=false -v:minimal
dotnet test DragonCAD.slnx --no-build -p:UseSharedCompilation=false --logger "console;verbosity=minimal"
```

Run the desktop app after a successful build:

```powershell
Start-Process .\src\DragonCAD.App\bin\Debug\net10.0\DragonCAD.App.exe -WorkingDirectory .\src\DragonCAD.App\bin\Debug\net10.0
```

## Agent Boundary Rules

Every issue or wave task should declare owned paths. Treat that list as the write boundary.

- Write only inside your owned paths.
- Read broadly enough to understand existing behavior and contracts.
- Do not revert, reformat, or overwrite another worker's changes.
- Do not move behavior across projects unless the story explicitly owns that boundary.
- Keep commits focused on the story.
- If another worker's change affects your slice, adapt to it without deleting it.
- If the issue cannot be completed inside the boundary, stop and report the blocker.

Documentation agents should avoid app behavior, schematic editor, board editor, project center, source code, sourcing, fabrication, importer, and shared-contract changes unless those paths are explicitly owned.

## Six-Agent Wave Execution

DragonCAD development waves are split so agents can advance independent tracks at the same time:

1. Component Core and Trusted Library.
2. Schematic Editor Completion.
3. PCB Editor and Routing Completion.
4. Project System and EAGLE Import Assembly.
5. Marketplace Sourcing and Fabrication Handoff.
6. Hardware IDE Long-Term Platform.

For each wave:

- Start by commenting on the assigned GitHub issue with your story and owned paths.
- Keep progress comments factual and tied to changed files, tests, and blockers.
- Validate only the affected boundary unless the change is broad enough to require the full suite.
- On completion, comment with files changed, validation run, commit hash if available, and follow-up.
- If validation passes and the change is safe, move the issue/project item to Done and close the issue.

## Commit Hygiene

Before committing:

1. Inspect `git status --short --branch`.
2. Inspect the path-scoped diff for your owned files.
3. Stage only owned files.
4. Review `git diff --staged`.
5. Commit with a message that describes the completed story.

Do not stage unrelated changes just because they are present in the shared checkout.
