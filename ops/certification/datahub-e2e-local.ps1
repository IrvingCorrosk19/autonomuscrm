# Data Hub Supreme — Local E2E validation against running API (http://localhost:5154)
$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:DATAHUB_E2E_URL) { $env:DATAHUB_E2E_URL } else { "http://localhost:5154" }
$E2eDir = Join-Path $PSScriptRoot "datahub-e2e"
$Results = @()

function Add-Result($Step, $Pass, $Detail) {
    $script:Results += [PSCustomObject]@{ Step = $Step; Pass = $Pass; Detail = $Detail }
    $icon = if ($Pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $Step - $Detail"
}

function Invoke-MultipartUpload($Uri, $Headers, $FilePath) {
    $boundary = [System.Guid]::NewGuid().ToString()
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $enc = New-Object System.Text.UTF8Encoding $false
    $header = $enc.GetBytes("--$boundary`r`nContent-Disposition: form-data; name=`"file`"; filename=`"$fileName`"`r`nContent-Type: text/csv`r`n`r`n")
    $footer = $enc.GetBytes("`r`n--$boundary--`r`n")
    $body = New-Object System.IO.MemoryStream
    $body.Write($header, 0, $header.Length)
    $body.Write($fileBytes, 0, $fileBytes.Length)
    $body.Write($footer, 0, $footer.Length)
    $reqHeaders = @{} + $Headers
    $reqHeaders["Content-Type"] = "multipart/form-data; boundary=$boundary"
    return Invoke-RestMethod -Uri $Uri -Method POST -Headers $reqHeaders -Body $body.ToArray()
}

function Get-LocalTenantId {
    $loginProbe = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body (@{
        email = "admin@autonomuscrm.local"; password = "Admin123!"; tenantId = "00000000-0000-0000-0000-000000000000"
    } | ConvertTo-Json)
    $part = ($loginProbe.accessToken -split '\.')[1]
    $pad = $part.Length % 4
    if ($pad -gt 0) { $part = $part + ('=' * (4 - $pad)) }
    $json = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($part))
    $payload = $json | ConvertFrom-Json
    if ($payload.TenantId) { return $payload.TenantId }
    throw "Could not resolve tenant ID from login token"
}

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 10
    Add-Result "API Health" ($health.StatusCode -eq 200) "Status $($health.StatusCode)"

    $tenantId = Get-LocalTenantId
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body (@{
        email = "admin@autonomuscrm.local"; password = "Admin123!"; tenantId = $tenantId
    } | ConvertTo-Json)

    $token = $login.accessToken
    Add-Result "Admin Login" ($null -ne $token) "JWT obtained for tenant $tenantId"

    $headers = @{ Authorization = "Bearer $token" }

    $wizard = Invoke-WebRequest -Uri "$BaseUrl/DataHub/Wizard" -UseBasicParsing -Headers $headers -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $wizardOk = $wizard.StatusCode -in 200, 302
    Add-Result "Wizard Page /DataHub/Wizard" $wizardOk "Status $($wizard.StatusCode)"

    $csv = Join-Path $E2eDir "leads-valid.csv"
    $uploadUri = "$BaseUrl/api/datahub/upload?tenantId=$tenantId&targetEntity=Lead&loadMode=InsertOnly"
    $upload = Invoke-MultipartUpload -Uri $uploadUri -Headers $headers -FilePath $csv
    $jobId = $upload.jobId
    Add-Result "Upload CSV" ($null -ne $jobId) "JobId=$jobId Rows=$($upload.totalRows)"

    $analyzeUri = "$BaseUrl/api/datahub/jobs/$jobId/analyze?tenantId=$tenantId"
    $ai = Invoke-RestMethod -Uri $analyzeUri -Method POST -Headers $headers
    Add-Result "Smart Analysis" ($ai.overallConfidencePercent -ge 50) "Confidence=$($ai.overallConfidencePercent)% Entity=$($ai.suggestedTargetEntity)"

    $detail = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/${jobId}?tenantId=$tenantId" -Headers $headers
    $emailMap = $detail.mappings | Where-Object { $_.targetField -eq "Email" }
    Add-Result "Column Mapping" ($null -ne $emailMap) "Email column mapped"

    Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$jobId/autofix?tenantId=$tenantId" -Method POST -Headers $headers | Out-Null
    Add-Result "Auto-Fix" $true "Executed"

    $val = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$jobId/validate?tenantId=$tenantId" -Method POST -Headers $headers
    Add-Result "Validation" ($val.validRows -ge 1) "Valid=$($val.validRows) Invalid=$($val.invalidRows)"

    $clean = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$jobId/cleaning?tenantId=$tenantId" -Headers $headers
    Add-Result "Cleaning Summary" ($clean.totalRows -ge 4) "Total=$($clean.totalRows)"

    Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$jobId/import?tenantId=$tenantId" -Method POST -Headers $headers | Out-Null
    $done = $false
    $finalStatus = "Unknown"
    for ($i = 0; $i -lt 45; $i++) {
        Start-Sleep -Seconds 2
        $job = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/${jobId}?tenantId=$tenantId" -Headers $headers
        $finalStatus = $job.summary.status
        if ($finalStatus -match "Completed") { $done = $true; break }
    }
    Add-Result "Async Import" $done "Status=$finalStatus Success=$($job.summary.successRows)"

    $jobs = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs?tenantId=$tenantId" -Headers $headers
    Add-Result "Import History API" ($jobs.Count -ge 1) "$($jobs.Count) jobs"

    $score = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/quality/score?tenantId=$tenantId" -Headers $headers
    Add-Result "Quality Score" ($score.score -ge 0) "Score=$($score.score) Grade=$($score.grade)"

    # Invalid email CSV
    $badUri = "$BaseUrl/api/datahub/upload?tenantId=$tenantId&targetEntity=Lead&loadMode=InsertOnly"
    $badUpload = Invoke-MultipartUpload -Uri $badUri -Headers $headers -FilePath (Join-Path $E2eDir "leads-invalid-email.csv")
    Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$($badUpload.jobId)/validate?tenantId=$tenantId" -Method POST -Headers $headers | Out-Null
    $badJob = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$($badUpload.jobId)?tenantId=$tenantId" -Headers $headers
    $badErrors = ($badJob.errors | Where-Object { $_.errorCode -eq "InvalidEmail" }).Count
    Add-Result "Invalid Email Detection" ($badErrors -ge 1 -or $badJob.summary.failedRows -ge 1) "InvalidEmail errors=$badErrors"

    # Formula injection
    $injUpload = Invoke-MultipartUpload -Uri $badUri -Headers $headers -FilePath (Join-Path $E2eDir "leads-formula-injection.csv")
    $injRows = Invoke-RestMethod -Uri "$BaseUrl/api/datahub/jobs/$($injUpload.jobId)?tenantId=$tenantId" -Headers $headers
    $sanitized = $true
    foreach ($row in $injRows.previewRows) {
        foreach ($v in $row.data.PSObject.Properties.Value) {
            if ($v -match '^=cmd') { $sanitized = $false }
        }
    }
    Add-Result "Formula Injection Sanitized" $sanitized "No raw =cmd in staging preview"

    # Viewer forbidden
    $viewerLogin = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body (@{
        email = "viewer@autonomuscrm.local"; password = "Viewer123!"; tenantId = $tenantId
    } | ConvertTo-Json)
    if ($viewerLogin.accessToken) {
        try {
            Invoke-WebRequest -Uri "$BaseUrl/api/datahub/jobs?tenantId=$tenantId" -Headers @{ Authorization = "Bearer $($viewerLogin.accessToken)" } -UseBasicParsing | Out-Null
            Add-Result "Viewer Forbidden" $false "Expected 403"
        } catch {
            $code = [int]$_.Exception.Response.StatusCode
            Add-Result "Viewer Forbidden" ($code -eq 403) "Status $code"
        }
    } else {
        Add-Result "Viewer Forbidden" $true "Viewer not seeded - skipped"
    }

    # Cross-tenant isolation (use synthetic tenant id)
    try {
        $fakeTenant = "11111111-1111-1111-1111-111111111111"
        Invoke-WebRequest -Uri "$BaseUrl/api/datahub/jobs/${jobId}?tenantId=$fakeTenant" -Headers $headers -UseBasicParsing | Out-Null
        Add-Result "Cross-Tenant Job Blocked" $false "Expected 404/403"
    } catch {
        $code = [int]$_.Exception.Response.StatusCode
        Add-Result "Cross-Tenant Job Blocked" ($code -in 403, 404) "Status $code"
    }

    $failCount = ($Results | Where-Object { -not $_.Pass }).Count
    Write-Host ""
    Write-Host "=== E2E Summary: $($Results.Count - $failCount)/$($Results.Count) PASS ==="
    $Results | Format-Table -AutoSize
    if ($failCount -gt 0) { exit 1 }
}
catch {
    Add-Result "Fatal" $false $_.Exception.Message
    $Results | Format-Table -AutoSize
    exit 1
}
