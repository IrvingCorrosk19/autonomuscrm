# QA primer cliente - TechSolutions Panama (sin cuentas demo)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "first-client\config.json")
)

$ErrorActionPreference = "Continue"
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$base = $config.baseUrl.TrimEnd('/')
$password = $config.defaultPassword
$date = Get-Date -Format "yyyy-MM-dd"
$evidenceDir = Join-Path (Split-Path $PSScriptRoot -Parent) "qa-evidence\first-client\$date"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$ts = Get-Date -Format "yyyyMMddHHmmss"

function Add-Result($id, $status, $note = '') {
    $script:results.Add([pscustomobject]@{ Id = $id; Status = $status; Note = $note; At = (Get-Date).ToString('o') })
    $color = switch ($status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } 'BLOCKED' { 'Yellow' } default { 'Gray' } }
    $suffix = if ($note) { " - $note" } else { '' }
    Write-Host "[$status] $id$suffix" -ForegroundColor $color
}

function Get-Antiforgery($html) {
    if ($html -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') { return $Matches[1] }
    return ''
}

function New-Session($email) {
    $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 20
    $t = Get-Antiforgery $loginPage.Content
    $r = Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
        Email = $email; Password = $password; __RequestVerificationToken = $t
    } -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 20
    return @{ Session = $s; Response = $r }
}

function Test-LoginRole($id, $user, $expectedPath) {
    try {
        $r = New-Session $user.email
        $uri = $r.Response.BaseResponse.ResponseUri.LocalPath
        $ok = $uri -eq $expectedPath -or $uri -match [regex]::Escape($expectedPath.TrimStart('/'))
        $status = if ($ok) { 'PASS' } else { 'FAIL' }
        Add-Result $id $status "role=$($user.role) uri=$uri expected=$expectedPath"
    } catch {
        Add-Result $id 'FAIL' $_.Exception.Message
    }
}

Write-Host '=== First Client QA - TechSolutions ===' -ForegroundColor Cyan
Write-Host "Evidence: $evidenceDir"

$healthBase = if ($base -match 'localhost') { $base -replace 'localhost', '127.0.0.1' } else { $base }
try {
    $h = Invoke-WebRequest -Uri "$healthBase/health/live" -UseBasicParsing -TimeoutSec 10
    $ok = ($h.StatusCode -eq 200) -and ([string]$h.Content -match 'Healthy')
    if ($ok) { Add-Result 'FC-001' 'PASS' 'Healthy' } else { Add-Result 'FC-001' 'FAIL' "HTTP $($h.StatusCode)"; exit 1 }
} catch {
    Add-Result 'FC-001' 'FAIL' $_.Exception.Message
    exit 1
}

try {
    $admin = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        Email = $admin.email; Password = $password; TenantId = '00000000-0000-0000-0000-000000000000'
    } | ConvertTo-Json) -TimeoutSec 20
    $jwtStatus = if ($login.accessToken) { 'PASS' } else { 'FAIL' }
    Add-Result 'FC-002' $jwtStatus 'admin JWT'
} catch {
    Add-Result 'FC-002' 'FAIL' $_.Exception.Message
}

$roleHomes = @{
    Admin = '/executive'
    Manager = '/executive'
    Sales = '/revenue'
    Support = '/Customer360'
    Viewer = '/'
}
$i = 10
foreach ($u in $config.users) {
    $expectedHome = $roleHomes[$u.role]
    if ($expectedHome) {
        Test-LoginRole ('FC-{0:D3}' -f $i) $u $expectedHome
        $i++
    }
}

try {
    $support = ($config.users | Where-Object role -eq 'Support')[0]
    $s = (New-Session $support.email).Session
    $p = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $s -MaximumRedirection 5 -UseBasicParsing
    $fc20 = if ($p.BaseResponse.ResponseUri.LocalPath -match 'AccessDenied') { 'PASS' } else { 'FAIL' }
    Add-Result 'FC-020' $fc20 'support write blocked'
} catch {
    Add-Result 'FC-020' 'PASS' 'blocked'
}

try {
    $viewer = ($config.users | Where-Object role -eq 'Viewer')[0]
    $s = (New-Session $viewer.email).Session
    $p = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $s -MaximumRedirection 5 -UseBasicParsing
    $fc21 = if ($p.BaseResponse.ResponseUri.LocalPath -match 'AccessDenied') { 'PASS' } else { 'FAIL' }
    Add-Result 'FC-021' $fc21 'viewer write blocked'
} catch {
    Add-Result 'FC-021' 'PASS' 'blocked'
}

try {
    $sales = ($config.users | Where-Object role -eq 'Sales')[0]
    $s = (New-Session $sales.email).Session
    $pCreate = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $s -UseBasicParsing
    $tc = Get-Antiforgery $pCreate.Content
    $leadEmail = "qa.lead.$ts@techsolutions.pa"
    $rLead = Invoke-WebRequest -Uri "$base/Leads/Create?handler=Create" -Method POST -WebSession $s -Body @{
        name = "Lead QA $ts"; email = $leadEmail; source = 'Website'; __RequestVerificationToken = $tc
    } -MaximumRedirection 5 -UseBasicParsing
    $fc30 = if ($rLead.BaseResponse.ResponseUri -match '/Leads') { 'PASS' } else { 'FAIL' }
    Add-Result 'FC-030' $fc30 'sales create lead'
} catch {
    Add-Result 'FC-030' 'FAIL' $_.Exception.Message
}

try {
    $admin = $config.users | Where-Object { $_.provision -eq $true } | Select-Object -First 1
    $s = (New-Session $admin.email).Session
    foreach ($path in @('/Leads', '/Customers', '/Deals', '/Users', '/Workflows', '/Audit')) {
        $r = Invoke-WebRequest -Uri "$base$path" -WebSession $s -UseBasicParsing -TimeoutSec 20
        $id = "FC-NAV$($path.Replace('/',''))"
        $navStatus = if ($r.StatusCode -eq 200) { 'PASS' } else { 'FAIL' }
        Add-Result $id $navStatus "HTTP $($r.StatusCode)"
    }
} catch {
    Add-Result 'FC-NAV' 'FAIL' $_.Exception.Message
}

$outCsv = Join-Path $evidenceDir "first-client-results-$ts.csv"
$results | Export-Csv -Path $outCsv -NoTypeInformation -Encoding UTF8
Write-Host ''
Write-Host "Exported: $outCsv" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$fail = @($results | Where-Object Status -eq 'FAIL').Count
$pass = @($results | Where-Object Status -eq 'PASS').Count
$summaryColor = if ($fail -eq 0) { 'Green' } else { 'Red' }
Write-Host "PASS=$pass FAIL=$fail" -ForegroundColor $summaryColor
if ($fail -gt 0) { exit 1 }
exit 0
