$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$readmePath = Join-Path $repositoryRoot "README.md"
$readme = Get-Content -Raw -Path $readmePath

$requiredLinks = @(
    @{
        Label = "Project board"
        Text = "[DragonCAD Iterative Development](https://github.com/users/tmassey1979/projects/5)"
    },
    @{
        Label = "Repository"
        Text = "[tmassey1979/DragonCAD](https://github.com/tmassey1979/DragonCAD)"
    },
    @{
        Label = "Implementation roadmap"
        Text = "[Implementation roadmap](docs/remaining-implementation-roadmap.md)"
        Path = "docs\remaining-implementation-roadmap.md"
    },
    @{
        Label = "Editor interaction backlog"
        Text = "[Editor interaction backlog](docs/editor-interaction-backlog.md)"
        Path = "docs\editor-interaction-backlog.md"
    },
    @{
        Label = "Component marketplace roadmap"
        Text = "[Component marketplace roadmap](docs/component-marketplace-roadmap.md)"
        Path = "docs\component-marketplace-roadmap.md"
    },
    @{
        Label = "Local help workspace"
        Text = "[Local help: workspace basics](docs/help/getting-started/workspace.md)"
        Path = "docs\help\getting-started\workspace.md"
    },
    @{
        Label = "Local help project folders"
        Text = "[Local help: project folders](docs/help/project-system/project-folders.md)"
        Path = "docs\help\project-system\project-folders.md"
    },
    @{
        Label = "Local help schematic wires"
        Text = "[Local help: schematic wires](docs/help/schematic-editing/placing-wires.md)"
        Path = "docs\help\schematic-editing\placing-wires.md"
    },
    @{
        Label = "Local help board basics"
        Text = "[Local help: board basics](docs/help/editing/board-basics.md)"
        Path = "docs\help\editing\board-basics.md"
    },
    @{
        Label = "Generated wiki home"
        Text = "[Generated wiki home](docs/wiki/Home.md)"
        Path = "docs\wiki\Home.md"
    },
    @{
        Label = "Architecture map"
        Text = "[Architecture map](docs/architecture.md)"
        Path = "docs\architecture.md"
    },
    @{
        Label = "Contributor guide"
        Text = "[Contributor guide](docs/contributing.md)"
        Path = "docs\contributing.md"
    }
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($requiredLink in $requiredLinks) {
    if (-not $readme.Contains($requiredLink.Text)) {
        $failures.Add("README is missing required link: $($requiredLink.Label)")
    }

    if ($requiredLink.ContainsKey("Path")) {
        $targetPath = Join-Path $repositoryRoot $requiredLink.Path
        if (-not (Test-Path -Path $targetPath -PathType Leaf)) {
            $failures.Add("README link target is missing: $($requiredLink.Path)")
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "README documentation links validated."
