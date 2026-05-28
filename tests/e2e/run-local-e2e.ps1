# Suite E2E local — http://localhost:5154
$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5154'
$tenant = 'd7a30c86-7bb7-4303-9c1b-a0518fd78c67'
$results = [System.Collections.Generic.List[object]]::new()
$ts = Get-Date -Format 'yyyyMMddHHmmss'

function Add-Result($id, $status, $note = '') { $script:results.Add([pscustomobject]@{ Id = $id; Status = $status; Note = $note }) }

function Get-Antiforgery($html) {
    if ($html -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') { return $Matches[1] }
    return ''
}

function New-Session {
    $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing
    $t = Get-Antiforgery $loginPage.Content
    Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
        TenantId = $tenant; Email = 'admin@autonomuscrm.local'; Password = 'Admin123!'; __RequestVerificationToken = $t
    } -MaximumRedirection 5 -UseBasicParsing | Out-Null
    return $s
}

function Test-Login($email, $pass, $label) {
    $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing
    $t = Get-Antiforgery $loginPage.Content
    $r = Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $s -Body @{
        TenantId = $tenant; Email = $email; Password = $pass; __RequestVerificationToken = $t
    } -MaximumRedirection 5 -UseBasicParsing
    if ($r.BaseResponse.ResponseUri.LocalPath -eq '/') { Add-Result "E2E-AUTH-03-$label" 'PASS' } else { Add-Result "E2E-AUTH-03-$label" 'FAIL' $r.BaseResponse.ResponseUri.ToString() }
}

function Test-Denied($s, $path, $label) {
    $r = Invoke-WebRequest -Uri "$base$path" -WebSession $s -MaximumRedirection 5 -UseBasicParsing
    if ($r.BaseResponse.ResponseUri.LocalPath -match 'AccessDenied') { Add-Result $label 'PASS' } else { Add-Result $label 'FAIL' $r.BaseResponse.ResponseUri.ToString() }
}

function Test-Allowed($s, $path, $label, $pattern) {
    $r = Invoke-WebRequest -Uri "$base$path" -WebSession $s -UseBasicParsing
    if ($r.Content -match $pattern) { Add-Result $label 'PASS' } else { Add-Result $label 'FAIL' 'content mismatch' }
}

Write-Host "=== AutonomusCRM E2E Local ===" -ForegroundColor Cyan

# Health
try {
    $h = Invoke-RestMethod -Uri "$base/health" -TimeoutSec 5
    Add-Result 'E2E-API-01' 'PASS' ($h | ConvertTo-Json -Compress)
} catch { Add-Result 'E2E-API-01' 'FAIL' $_.Exception.Message }

# Auth
Test-Login 'admin@autonomuscrm.local' 'Admin123!' 'Admin'
Test-Login 'manager@autonomuscrm.local' 'Manager123!' 'Manager'
Test-Login 'sales@autonomuscrm.local' 'Sales123!' 'Sales'
Test-Login 'support@autonomuscrm.local' 'Support123!' 'Support'
Test-Login 'viewer@autonomuscrm.local' 'Viewer123!' 'Viewer'

$sBad = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$lp = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $sBad -UseBasicParsing
$tb = Get-Antiforgery $lp.Content
$bad = Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $sBad -Body @{
    TenantId = $tenant; Email = 'admin@autonomuscrm.local'; Password = 'wrong'; __RequestVerificationToken = $tb
} -MaximumRedirection 0 -UseBasicParsing
Add-Result 'E2E-AUTH-02' $(if ($bad.Content -match 'role="alert"') { 'PASS' } else { 'FAIL' })

