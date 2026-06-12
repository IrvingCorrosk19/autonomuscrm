param(
    [string]$BaseUrl = "http://localhost:5154",
    [int[]]$ConcurrencyLevels = @(50, 100, 250, 500),
    [string]$OutputDir = "ops/certification/results"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$endpoints = @("/health", "/health/live", "/health/ready")
$report = [System.Collections.Generic.List[object]]::new()

function Test-EndpointLoad {
    param([string]$Path, [int]$Concurrency)

    $url = "$BaseUrl$Path"
    $sem = [System.Threading.SemaphoreSlim]::new($Concurrency)
    $levelSw = [System.Diagnostics.Stopwatch]::StartNew()
    $tasks = @()

    for ($i = 0; $i -lt $Concurrency; $i++) {
        $tasks += [System.Threading.Tasks.Task]::Run({
            $null = $using:sem.Wait()
            try {
                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                $r = Invoke-WebRequest -Uri $using:url -UseBasicParsing -TimeoutSec 60
                return [PSCustomObject]@{
                    Ok = ($r.StatusCode -ge 200 -and $r.StatusCode -lt 300)
                    Ms = $sw.ElapsedMilliseconds
                }
            }
            catch {
                return [PSCustomObject]@{ Ok = $false; Ms = 0 }
            }
            finally { $null = $using:sem.Release() }
        })
    }

    [System.Threading.Tasks.Task]::WaitAll($tasks)
    $levelSw.Stop()
    $results = $tasks | ForEach-Object { $_.Result }
    $ok = @($results | Where-Object { $_.Ok }).Count
    $ms = @($results | ForEach-Object { $_.Ms } | Sort-Object)

    return [PSCustomObject]@{
        Endpoint = $Path
        Concurrency = $Concurrency
        TotalRequests = $Concurrency
        Success = $ok
        Errors = $Concurrency - $ok
        DurationSec = [math]::Round($levelSw.Elapsed.TotalSeconds, 2)
        ThroughputRps = if ($levelSw.Elapsed.TotalSeconds -gt 0) { [math]::Round($ok / $levelSw.Elapsed.TotalSeconds, 1) } else { 0 }
        LatencyP50Ms = if ($ms.Count) { $ms[[int]($ms.Count * 0.5)] } else { 0 }
        LatencyP95Ms = if ($ms.Count) { $ms[[min]([int]($ms.Count * 0.95), $ms.Count - 1)] } else { 0 }
        LatencyMaxMs = if ($ms.Count) { $ms[-1] } else { 0 }
    }
}

foreach ($level in $ConcurrencyLevels) {
    foreach ($ep in $endpoints) {
        Write-Host "Load: $ep @ $level concurrent"
        $report.Add((Test-EndpointLoad -Path $ep -Concurrency $level))
    }
}

$mem = Get-CimInstance Win32_OperatingSystem
$report | Format-Table -AutoSize
$outFile = Join-Path $OutputDir ("load-test-{0:yyyyMMdd-HHmmss}.json" -f (Get-Date))
$report | ConvertTo-Json -Depth 4 | Set-Content $outFile
Write-Host "Saved: $outFile"
Write-Host ("RAM free GB: {0:N1}" -f ($mem.FreePhysicalMemory / 1MB))
