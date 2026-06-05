$ErrorActionPreference = "Stop"

function Write-Resx([string]$Path, [hashtable]$Dict) {
    $lines = @(
        '<?xml version="1.0" encoding="utf-8"?>',
        '<root>',
        '  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>',
        '  <resheader name="version"><value>2.0</value></resheader>',
        '  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>',
        '  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>'
    )
    foreach ($k in ($Dict.Keys | Sort-Object)) {
        $v = [System.Security.SecurityElement]::Escape([string]$Dict[$k])
        $lines += "  <data name=""$k"" xml:space=""preserve""><value>$v</value></data>"
    }
    $lines += '</root>'
    [IO.File]::WriteAllLines($Path, $lines, [Text.UTF8Encoding]::new($false))
}

$base = Join-Path $PSScriptRoot "..\AutonomusCRM.API\Resources"
$esPath = Join-Path $base "localization-es.json"
$enPath = Join-Path $base "localization-en.json"

if (-not (Test-Path $esPath)) { throw "Missing $esPath" }

function ConvertFrom-JsonToHashtable([string]$Json) {
    $obj = $Json | ConvertFrom-Json
    $ht = @{}
    $obj.PSObject.Properties | ForEach-Object { $ht[$_.Name] = [string]$_.Value }
    return $ht
}
$es = ConvertFrom-JsonToHashtable (Get-Content $esPath -Raw -Encoding UTF8)
$en = ConvertFrom-JsonToHashtable (Get-Content $enPath -Raw -Encoding UTF8)

Write-Resx (Join-Path $base "SharedResource.es.resx") $es
Write-Resx (Join-Path $base "SharedResource.en.resx") $en

$valEs = @{
    Required = 'El campo {0} es obligatorio.'
    StringLength = 'El campo {0} debe tener entre {2} y {1} caracteres.'
    EmailAddress = 'El campo {0} no es una dirección de email válida.'
    Range = 'El valor de {0} debe estar entre {1} y {2}.'
}
$valEn = @{
    Required = 'The {0} field is required.'
    StringLength = 'The {0} field must be between {2} and {1} characters.'
    EmailAddress = 'The {0} field is not a valid email address.'
    Range = 'The {0} value must be between {1} and {2}.'
}
Write-Resx (Join-Path $base "ValidationMessages.es.resx") $valEs
Write-Resx (Join-Path $base "ValidationMessages.en.resx") $valEn
Write-Host "OK: $($es.Count) keys"
