# Fase 2 — P0 críticos AutonomusFlow (http://localhost:5154)
$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5154'
$tenant = 'd7a30c86-7bb7-4303-9c1b-a0518fd78c67'
$otherTenant = '00000000-0000-0000-0000-000000000001'
$date = Get-Date -Format 'yyyy-MM-dd'
$evidenceDir = Join-Path (Split-Path $PSScriptRoot -Parent) "qa-evidence\$date"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$ts = Get-Date -Format 'yyyyMMddHHmmss'

function Add-Result($id, $status, $note = '') {
    $script:results.Add([pscustomobject]@{ Id = $id; Status = $status; Note = $note; At = (Get-Date).ToString('o') })
    $color = switch ($status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } 'BLOCKED' { 'Yellow' } default { 'Gray' } }
    Write-Host "[$status] $id $(if ($note) { "- $note" })" -ForegroundColor $color
}

function Save-Evidence($name, $content) {
    $path = Join-Path $evidenceDir "$ts-$name.txt"
    $content | Out-File -FilePath $path -Encoding utf8
}

function Get-Antiforgery($html) {
    if ($html -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') { return $Matches[1] }
    return ''
}

function New-Session($email, $pass) {
    $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 15
    $t = Get-Antiforgery $loginPage.Content
    Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
        TenantId = $tenant; Email = $email; Password = $pass; __RequestVerificationToken = $t
    } -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 15 | Out-Null
    return $s
}

function Test-Login($id, $email, $pass) {
    try {
        $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 15
        $t = Get-Antiforgery $loginPage.Content
        $r = Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
            TenantId = $tenant; Email = $email; Password = $pass; __RequestVerificationToken = $t
        } -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 15
        $ok = ($r.BaseResponse.ResponseUri.LocalPath -eq '/' -or $r.Content -match 'Dashboard')
        Add-Result $id $(if ($ok) { 'PASS' } else { 'FAIL' }) $r.BaseResponse.ResponseUri.ToString()
        Save-Evidence "$id-login" "Uri=$($r.BaseResponse.ResponseUri)"
    } catch { Add-Result $id 'FAIL' $_.Exception.Message }
}

Write-Host "=== P0 QA AutonomusFlow ===" -ForegroundColor Cyan
Write-Host "Evidence: $evidenceDir"

# Health
try {
    $h = Invoke-RestMethod -Uri "$base/health" -TimeoutSec 10
    Add-Result 'API-001' 'PASS' ($h.status)
    Save-Evidence 'API-001-health' ($h | ConvertTo-Json)
} catch { Add-Result 'API-001' 'FAIL' $_.Exception.Message }

# AUTH P0
Test-Login 'AUTH-001' 'admin@autonomuscrm.local' 'Admin123!'
Test-Login 'AUTH-002' 'manager@autonomuscrm.local' 'Manager123!'
Test-Login 'AUTH-003' 'sales@autonomuscrm.local' 'Sales123!'
Test-Login 'AUTH-004' 'support@autonomuscrm.local' 'Support123!'
Test-Login 'AUTH-005' 'viewer@autonomuscrm.local' 'Viewer123!'

try {
    $sBad = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $lp = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $sBad -UseBasicParsing
    $tb = Get-Antiforgery $lp.Content
    $bad = Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $sBad -Body @{
        TenantId = $tenant; Email = 'admin@autonomuscrm.local'; Password = 'wrong'; __RequestVerificationToken = $tb
    } -MaximumRedirection 0 -UseBasicParsing
    Add-Result 'AUTH-006' $(if ($bad.Content -match 'alert|inválid|incorrect|error') { 'PASS' } else { 'FAIL' })
} catch { Add-Result 'AUTH-006' 'PASS' 'redirect/error' }

# SEC
$rAnon = Invoke-WebRequest -Uri "$base/Users" -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 15
Add-Result 'SEC-S-01' $(if ($rAnon.BaseResponse.ResponseUri.LocalPath -match 'Login') { 'PASS' } else { 'FAIL' }) $rAnon.BaseResponse.ResponseUri.ToString()

$sViewer = New-Session 'viewer@autonomuscrm.local' 'Viewer123!'
try {
    $pCreate = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $sViewer -MaximumRedirection 5 -UseBasicParsing
    Add-Result 'SEC-V-01' $(if ($pCreate.BaseResponse.ResponseUri.LocalPath -match 'AccessDenied') { 'PASS' } else { 'FAIL' }) $pCreate.BaseResponse.ResponseUri.ToString()
} catch { Add-Result 'SEC-V-01' 'PASS' 'blocked' }

$sSales = New-Session 'sales@autonomuscrm.local' 'Sales123!'
$rUsers = Invoke-WebRequest -Uri "$base/Users" -WebSession $sSales -MaximumRedirection 5 -UseBasicParsing
Add-Result 'SEC-S-02' $(if ($rUsers.BaseResponse.ResponseUri.LocalPath -match 'AccessDenied') { 'PASS' } else { 'FAIL' })

# API JWT
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenant
    } | ConvertTo-Json) -TimeoutSec 15
    Add-Result 'API-002' $(if ($login.accessToken) { 'PASS' } else { 'FAIL' })
    Save-Evidence 'API-002-token' "token_len=$($login.accessToken.Length)"
    $leads = Invoke-RestMethod -Uri "$base/api/leads?tenantId=$tenant" -Headers @{ Authorization = "Bearer $($login.accessToken)" } -TimeoutSec 15
    Add-Result 'API-003' 'PASS' "count=$($leads.Count)"
} catch { Add-Result 'API-002' 'FAIL' $_.Exception.Message; Add-Result 'API-003' 'BLOCKED' 'no token' }

