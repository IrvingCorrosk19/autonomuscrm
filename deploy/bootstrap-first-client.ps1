# Bootstrap tenant + usuarios para pruebas primer cliente (TechSolutions Panama)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "tests\first-client\config.json"),
    [switch]$SkipHealthWait,
    [int]$HealthTimeoutSec = 120
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
if (-not $PSScriptRoot) { $Root = (Get-Location).Path }

if (-not (Test-Path $ConfigPath)) {
    throw "No existe $ConfigPath. Copie config.example.json a config.json y ajuste credenciales."
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$base = $config.baseUrl.TrimEnd('/')
$platformKey = $config.platformKey
$tenantName = $config.tenantName
$password = $config.defaultPassword
$pgBin = "C:\Program Files\PostgreSQL\18\bin"
$Psql = Join-Path $pgBin "psql.exe"

function Test-ApiHealthy([string]$uri) {
    $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 10
    if ($r.StatusCode -ne 200) { return $false }
    $body = [string]$r.Content
    return ($body -match 'Healthy')
}

function Wait-ApiHealth {
    if ($SkipHealthWait) { return }
    $healthUri = if ($base -match 'localhost') { "$($base -replace 'localhost','127.0.0.1')/health/live" } else { "$base/health/live" }
    Write-Host "==> Esperando API en $healthUri (max ${HealthTimeoutSec}s)..."
    $deadline = (Get-Date).AddSeconds($HealthTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            if (Test-ApiHealthy $healthUri) {
                Write-Host "    API lista: Healthy"
                return
            }
        } catch { Start-Sleep -Seconds 2 }
    }
    throw "API no respondio en $base dentro de ${HealthTimeoutSec}s. Ejecute: dotnet run --project AutonomusCRM.API --urls http://127.0.0.1:5154"
}

function Get-TenantIdFromDb {
    if (-not (Test-Path $Psql)) {
        Write-Host "    psql no encontrado; omitiendo consulta tenant en BD"
        return $null
    }
    $pg = $config.postgres
    $sqlFile = Join-Path $Root "ops\database\13_get_tenant_id_by_name.sql"
    if (-not (Test-Path $sqlFile)) { return $null }
    $env:PGPASSWORD = $pg.password
    try {
        $id = & $Psql -h $pg.host -p $pg.port -U $pg.user -d $pg.database -t -A `
            -v "tenant_name=$tenantName" -f $sqlFile 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "    Consulta tenant en BD omitida ($id)"
            return $null
        }
        if ($id -and $id.Trim()) { return [guid]$id.Trim() }
    } catch {
        Write-Host "    No se pudo consultar tenant en BD: $($_.Exception.Message)"
    } finally {
        $env:PGPASSWORD = $null
    }
    return $null
}

function Get-AdminToken([guid]$tenantId, [string]$adminEmail) {
    $body = @{ Email = $adminEmail; Password = $password; TenantId = '00000000-0000-0000-0000-000000000000' } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 30
    if (-not $login.accessToken) { throw "Login fallo para $adminEmail" }
    return $login.accessToken
}

function Set-TestPlanStarter([guid]$tenantId) {
    if (-not (Test-Path $Psql)) {
        Write-Host "    psql no disponible; plan free limita a 5 usuarios"
        return
    }
    $pg = $config.postgres
    $sqlFile = Join-Path $Root "ops\database\12_bump_tenant_plan_starter.sql"
    if (-not (Test-Path $sqlFile)) { return }
    $env:PGPASSWORD = $pg.password
    $tid = $tenantId.ToString()
    try {
        & $Psql -h $pg.host -p $pg.port -U $pg.user -d $pg.database -v ON_ERROR_STOP=1 `
            -v "tenant_id=$tid" -f $sqlFile | Out-Null
        Write-Host "    Plan starter activado (max 10 usuarios) para pruebas"
    } catch {
        Write-Host "    No se pudo actualizar plan: $($_.Exception.Message)"
    } finally {
        $env:PGPASSWORD = $null
    }
}

function Ensure-Tenant {
    $existing = Get-TenantIdFromDb
    if ($existing) {
        Write-Host "==> Tenant ya existe: $tenantName ($existing)"
        return $existing
    }

    Write-Host "==> Provisionando tenant '$tenantName'..."
    $provisionUser = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
    if (-not $provisionUser) { throw "config.json debe tener un usuario con provision=true" }

    $body = @{
        name = $tenantName
        description = "Tenant de pruebas primer cliente"
        adminEmail = $provisionUser.email
        adminPassword = $password
    } | ConvertTo-Json

    $headers = @{ "X-Platform-Key" = $platformKey }
    $resp = Invoke-RestMethod -Uri "$base/api/provisioning/tenants" -Method POST -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 60
    Write-Host "    Tenant creado: $($resp.tenantId)"
    return [guid]$resp.tenantId
}

function Ensure-TeamUsers([guid]$tenantId, [string]$adminEmail) {
    $token = Get-AdminToken $tenantId $adminEmail
    $headers = @{ Authorization = "Bearer $token" }

    foreach ($u in $config.users) {
        if ($u.provision -eq $true) {
            Write-Host "    Admin provisionado: $($u.email) [$($u.role)]"
            continue
        }

        $body = @{
            tenantId = $tenantId
            email = $u.email
            password = $password
            firstName = $u.firstName
            lastName = $u.lastName
            role = $u.role
        } | ConvertTo-Json

        try {
            $userId = Invoke-RestMethod -Uri "$base/api/users" -Method POST -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 30
            Write-Host "    Usuario creado: $($u.email) [$($u.role)] id=$userId"
        } catch {
            $msg = $_.Exception.Message
            if ($msg -match 'duplicate|already|exists|unique|400|Bad Request') {
                Write-Host "    Usuario ya existe: $($u.email) (omitido)"
            } elseif ($msg -match '403|Forbidden|PLAN_LIMIT') {
                Write-Host "    Limite de plan; activando starter y reintentando $($u.email)..."
                Set-TestPlanStarter $tenantId
                $userId = Invoke-RestMethod -Uri "$base/api/users" -Method POST -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 30
                Write-Host "    Usuario creado: $($u.email) [$($u.role)] id=$userId"
            } else {
                throw "Error creando $($u.email): $msg"
            }
        }
    }
}

function Write-CredentialsFile([guid]$tenantId) {
    $outDir = Join-Path $Root "tests\first-client"
    $credFile = Join-Path $outDir "credentials-$((Get-Date).ToString('yyyyMMdd-HHmmss')).txt"
    $lines = @(
        "TechSolutions Panama - credenciales de prueba",
        "Generado: $(Get-Date -Format o)",
        "URL: $base/Account/Login",
        "TenantId: $tenantId",
        "Password (todos): $password",
        ""
    )
    foreach ($u in $config.users) {
        $lines += "$($u.role.PadRight(8)) $($u.email)"
    }
    $lines += ""
    $lines += "Ejecutar QA: .\tests\e2e\run-first-client-qa.ps1"
    $lines | Set-Content $credFile -Encoding UTF8
    Write-Host ""
    Write-Host "Credenciales guardadas: $credFile" -ForegroundColor Cyan
}

Write-Host "=== Bootstrap primer cliente ===" -ForegroundColor Cyan
Wait-ApiHealth

$admin = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
$tenantId = Ensure-Tenant
Set-TestPlanStarter $tenantId
Ensure-TeamUsers $tenantId $admin.email
Write-CredentialsFile $tenantId

Write-Host ""
Write-Host "LISTO. Inicie sesion en $base/Account/Login" -ForegroundColor Green
Write-Host "  $($admin.email) / $password"
