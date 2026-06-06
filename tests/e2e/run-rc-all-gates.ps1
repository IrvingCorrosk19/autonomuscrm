# RC Zero — run Gates 6–9 automated suite (API must be running)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "first-client\config.json"),
    [switch]$SkipResponsive
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$scripts = @(
    @{ Name = 'Gate 8 Smoke'; File = 'run-rc-smoke.ps1' },
    @{ Name = 'Gate 7/9 First Client'; File = 'run-first-client-qa.ps1' }
)
if (-not $SkipResponsive) {
    $scripts += @{ Name = 'Gate 6 Responsive'; File = 'run-responsive-gate.ps1' }
}

Write-Host '=== RC Zero — All Operational Gates ===' -ForegroundColor Cyan
$failed = @()

foreach ($s in $scripts) {
    Write-Host ""
    Write-Host "--- $($s.Name) ---" -ForegroundColor Yellow
    $path = Join-Path $PSScriptRoot $s.File
    & powershell -NoProfile -File $path -ConfigPath $ConfigPath
    if ($LASTEXITCODE -ne 0) {
        $failed += $s.Name
    }
}

Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host 'ALL GATES PASS (6–9 automated)' -ForegroundColor Green
    Write-Host 'Handoff: QA_HANDOFF_READY.md | Certification: ENTERPRISE_CERTIFICATION_FINAL_REPORT.md'
    exit 0
}

Write-Host "FAILED: $($failed -join ', ')" -ForegroundColor Red
exit 1
