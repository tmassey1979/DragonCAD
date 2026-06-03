$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$roadmapPath = Join-Path $repositoryRoot "docs\roadmap.md"
$releaseNotesTemplatePath = Join-Path $repositoryRoot "docs\release-notes-template.md"

$failures = New-Object System.Collections.Generic.List[string]

foreach ($path in @($roadmapPath, $releaseNotesTemplatePath)) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        $failures.Add("Missing required documentation file: $path")
    }
}

if ($failures.Count -eq 0) {
    $roadmap = Get-Content -Raw -Path $roadmapPath
    $releaseNotesTemplate = Get-Content -Raw -Path $releaseNotesTemplatePath

    $requiredRoadmapLinks = @(
        "https://github.com/users/tmassey1979/projects/5",
        "https://github.com/tmassey1979/DragonCAD/milestone/1",
        "https://github.com/tmassey1979/DragonCAD/milestone/2",
        "https://github.com/tmassey1979/DragonCAD/milestone/3",
        "https://github.com/tmassey1979/DragonCAD/milestone/4",
        "https://github.com/tmassey1979/DragonCAD/milestone/5",
        "https://github.com/tmassey1979/DragonCAD/milestone/6",
        "https://github.com/tmassey1979/DragonCAD/milestone/7"
    )

    foreach ($link in $requiredRoadmapLinks) {
        if (-not $roadmap.Contains($link)) {
            $failures.Add("Roadmap is missing required public link: $link")
        }
    }

    $requiredRoadmapSections = @(
        "## Public Tracking",
        "## Milestone Map",
        "## Current Execution Waves",
        "## Roadmap Update Rules"
    )

    foreach ($section in $requiredRoadmapSections) {
        if (-not $roadmap.Contains($section)) {
            $failures.Add("Roadmap is missing required section: $section")
        }
    }

    $issueRangePattern = "https://github\.com/tmassey1979/DragonCAD/issues/\d+\)-\[#\d+\]\(https://github\.com/tmassey1979/DragonCAD/issues/\d+"
    if ([regex]::Matches($roadmap, $issueRangePattern).Count -lt 7) {
        $failures.Add("Roadmap must include issue ranges for all public epic milestones.")
    }

    $requiredReleaseSections = @(
        "## Release",
        "## Shipped Features",
        "## Tests Run",
        "## Screenshots And Artifacts",
        "## Known Gaps",
        "## Next Stories",
        "## Release Note Checklist"
    )

    foreach ($section in $requiredReleaseSections) {
        if (-not $releaseNotesTemplate.Contains($section)) {
            $failures.Add("Release notes template is missing required section: $section")
        }
    }

    foreach ($section in $requiredReleaseSections) {
        $sectionPattern = "(?ms)^$([regex]::Escape($section))\s*(?<body>.*?)(?=^## |\z)"
        $match = [regex]::Match($releaseNotesTemplate, $sectionPattern)
        if (-not $match.Success) {
            continue
        }

        $body = $match.Groups["body"].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($body)) {
            $failures.Add("Release notes template section is empty: $section")
        }
    }

    if (-not $releaseNotesTemplate.Contains("https://github.com/users/tmassey1979/projects/5")) {
        $failures.Add("Release notes template is missing the GitHub Project #5 link.")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "Roadmap and release notes documentation validated."