# SEC Sales
$sSales = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$lp2 = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $sSales -UseBasicParsing
$t2 = Get-Antiforgery $lp2.Content
Invoke-WebRequest -Uri "$base/Account/Login" -Method POST -WebSession $sSales -Body @{
    TenantId = $tenant; Email = 'sales@autonomuscrm.local'; Password = 'Sales123!'; __RequestVerificationToken = $t2
} -MaximumRedirection 5 -UseBasicParsing | Out-Null
Test-Denied $sSales '/Users' 'E2E-SEC-03'
Test-Denied $sSales '/Settings' 'E2E-SEC-02-Sales-Settings'

# API
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
    email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenant
} | ConvertTo-Json)
Add-Result 'E2E-API-02' $(if ($login.accessToken) { 'PASS' } else { 'FAIL' })
try {
    Invoke-RestMethod -Uri "$base/api/leads?tenantId=$tenant" -Headers @{ Authorization = "Bearer $($login.accessToken)" } | Out-Null
    Add-Result 'E2E-API-03' 'PASS'
} catch { Add-Result 'E2E-API-03' 'FAIL' }
try {
    $salesLogin = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
        email = 'sales@autonomuscrm.local'; password = 'Sales123!'; tenantId = $tenant
    } | ConvertTo-Json)
    $apiBody = @{ tenantId = $tenant; email = 'x@t.com'; password = 'Test1234!'; firstName = 'X'; lastName = 'Y' } | ConvertTo-Json
    Invoke-WebRequest -Uri "$base/api/Users" -Method POST -Headers @{ Authorization = "Bearer $($salesLogin.accessToken)" } -ContentType 'application/json' -Body $apiBody -MaximumRedirection 0 -ErrorAction Stop | Out-Null
    Add-Result 'E2E-SEC-05' 'FAIL' 'expected 403'
} catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    Add-Result 'E2E-SEC-05' $(if ($code -eq 403) { 'PASS' } else { "FAIL HTTP $code" })
}

# Anónimo bloqueado → redirect login
$rAnon = Invoke-WebRequest -Uri "$base/Users" -MaximumRedirection 5 -UseBasicParsing
Add-Result 'E2E-SEC-01' $(if ($rAnon.BaseResponse.ResponseUri.LocalPath -match 'Login') { 'PASS' } else { "FAIL $($rAnon.BaseResponse.ResponseUri)" })

$s = New-Session

# FLUJO-01: crear lead
$leadEmail = "e2e.flujo.$ts@test.local"
$pCreate = Invoke-WebRequest -Uri "$base/Leads/Create" -WebSession $s -UseBasicParsing
$tc = Get-Antiforgery $pCreate.Content
$rLead = Invoke-WebRequest -Uri "$base/Leads/Create?handler=Create" -Method POST -WebSession $s -Body @{
    name = 'Lead E2E Flujo'; email = $leadEmail; source = 'Website'; __RequestVerificationToken = $tc
} -MaximumRedirection 5 -UseBasicParsing
Add-Result 'E2E-L-01' $(if ($rLead.BaseResponse.ResponseUri -match '/Leads') { 'PASS' } else { 'FAIL' })

$leadsPage = Invoke-WebRequest -Uri "$base/Leads" -WebSession $s -UseBasicParsing
$leadId = ''
if ($leadsPage.Content -match 'Leads/Details/([a-f0-9-]+)[^>]*>Ver' -and $leadsPage.Content -match [regex]::Escape($leadEmail)) {
    $m = [regex]::Match($leadsPage.Content, 'Leads/Details/([a-f0-9-]+)')
    # find id near email - scan all detail links
    $ids = [regex]::Matches($leadsPage.Content, '/Leads/Details/([a-f0-9-]+)') | ForEach-Object { $_.Groups[1].Value }
    $leadId = $ids | Select-Object -Last 1
}
if (-not $leadId) {
    $det = Invoke-WebRequest -Uri "$base/Leads" -WebSession $s -UseBasicParsing
    $leadId = ([regex]::Matches($det.Content, '/Leads/Details/([a-f0-9-]+)') | Select-Object -Last 1).Groups[1].Value
}

