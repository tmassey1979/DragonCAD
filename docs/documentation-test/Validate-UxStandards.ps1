$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$standardsPath = Join-Path $repositoryRoot "docs\ux\eagle-plus-standards.md"
$checklistPath = Join-Path $repositoryRoot "docs\ux\editor-acceptance-checklist.md"

$failures = New-Object System.Collections.Generic.List[string]

foreach ($path in @($standardsPath, $checklistPath)) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        $failures.Add("Missing required UX documentation file: $path")
    }
}

if ($failures.Count -eq 0) {
    $standards = Get-Content -Raw -Path $standardsPath
    $checklist = Get-Content -Raw -Path $checklistPath

    $requiredStandards = @(
        "Speed",
        "Keyboard Workflows",
        "Toolbars",
        "Command Line",
        "Viewport Controls",
        "Docking",
        "Local-First Behavior",
        "Review-First AI",
        "Review-First Marketplace",
        "Project History"
    )

    foreach ($standard in $requiredStandards) {
        if ($standards -notmatch "(?m)^## $([regex]::Escape($standard))$") {
            $failures.Add("Standards document is missing section: $standard")
        }
    }

    foreach ($classification in @("Preserve", "Modernize", "Replace")) {
        if ($standards -notmatch "(?i)\b$classification\b") {
            $failures.Add("Standards document does not describe EAGLE behavior to $classification.")
        }
    }

    $checklistSections = [regex]::Matches($checklist, "(?ms)^## \d+\. .+?(?=^## \d+\. |\z)")
    if ($checklistSections.Count -eq 0) {
        $failures.Add("Checklist document does not contain numbered checklist sections.")
    }

    foreach ($section in $checklistSections) {
        $sectionText = $section.Value
        $header = ($sectionText -split "`r?`n")[0]

        if ($sectionText -notmatch "https://github\.com/tmassey1979/DragonCAD/(issues/\d+|milestone/\d+)") {
            $failures.Add("Checklist section lacks a GitHub issue or milestone link: $header")
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "Eagle-Plus UX standards validated."
