# RC Zero — Gate 8 smoke test (all operational routes)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "first-client\config.json")
)

$ErrorActionPreference = 'Continue'
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$base = $config.baseUrl.TrimEnd('/')
$password = $config.defaultPassword
$date = Get-Date -Format 'yyyy-MM-dd'
$evidenceDir = Join-Path (Split-Path $PSScriptRoot -Parent) "qa-evidence\rc-zero\$date"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$ts = Get-Date -Format 'yyyyMMddHHmmss'

function Add-R($id, $status, $note = '') {
    $script:results.Add([pscustomobject]@{ Id = $id; Status = $status; Note = $note })
    $c = switch ($status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Yellow' } }
    Write-Host "[$status] $id $(if ($note) { "- $note" })" -ForegroundColor $c
}

function Get-Antiforgery($html) {
    if ($html -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') { return $Matches[1] }
    return ''
}

function New-AdminSession {
    $admin = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
    $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 25
    $t = Get-Antiforgery $loginPage.Content
    Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
        Email = $admin.email; Password = $password; __RequestVerificationToken = $t
    } -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 25 | Out-Null
    return $s
}

$smokeRoutes = @(
    '/', '/executive', '/revenue', '/TrustInbox', '/Customer360',
    '/Leads', '/Customers', '/Deals', '/Tasks', '/Users', '/Policies', '/Settings',
    '/Integrations', '/Audit', '/billing', '/Workflows', '/Memory', '/customer-success',
    '/Leads/Create', '/Customers/Create', '/Deals/Create', '/Users/Create'
)

Write-Host '=== RC Zero Smoke Test ===' -ForegroundColor Cyan

$healthBase = if ($base -match 'localhost') { $base -replace 'localhost', '127.0.0.1' } else { $base }
try {
    $h = Invoke-WebRequest -Uri "$healthBase/health/live" -UseBasicParsing -TimeoutSec 15
    $ok = ($h.StatusCode -eq 200) -and ([string]$h.Content -match 'Healthy')
    if ($ok) { Add-R 'SMK-HEALTH' 'PASS' 'Healthy' } else { Add-R 'SMK-HEALTH' 'FAIL' "HTTP $($h.StatusCode)"; exit 1 }
} catch {
    Add-R 'SMK-HEALTH' 'FAIL' $_.Exception.Message
    exit 1
}

try {
    $s = New-AdminSession
    foreach ($path in $smokeRoutes) {
        $id = 'SMK-' + ($path.Trim('/') -replace '[/]', '-' -replace '^$', 'home')
        try {
            $r = Invoke-WebRequest -Uri "$base$path" -WebSession $s -UseBasicParsing -TimeoutSec 25 -MaximumRedirection 5
            $code = [int]$r.StatusCode
            $has500 = $r.Content -match 'Status Code:\s*500|Internal Server Error|NullReferenceException|SqlException'
            $has404 = $code -eq 404 -or $r.Content -match '404|Not Found'
            if ($has500) { Add-R $id 'FAIL' '500 or exception in body' }
            elseif ($has404) { Add-R $id 'FAIL' '404' }
            elseif ($code -ge 200 -and $code -lt 400) { Add-R $id 'PASS' "HTTP $code" }
            else { Add-R $id 'FAIL' "HTTP $code" }
        } catch {
            $msg = $_.Exception.Message
            if ($msg -match '404') { Add-R $id 'FAIL' '404' }
            elseif ($msg -match '500') { Add-R $id 'FAIL' '500' }
            else { Add-R $id 'FAIL' $msg }
        }
    }
} catch {
    Add-R 'SMK-SESSION' 'FAIL' $_.Exception.Message
}

$out = Join-Path $evidenceDir "rc-smoke-$ts.csv"
$results | Export-Csv $out -NoTypeInformation -Encoding UTF8
$fail = @($results | Where-Object Status -eq 'FAIL').Count
$pass = @($results | Where-Object Status -eq 'PASS').Count
Write-Host "Evidence: $out | PASS=$pass FAIL=$fail" -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
exit $(if ($fail -gt 0) { 1 } else { 0 })