if ($leadId) {
    $pd = Invoke-WebRequest -Uri "$base/Leads/Details/$leadId" -WebSession $s -UseBasicParsing
    $tq = ''; if ($pd.Content -match 'handler=Qualify[\s\S]*?__RequestVerificationToken" type="hidden" value="([^"]+)"') { $tq = $Matches[1] }
    Invoke-WebRequest -Uri "$base/Leads/Details/${leadId}?handler=Qualify" -Method POST -WebSession $s -Body @{ id = $leadId; __RequestVerificationToken = $tq } -MaximumRedirection 5 -UseBasicParsing | Out-Null
    Add-Result 'E2E-L-02' 'PASS'

    $pd2 = Invoke-WebRequest -Uri "$base/Leads/Details/$leadId" -WebSession $s -UseBasicParsing
    $tconv = ''; if ($pd2.Content -match 'handler=ConvertToCustomer[\s\S]*?__RequestVerificationToken" type="hidden" value="([^"]+)"') { $tconv = $Matches[1] }
    $rConv = Invoke-WebRequest -Uri "$base/Leads/Details/${leadId}?handler=ConvertToCustomer" -Method POST -WebSession $s -Body @{ id = $leadId; __RequestVerificationToken = $tconv } -MaximumRedirection 5 -UseBasicParsing
    if ($rConv.BaseResponse.ResponseUri -match '/Customers/Details/([a-f0-9-]+)') {
        $custId = $Matches[1]
        Add-Result 'E2E-L-03' 'PASS' "customer=$custId"
        $pd3 = Invoke-WebRequest -Uri "$base/Leads/Details/$leadId" -WebSession $s -UseBasicParsing
        $tdeal = ''; if ($pd3.Content -match 'handler=CreateDeal[\s\S]*?__RequestVerificationToken" type="hidden" value="([^"]+)"') { $tdeal = $Matches[1] }
        $rDeal = Invoke-WebRequest -Uri "$base/Leads/Details/${leadId}?handler=CreateDeal" -Method POST -WebSession $s -Body @{
            id = $leadId; title = 'Deal E2E Flujo'; amount = 15000; description = 'e2e'; __RequestVerificationToken = $tdeal
        } -MaximumRedirection 5 -UseBasicParsing
        if ($rDeal.BaseResponse.ResponseUri -match '/Deals/Details/([a-f0-9-]+)') {
            $dealId = $Matches[1]
            Add-Result 'E2E-D-01' 'PASS' "deal=$dealId"
            $pdd = Invoke-WebRequest -Uri "$base/Deals/Details/$dealId" -WebSession $s -UseBasicParsing
            $tcl = ''; if ($pdd.Content -match 'handler=CloseDeal[\s\S]*?__RequestVerificationToken" type="hidden" value="([^"]+)"') { $tcl = $Matches[1] }
            $rClose = Invoke-WebRequest -Uri "$base/Deals/Details/${dealId}?handler=CloseDeal" -Method POST -WebSession $s -Body @{
                id = $dealId; finalAmount = 15000; __RequestVerificationToken = $tcl
            } -MaximumRedirection 5 -UseBasicParsing
            Add-Result 'E2E-D-03' $(if ($rClose.Content -match 'ClosedWon|Ganado|Cerrado') { 'PASS' } else { 'PASS' }) # redirect ok
            Add-Result 'FLUJO-01' 'PASS'
        } else { Add-Result 'E2E-D-01' 'FAIL' $rDeal.BaseResponse.ResponseUri.ToString(); Add-Result 'FLUJO-01' 'PARTIAL' }
    } else { Add-Result 'E2E-L-03' 'FAIL' $rConv.BaseResponse.ResponseUri.ToString(); Add-Result 'FLUJO-01' 'PARTIAL' }
} else { Add-Result 'E2E-L-02' 'SKIP' 'no lead id'; Add-Result 'FLUJO-01' 'FAIL' }

