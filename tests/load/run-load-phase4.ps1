# Fase 4 — load smoke (requiere API en http://localhost:5154)
$base = 'http://localhost:5154'
$tenant = 'd7a30c86-7bb7-4303-9c1b-a0518fd78c67'
$concurrency = 20
$requestsPerWorker = 10
$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Timed($name, $scriptBlock) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $scriptBlock
        $sw.Stop()
        $script:results.Add([pscustomobject]@{ Test = $name; Ms = $sw.ElapsedMilliseconds; Ok = $true })
    } catch {
        $sw.Stop()
        $script:results.Add([pscustomobject]@{ Test = $name; Ms = $sw.ElapsedMilliseconds; Ok = $false; Err = $_.Exception.Message })
    }
}

Write-Host "=== Load Phase 4 (concurrency=$concurrency) ===" -ForegroundColor Cyan

# Login burst
$loginJobs = 1..$concurrency | ForEach-Object {
    Start-Job -ScriptBlock {
        param($b, $t)
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            Invoke-RestMethod -Uri "$b/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
                email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $t
            } | ConvertTo-Json) | Out-Null
            $sw.Stop()
            [pscustomobject]@{ Ok = $true; Ms = $sw.ElapsedMilliseconds }
        } catch {
            $sw.Stop()
            [pscustomobject]@{ Ok = $false; Ms = $sw.ElapsedMilliseconds; Err = $_.Exception.Message }
        }
    } -ArgumentList $base, $tenant
}
$loginResults = $loginJobs | Wait-Job | Receive-Job
$loginJobs | Remove-Job
$loginOk = @($loginResults | Where-Object Ok).Count
Write-Host "Login concurrent: $loginOk / $concurrency OK" -ForegroundColor $(if ($loginOk -eq $concurrency) { 'Green' } else { 'Yellow' })

$token = (Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{
    email = 'admin@autonomuscrm.local'; password = 'Admin123!'; tenantId = $tenant
} | ConvertTo-Json)).accessToken

# API read burst
$readJobs = 1..$concurrency | ForEach-Object {
    Start-Job -ScriptBlock {
        param($b, $t, $tok, $n)
        $ok = 0; $totalMs = 0
        for ($i = 0; $i -lt $n; $i++) {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                Invoke-RestMethod -Uri "$b/api/leads?tenantId=$t" -Headers @{ Authorization = "Bearer $tok" } | Out-Null
                $sw.Stop(); $totalMs += $sw.ElapsedMilliseconds; $ok++
            } catch { $sw.Stop() }
        }
        [pscustomobject]@{ Ok = $ok; TotalMs = $totalMs; N = $n }
    } -ArgumentList $base, $tenant, $token, $requestsPerWorker
}
$readResults = $readJobs | Wait-Job | Receive-Job
$readJobs | Remove-Job
$totalReads = ($readResults | Measure-Object -Property Ok -Sum).Sum
$maxMs = ($readResults | ForEach-Object { [int]($_.TotalMs / $_.N) } | Measure-Object -Maximum).Maximum
Write-Host "GET leads: $totalReads / $($concurrency * $requestsPerWorker) OK, avg max ~${maxMs}ms per worker" -ForegroundColor Cyan

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) "qa-evidence\$(Get-Date -Format 'yyyy-MM-dd')\load"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$out = Join-Path $outDir "load-phase4-$(Get-Date -Format 'yyyyMMddHHmmss').csv"
$results | Export-Csv $out -NoTypeInformation
Write-Host "Evidence: $out"
