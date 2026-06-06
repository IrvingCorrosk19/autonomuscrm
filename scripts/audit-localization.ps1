$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot ".."
$enPath = Join-Path $root "AutonomusCRM.API\Resources\localization-en.json"
$esPath = Join-Path $root "AutonomusCRM.API\Resources\localization-es.json"

$enJson = Get-Content $enPath -Raw -Encoding UTF8 | ConvertFrom-Json
$esJson = Get-Content $esPath -Raw -Encoding UTF8 | ConvertFrom-Json
$enKeys = @($enJson.PSObject.Properties.Name)
$esKeys = @($esJson.PSObject.Properties.Name)

$missingEs = $enKeys | Where-Object { $_ -notin $esKeys }
$missingEn = $esKeys | Where-Object { $_ -notin $enKeys }
$same = 0
$sameSamples = @()
foreach ($k in $enKeys) {
    if ($k -notin $esKeys) { continue }
    $ev = [string]$enJson.$k
    $sv = [string]$esJson.$k
    if ($ev -eq $sv -and $ev -match '[A-Za-z]{4,}') {
        $same++
        if ($sameSamples.Count -lt 25) { $sameSamples += "$k|$ev" }
    }
}

$cshtml = Get-ChildItem (Join-Path $root "AutonomusCRM.API\Pages") -Filter "*.cshtml" -Recurse
$withL = ($cshtml | Where-Object { Select-String -Path $_.FullName -Pattern 'L\[' -Quiet }).Count
$totalCshtml = $cshtml.Count

Write-Output "JSON_EN_KEYS=$($enKeys.Count)"
Write-Output "JSON_ES_KEYS=$($esKeys.Count)"
Write-Output "JSON_MISSING_ES=$($missingEs.Count)"
Write-Output "JSON_MISSING_EN=$($missingEn.Count)"
Write-Output "JSON_IDENTICAL_EN_ES=$same"
Write-Output "CSHTML_TOTAL=$totalCshtml"
Write-Output "CSHTML_WITH_L=$withL"
Write-Output "SAMPLES_START"
$sameSamples | ForEach-Object { Write-Output $_ }
Write-Output "SAMPLES_END"