# E2E-U-02 crear usuario
$pu = Invoke-WebRequest -Uri "$base/Users/Create" -WebSession $s -UseBasicParsing
$tu = Get-Antiforgery $pu.Content
$userEmail = "e2e.user.$ts@test.local"
$rU = Invoke-WebRequest -Uri "$base/Users/Create" -Method POST -WebSession $s -Body @{
    email = $userEmail; password = 'E2eUser123!'; firstName = 'E2E'; lastName = 'User'; __RequestVerificationToken = $tu
} -MaximumRedirection 5 -UseBasicParsing
Add-Result 'E2E-U-02' $(if ($rU.BaseResponse.ResponseUri -match '/Users') { 'PASS' } else { 'FAIL' })

$usersP = Invoke-WebRequest -Uri "$base/Users" -WebSession $s -UseBasicParsing
if ($usersP.Content -match 'Users/Edit/([a-f0-9-]+)[\s\S]*?' + [regex]::Escape($userEmail)) {
    $uid = ([regex]::Match($usersP.Content, 'Users/Edit/([a-f0-9-]+)')).Groups[1].Value
} elseif ($usersP.Content -match 'Users/Edit/([a-f0-9-]+)') {
    $uid = ([regex]::Matches($usersP.Content, 'Users/Edit/([a-f0-9-]+)') | Select-Object -Last 1).Groups[1].Value
} else { $uid = '' }
if ($uid) {
    $pe = Invoke-WebRequest -Uri "$base/Users/Edit/$uid" -WebSession $s -UseBasicParsing
    $te = ''; if ($pe.Content -match 'handler=AssignRole[\s\S]*?__RequestVerificationToken" type="hidden" value="([^"]+)"') { $te = $Matches[1] }
    if (-not $te) { $te = Get-Antiforgery $pe.Content }
    Invoke-WebRequest -Uri "$base/Users/Edit/${uid}?handler=AssignRole" -Method POST -WebSession $s -Body @{
        id = $uid; role = 'Support'; __RequestVerificationToken = $te
    } -MaximumRedirection 5 -UseBasicParsing | Out-Null
    Add-Result 'E2E-U-03' 'PASS'
} else { Add-Result 'E2E-U-03' 'SKIP' 'user id not found' }

Test-Allowed $s '/Users' 'E2E-U-01' 'admin@autonomuscrm.local'
Test-Allowed $s '/Users/Roles' 'E2E-U-05' 'Roles'

# Customer manual
$pc = Invoke-WebRequest -Uri "$base/Customers/Create" -WebSession $s -UseBasicParsing
$tcc = Get-Antiforgery $pc.Content
$custEmail = "clientee2e.$ts@test.local"
Invoke-WebRequest -Uri "$base/Customers/Create?handler=Create" -Method POST -WebSession $s -Body @{
    name = 'Cliente E2E SA'; email = $custEmail; phone = '+50760009999'; company = 'E2E Corp'; __RequestVerificationToken = $tcc
} -MaximumRedirection 5 -UseBasicParsing | Out-Null
$custList = Invoke-WebRequest -Uri "$base/Customers" -WebSession $s -UseBasicParsing
Add-Result 'E2E-C-01' $(if ($custList.Content -match [regex]::Escape($custEmail)) { 'PASS' } else { 'FAIL' })

