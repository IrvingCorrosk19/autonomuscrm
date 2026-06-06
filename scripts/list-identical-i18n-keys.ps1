$root = Join-Path $PSScriptRoot ".."
$en = Get-Content (Join-Path $root "AutonomusCRM.API\Resources\localization-en.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$es = Get-Content (Join-Path $root "AutonomusCRM.API\Resources\localization-es.json") -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($k in $en.PSObject.Properties.Name) {
    $ev = [string]$en.$k
    $sv = [string]$es.$k
    if ($ev -eq $sv -and $ev -match '[A-Za-z]{3,}') {
        Write-Output "$k|$ev"
    }
}
