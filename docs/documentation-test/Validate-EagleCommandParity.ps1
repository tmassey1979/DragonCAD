$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$matrixPath = Join-Path $repositoryRoot "docs\eagle-command-parity.md"
$matrix = Get-Content -Path $matrixPath

$approvedStatuses = @(
    "Implemented",
    "Partial",
    "Planned",
    "Intentionally different"
)

$requiredCommands = @(
    "ADD",
    "MOVE",
    "COPY",
    "DELETE",
    "GROUP",
    "ROUTE",
    "RIPUP",
    "VIA",
    "CHANGE",
    "DISPLAY",
    "GRID",
    "SMASH",
    "INVOKE",
    "NAME",
    "VALUE",
    "ERC",
    "DRC",
    "EXPORT",
    "SCRIPT/ULP",
    "LIBRARY"
)

$expectedHeader = "| EAGLE command/tool | DragonCAD workflow | Status | DragonCAD command id | Help topic | Implementation issues | Notes |"
$failures = New-Object System.Collections.Generic.List[string]

if ($matrix -notcontains $expectedHeader) {
    $failures.Add("EAGLE command parity matrix must use the 7-column completion header.")
}

$rows = $matrix | Where-Object {
    $_ -match '^\|' -and
    $_ -notmatch '^\|\s*---' -and
    $_ -notmatch '^\|\s*EAGLE command/tool\s*\|'
}

if ($rows.Count -eq 0) {
    $failures.Add("EAGLE command parity matrix does not contain any data rows.")
}

foreach ($row in $rows) {
    $columns = $row.Trim("|").Split("|").ForEach({ $_.Trim() })

    if ($columns.Count -ne 7) {
        $failures.Add("Matrix row does not have 7 columns: $row")
        continue
    }

    $command = $columns[0]
    $status = $columns[2]
    $dragonCadCommandId = $columns[3]
    $helpTopic = $columns[4]
    $implementationIssues = $columns[5]

    if ($approvedStatuses -notcontains $status) {
        $failures.Add("Unexpected status '$status' for '$command'.")
    }

    if ([string]::IsNullOrWhiteSpace($dragonCadCommandId)) {
        $failures.Add("Row '$command' must include a DragonCAD command id or 'Not available'.")
    }

    if ($helpTopic -notmatch '\]\([^)]+\)') {
        $failures.Add("Row '$command' must link to a help topic.")
    }

    if ($implementationIssues -notmatch 'https://github\.com/tmassey1979/DragonCAD/issues/\d+') {
        $failures.Add("Row '$command' must link to at least one implementation issue.")
    }

    if ($status -eq "Planned" -and $implementationIssues -notmatch 'https://github\.com/tmassey1979/DragonCAD/issues/\d+') {
        $failures.Add("Planned row '$command' must link to a GitHub issue.")
    }
}

$commandsByName = @{}
foreach ($row in $rows) {
    $columns = $row.Trim("|").Split("|").ForEach({ $_.Trim() })
    if ($columns.Count -eq 7) {
        $commandsByName[$columns[0].ToUpperInvariant()] = $true
    }
}

foreach ($requiredCommand in $requiredCommands) {
    if (-not $commandsByName.ContainsKey($requiredCommand)) {
        $failures.Add("EAGLE command parity matrix is missing required command '$requiredCommand'.")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "EAGLE command parity matrix validated."