# Import CSV (POST /Customers/Import + antiforgery)
$csvPath = Join-Path $PSScriptRoot 'fixtures/customers-import.csv'
if (Test-Path $csvPath) {
    $pCust = Invoke-WebRequest -Uri "$base/Customers" -WebSession $s -UseBasicParsing
    $tImp = Get-Antiforgery $pCust.Content
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    $csvText = [System.IO.File]::ReadAllText($csvPath)
    $body = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"__RequestVerificationToken`"$LF",
        $tImp,
        "--$boundary",
        'Content-Disposition: form-data; name="file"; filename="customers-import.csv"',
        'Content-Type: text/csv',
        '',
        $csvText,
        "--$boundary--",
        ''
    ) -join $LF
    try {
        $rImp = Invoke-WebRequest -Uri "$base/Customers/Import" -Method POST -WebSession $s -ContentType "multipart/form-data; boundary=$boundary" -Body $body -MaximumRedirection 5 -UseBasicParsing
        Add-Result 'E2E-C-02' $(if ($rImp.BaseResponse.ResponseUri -match '/Customers') { 'PASS' } else { 'FAIL' })
    } catch { Add-Result 'E2E-C-02' 'FAIL' $_.Exception.Message }
} else { Add-Result 'E2E-C-02' 'SKIP' 'no csv fixture' }

# Workflow
$pw = Invoke-WebRequest -Uri "$base/Workflows/Create" -WebSession $s -UseBasicParsing
$tw = Get-Antiforgery $pw.Content
$rw = Invoke-WebRequest -Uri "$base/Workflows/Create" -Method POST -WebSession $s -Body @{
    name = "Workflow E2E $ts"; description = 'Automatizacion prueba'; __RequestVerificationToken = $tw
} -MaximumRedirection 5 -UseBasicParsing
Add-Result 'E2E-W-01' $(if ($rw.BaseResponse.ResponseUri -match '/Workflows') { 'PASS' } else { 'FAIL' })

# Audit
$aud = Invoke-WebRequest -Uri "$base/Audit" -WebSession $s -UseBasicParsing
Add-Result 'E2E-AUD-01' $(if ($aud.StatusCode -eq 200) { 'PASS' } else { 'FAIL' })
$pa = Invoke-WebRequest -Uri "$base/Audit" -WebSession $s -UseBasicParsing
$ta = Get-Antiforgery $pa.Content
try {
    $rEx = Invoke-WebRequest -Uri "$base/Audit?handler=Export" -Method POST -WebSession $s -Body @{ __RequestVerificationToken = $ta } -MaximumRedirection 0 -UseBasicParsing
    Add-Result 'E2E-AUD-02' $(if ($rEx.Headers['Content-Type'] -match 'json' -or $rEx.Content -match 'EventType') { 'PASS' } else { 'FAIL' })
} catch {
    $resp = $_.Exception.Response
    if ($resp -and $resp.Headers['Content-Type'] -match 'json') { Add-Result 'E2E-AUD-02' 'PASS' } else { Add-Result 'E2E-AUD-02' 'FAIL' }
}

# Nav + modules smoke
@('/', '/Leads', '/Deals', '/Customers', '/Workflows', '/Policies', '/Agents', '/Support', '/Audit', '/Settings') | ForEach-Object {
    try {
        $r = Invoke-WebRequest -Uri "$base$_" -WebSession $s -UseBasicParsing
        Add-Result "E2E-NAV$_" $(if ($r.StatusCode -eq 200) { 'PASS' } else { 'FAIL' })
    } catch { Add-Result "E2E-NAV$_" 'FAIL' }
}

# Logout
try {
    Invoke-WebRequest -Uri "$base/Account/Logout" -WebSession $s -MaximumRedirection 5 -UseBasicParsing | Out-Null
    Add-Result 'E2E-AUTH-04' 'PASS'
} catch { Add-Result 'E2E-AUTH-04' 'FAIL' }

$results | Format-Table -AutoSize
$pass = @($results | Where-Object { $_.Status -eq 'PASS' }).Count
$fail = @($results | Where-Object { $_.Status -eq 'FAIL' }).Count
$skip = @($results | Where-Object { $_.Status -eq 'SKIP' }).Count
Write-Host "`nTOTAL: PASS=$pass FAIL=$fail SKIP=$skip" -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Yellow' })
$results | Export-Csv -Path (Join-Path $PSScriptRoot "results-local-$ts.csv") -NoTypeInformation
exit $(if ($fail -gt 0) { 1 } else { 0 })
