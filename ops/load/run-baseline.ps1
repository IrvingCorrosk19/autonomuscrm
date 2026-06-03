# k6 baseline runner — 10 / 50 / 100 VUs
param(
    [string]$BaseUrl = $env:BASE_URL ?? "http://localhost:8080",
    [string]$TenantId = $env:TENANT_ID
)

if (-not $TenantId) { throw "TENANT_ID required" }

$scripts = @(
    "ops/load/health.js",
    "ops/load/login.js",
    "ops/load/revenue.js"
)

foreach ($vus in @(10, 50, 100)) {
    foreach ($script in $scripts) {
        Write-Host "=== k6 $script @ ${vus} VUs ==="
        k6 run --vus $vus --duration 30s `
            -e "BASE_URL=$BaseUrl" `
            -e "TENANT_ID=$TenantId" `
            -e "ADMIN_EMAIL=$($env:ADMIN_EMAIL ?? 'admin@autonomuscrm.local')" `
            -e "ADMIN_PASSWORD=$($env:ADMIN_PASSWORD ?? 'Admin123!')" `
            $script
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Baseline stopped at ${vus} VUs — $script failed"
            exit 1
        }
    }
}

Write-Host "Baseline complete: 10/50/100 VUs passed"