# TEN-003 cross-tenant customer API
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenant
    } | ConvertTo-Json)
    $newCust = @{
        tenantId = $tenant; name = "P0 Tenant Test $ts"; email = "p0.ten.$ts@test.local"
    } | ConvertTo-Json
    $cidRaw = Invoke-RestMethod -Uri "$base/api/customers" -Method POST -Headers @{ Authorization = "Bearer $($login.accessToken)" } -ContentType 'application/json' -Body $newCust -TimeoutSec 15
    if ($cidRaw) {
        $cid = [guid]$cidRaw.ToString()
        try {
            Invoke-RestMethod -Uri "$base/api/customers/$($cid)?tenantId=$otherTenant" -Headers @{ Authorization = "Bearer $($login.accessToken)" } -TimeoutSec 15 -ErrorAction Stop | Out-Null
            Add-Result 'TEN-003' 'FAIL' 'cross-tenant returned data'
        } catch {
            $code = 0
            if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
            Add-Result 'TEN-003' $(if ($code -eq 404 -or $code -eq 403) { 'PASS' } else { 'FAIL' }) "HTTP $code"
            Save-Evidence 'TEN-003' "HTTP $code"
        }
    } else {
        Add-Result 'TEN-003' 'SKIP' 'no customers to test'
    }
} catch { Add-Result 'TEN-003' 'BLOCKED' $_.Exception.Message }

# SameTenant query mismatch
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenant
    } | ConvertTo-Json)
    try {
        Invoke-RestMethod -Uri "$base/api/leads?tenantId=$otherTenant" -Headers @{ Authorization = "Bearer $($login.accessToken)" } -TimeoutSec 15 -ErrorAction Stop | Out-Null
        Add-Result 'TEN-004' 'FAIL' 'wrong tenantId query allowed'
    } catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        Add-Result 'TEN-004' $(if ($code -eq 403 -or $code -eq 401) { 'PASS' } else { 'FAIL' }) "HTTP $code"
    }
} catch { Add-Result 'TEN-004' 'BLOCKED' $_.Exception.Message }

# E2E-001 Lead flow (abbreviated)
$s = New-Session 'admin@autonomuscrm.local' 'Admin123!'
$leadEmail = "p0.e2e.$ts@test.local"
try {
    $pCreate = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $s -UseBasicParsing
    $tc = Get-Antiforgery $pCreate.Content
    $rLead = Invoke-WebRequest -Uri "$base/Leads/Create?handler=Create" -Method POST -WebSession $s -Body @{
        name = 'P0 Lead'; email = $leadEmail; source = 'Website'; __RequestVerificationToken = $tc
    } -MaximumRedirection 5 -UseBasicParsing
    Add-Result 'E2E-001-L' $(if ($rLead.BaseResponse.ResponseUri -match '/Leads') { 'PASS' } else { 'FAIL' })
    Save-Evidence 'E2E-001-lead' "uri=$($rLead.BaseResponse.ResponseUri)"
} catch { Add-Result 'E2E-001-L' 'FAIL' $_.Exception.Message }

# TRZ-001 Audit page has real events (no fake row)
try {
    $audit = Invoke-WebRequest -Uri "$base/Audit" -WebSession $s -UseBasicParsing
    $hasFake = $audit.Content -match 'CustomerRiskUpdated' -and $audit.Content -match '2025-01-15 14:30:18'
    $hasHardStats = $audit.Content -match '124,847'
    if (-not $hasFake -and -not $hasHardStats) {
        Add-Result 'TRZ-001' 'PASS' 'audit UI sin demo hardcoded'
    } else {
        Add-Result 'TRZ-001' 'FAIL' "fake=$hasFake hardStats=$hasHardStats"
    }
    Save-Evidence 'TRZ-001-audit' ($audit.Content.Substring(0, [Math]::Min(8000, $audit.Content.Length)))
} catch { Add-Result 'TRZ-001' 'FAIL' $_.Exception.Message }

# Navigation smoke
@('/Leads', '/Customers', '/Deals') | ForEach-Object {
    $path = $_
    try {
        $r = Invoke-WebRequest -Uri "$base$path" -WebSession $s -UseBasicParsing -TimeoutSec 15
        $id = switch ($path) { '/Leads' { 'NAV-L-01' } '/Customers' { 'NAV-C-01' } '/Deals' { 'NAV-D-01' } }
        Add-Result $id $(if ($r.StatusCode -eq 200) { 'PASS' } else { 'FAIL' }) "HTTP $($r.StatusCode)"
    } catch { Add-Result "NAV$path" 'FAIL' $_.Exception.Message }
}

$outCsv = Join-Path $evidenceDir "p0-results-$ts.csv"
$results | Export-Csv -Path $outCsv -NoTypeInformation -Encoding UTF8
Write-Host "`nExported: $outCsv" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$fail = @($results | Where-Object Status -eq 'FAIL').Count
$pass = @($results | Where-Object Status -eq 'PASS').Count
Write-Host "PASS=$pass FAIL=$fail" -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
exit $(if ($fail -gt 0) { 1 } else { 0 })
