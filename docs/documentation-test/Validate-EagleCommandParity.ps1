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

$failures = New-Object System.Collections.Generic.List[string]
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

    if ($columns.Count -ne 5) {
        $failures.Add("Matrix row does not have 5 columns: $row")
        continue
    }

    $command = $columns[0]
    $status = $columns[2]
    $followUp = $columns[4]

    if ($approvedStatuses -notcontains $status) {
        $failures.Add("Unexpected status '$status' for '$command'.")
    }

    if (($status -eq "Partial" -or $status -eq "Planned") -and $followUp -notmatch '\]\([^)]+\)') {
        $failures.Add("$status row '$command' must link to a GitHub issue or roadmap section.")
    }

    if ($status -eq "Planned" -and $followUp -notmatch 'https://github\.com/tmassey1979/DragonCAD/issues/\d+') {
        $failures.Add("Planned row '$command' must link to a GitHub issue.")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "EAGLE command parity matrix validated."
