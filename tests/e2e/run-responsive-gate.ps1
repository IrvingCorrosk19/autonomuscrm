# RC Zero — Gate 6 responsive validation (Playwright headless)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "first-client\config.json")
)

$ErrorActionPreference = 'Stop'
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$env:CRM_BASE_URL = if ($config.baseUrl -match 'localhost') { $config.baseUrl -replace 'localhost', '127.0.0.1' } else { $config.baseUrl }
$admin = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
$env:CRM_ADMIN_EMAIL = $admin.email
$env:CRM_ADMIN_PASSWORD = $config.defaultPassword

Write-Host '=== RC Zero Responsive Gate ===' -ForegroundColor Cyan
Write-Host "Base: $env:CRM_BASE_URL"

$scriptDir = $PSScriptRoot
Push-Location $scriptDir
try {
    if (-not (Test-Path 'node_modules/playwright')) {
        Write-Host 'Installing playwright (one-time)...'
        npm init -y 2>$null | Out-Null
        npm install playwright --no-save 2>&1 | Out-Null
        npx playwright install chromium 2>&1 | Out-Null
    }
    node run-responsive-gate.mjs
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
