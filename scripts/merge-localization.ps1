$ErrorActionPreference = "Stop"
$base = Join-Path $PSScriptRoot "..\AutonomusCRM.API\Resources"

function Merge-Json([string]$Target, [string]$Extension) {
    $targetObj = Get-Content $Target -Raw -Encoding UTF8 | ConvertFrom-Json
    $extObj = Get-Content $Extension -Raw -Encoding UTF8 | ConvertFrom-Json
    $ht = [ordered]@{}
    foreach ($p in $targetObj.PSObject.Properties) { $ht[$p.Name] = $p.Value }
    foreach ($p in $extObj.PSObject.Properties) { $ht[$p.Name] = $p.Value }
    $ht | ConvertTo-Json -Depth 5 | Set-Content $Target -Encoding UTF8
}

Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext-en.json")
Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext2-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext2-en.json")
Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext3-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext3-en.json")
Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext4-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext4-en.json")
Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext5-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext5-en.json")
Merge-Json (Join-Path $base "localization-es.json") (Join-Path $PSScriptRoot "localization-ext6-es.json")
Merge-Json (Join-Path $base "localization-en.json") (Join-Path $PSScriptRoot "localization-ext6-en.json")
& (Join-Path $PSScriptRoot "generate-localization-resx.ps1")
Write-Host "Merged and regenerated resx."
