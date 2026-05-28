# Fase 3 — Hardening enterprise QA
$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5154'
$tenantA = 'd7a30c86-7bb7-4303-9c1b-a0518fd78c67'
$tenantB = 'a8f41d97-8cc8-5414-0a2c-b1629fe89d78'
$date = Get-Date -Format 'yyyy-MM-dd'
$evidenceDir = Join-Path (Split-Path $PSScriptRoot -Parent) "qa-evidence\$date\phase3"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$ts = Get-Date -Format 'yyyyMMddHHmmss'

function Add-R($id, $status, $note = '') {
    $script:results.Add([pscustomobject]@{ Id = $id; Status = $status; Note = $note })
    Write-Host "[$status] $id $(if ($note){"- $note"})" -ForegroundColor $(if ($status -eq 'PASS'){'Green'}elseif($status -eq 'FAIL'){'Red'}else{'Yellow'})
}

Write-Host "=== Phase 3 QA ===" -ForegroundColor Cyan

# Health + correlation header
try {
    $h = Invoke-WebRequest -Uri "$base/health" -Headers @{ 'X-Correlation-Id' = "phase3-$ts" } -UseBasicParsing
    $corr = $h.Headers['X-Correlation-Id']
    Add-R 'OBS-001' $(if ($corr) { 'PASS' } else { 'FAIL' }) "corr=$corr"
} catch { Add-R 'OBS-001' 'FAIL' $_.Exception.Message }

# TEN: login tenant B admin
try {
    $loginB = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin-b@qa.autonomusflow.local'; password = 'Admin123!'; tenantId = $tenantB
    } | ConvertTo-Json)
    Add-R 'TEN-B-LOGIN' $(if ($loginB.accessToken) { 'PASS' } else { 'FAIL' })
} catch { Add-R 'TEN-B-LOGIN' 'FAIL' $_.Exception.Message; $loginB = $null }

# TEN: exclusive data in B not visible in A
if ($loginB) {
    try {
        $leadsB = Invoke-RestMethod -Uri "$base/api/leads?tenantId=$tenantB" -Headers @{ Authorization = "Bearer $($loginB.accessToken)" }
        $hasExclusive = @($leadsB | Where-Object { $_.name -match 'EXCLUSIVO QA-B' }).Count -gt 0
        Add-R 'TEN-B-DATA' $(if ($hasExclusive) { 'PASS' } else { 'FAIL' }) 'lead exclusive'
    } catch { Add-R 'TEN-B-DATA' 'FAIL' $_.Exception.Message }

    $loginA = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenantA
    } | ConvertTo-Json)
    try {
        $leadsWrong = Invoke-RestMethod -Uri "$base/api/leads?tenantId=$tenantB" -Headers @{ Authorization = "Bearer $($loginA.accessToken)" } -ErrorAction Stop
        Add-R 'TEN-CROSS-QUERY' 'FAIL' 'tenant A read B via query'
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        Add-R 'TEN-CROSS-QUERY' $(if ($code -eq 403) { 'PASS' } else { 'FAIL' }) "HTTP $code"
    }

    # IDOR: lead id from B with token A
    if ($leadsB -and $leadsB.Count -gt 0) {
        $lid = $leadsB[0].id
        try {
            Invoke-RestMethod -Uri "$base/api/leads/$($lid)?tenantId=$tenantA" -Headers @{ Authorization = "Bearer $($loginA.accessToken)" } -ErrorAction Stop
            Add-R 'TEN-IDOR-LEAD' 'FAIL' 'returned cross tenant'
        } catch {
            $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
            Add-R 'TEN-IDOR-LEAD' $(if ($code -eq 404 -or $code -eq 403) { 'PASS' } else { 'FAIL' }) "HTTP $code"
        }
    }
}

# JWT tampering
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenantA
    } | ConvertTo-Json)
    $bad = $login.accessToken + 'x'
    try {
        Invoke-RestMethod -Uri "$base/api/leads?tenantId=$tenantA" -Headers @{ Authorization = "Bearer $bad" } -ErrorAction Stop
        Add-R 'OWASP-JWT' 'FAIL'
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        Add-R 'OWASP-JWT' $(if ($code -eq 401) { 'PASS' } else { 'FAIL' }) "HTTP $code"
    }
} catch { Add-R 'OWASP-JWT' 'BLOCKED' $_.Exception.Message }

# Import guard — empty file simulation via API not available; skip file upload in script
Add-R 'IMP-SKIP-UI' 'SKIP' 'use manual upload tests/qa-data'

# Concurrency deal stage
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenantA
    } | ConvertTo-Json)
    $deals = Invoke-RestMethod -Uri "$base/api/deals?tenantId=$tenantA" -Headers @{ Authorization = "Bearer $($login.accessToken)" }
    if ($deals -and $deals.Count -gt 0) {
        $did = $deals[0].id
        $v = $deals[0].version
        $uri = "$base/api/deals/$did/stage?tenantId=$tenantA"
        $body = @{ stage = 'Qualification'; expectedVersion = $v } | ConvertTo-Json
        # API may not expose stage endpoint — use UI path via qualify if missing
        Add-R 'CONC-DEAL' 'SKIP' 'API stage endpoint optional'
    }
} catch { Add-R 'CONC-DEAL' 'BLOCKED' $_.Exception.Message }

# Security headers
try {
    $r = Invoke-WebRequest -Uri "$base/Account/Login" -UseBasicParsing
    $hasHeaders = ($r.Headers['X-Content-Type-Options']) -and ($r.Headers['X-Frame-Options'] -or $r.Headers['Content-Security-Policy'])
    Add-R 'OWASP-HEADERS' $(if ($hasHeaders) { 'PASS' } else { 'FAIL' })
} catch { Add-R 'OWASP-HEADERS' 'FAIL' $_.Exception.Message }

$out = Join-Path $evidenceDir "phase3-$ts.csv"
$results | Export-Csv $out -NoTypeInformation
$fail = @($results | Where-Object Status -eq 'FAIL').Count
Write-Host "Evidence: $out | FAIL=$fail"
exit $(if ($fail -gt 0) { 1 } else { 0 })
