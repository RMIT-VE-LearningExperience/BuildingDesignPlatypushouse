param(
    [Parameter(Mandatory)][string] $IfcConvert,
    [string] $Source = (Join-Path $PSScriptRoot '../../Platypus House.ifc')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$work = New-Item -Force -ItemType Directory (Join-Path $root '.work')
$snapshot = Join-Path $work.FullName 'Source.ifc'
Copy-Item -LiteralPath $Source -Destination $snapshot -Force
$header = Get-Content -LiteralPath $snapshot -TotalCount 8
$export = [regex]::Match(($header -join "`n"), "FILE_NAME\('[^']*','([^']+)'").Groups[1].Value
if (!$export) { throw 'The IFC snapshot is missing its export timestamp.' }

$version = ((& $IfcConvert --version | Out-String) -replace '\x00', '').Trim()
if ($LASTEXITCODE) { throw 'Could not read IfcConvert version.' }
$target = Join-Path $work.FullName 'House.glb'
& $IfcConvert -y -j 4 --use-element-guids --building-local-placement $snapshot $target
if ($LASTEXITCODE -or !(Test-Path -LiteralPath $target)) { throw 'IFC conversion failed.' }

Copy-Item -LiteralPath $target -Destination (Join-Path $root 'Assets/Models/House.glb') -Force
[ordered]@{
    exportTimestamp = $export
    sha256 = (Get-FileHash -LiteralPath $snapshot).Hash
    converter = $version
    options = '-y -j 4 --use-element-guids --building-local-placement'
} | ConvertTo-Json | Set-Content (Join-Path $root 'Tools/HouseSource.json')


# Keep Revit product identity alongside the GUID-named glTF nodes.
$products = foreach ($line in [IO.File]::ReadLines($snapshot)) {
    if ($line -match "^#\d+=(IFC[A-Z0-9]+)\('([^']{22})',#\d+,'((?:''|[^'])*)'") {
        "{0}`t{1}`t{2}" -f $Matches[2], $Matches[1], $Matches[3].Replace("''", "'")
    }
}
$products | Set-Content -Encoding utf8 (Join-Path $root 'Tools/HouseElements.tsv')
