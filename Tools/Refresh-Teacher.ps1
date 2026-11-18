param(
    [Parameter(Mandatory)][string]$Source,
    [string]$Revit = 'C:\Program Files\Autodesk\Revit 2027\Revit.exe'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$work = New-Item -Type Directory -Force (Join-Path $root 'VR/.work')
$run = New-Item -Type Directory (Join-Path ([IO.Path]::GetTempPath()) ("TeacherTour-" + [Guid]::NewGuid().ToString('N')))
$inputCopy = Join-Path $run.FullName 'Teacher.rvt'
Copy-Item -LiteralPath (Resolve-Path $Source) -Destination $inputCopy
$output = New-Item -Type Directory (Join-Path $run.FullName 'geometry')
$assembly = Join-Path $run.FullName 'assemblies'
dotnet build (Join-Path $PSScriptRoot 'RevitExport/TeacherExport.csproj') -o $assembly
if ($LASTEXITCODE) { throw 'Revit export command compilation failed.' }
$id = '54f33cb8-368f-49ee-ae7d-45fb17502ecf'
$library = [Security.SecurityElement]::Escape((Join-Path $assembly 'TeacherExport.dll'))
$manifest = "<RevitAddIns><AddIn Type=""Command""><Assembly>$library</Assembly><AddInId>$id</AddInId><FullClassName>TeacherExport</FullClassName><Text>Export teacher model for VR</Text><VendorId>COVE</VendorId><VendorDescription>Digital Media Design Team</VendorDescription></AddIn></RevitAddIns>"
[IO.File]::WriteAllText((Join-Path $run.FullName 'TeacherExport.addin'),$manifest)
$journal=Join-Path $run.FullName 'journal.0001.txt'
$lines=@(
    "' 0:< Initial VM: Revit journal",
    "' 0:< GUI Resource Usage",
    "'C 18-Nov-2026 00:00:00.000;  started recording journal file",
    "' Build: 20260716_1515(x64)",
    "' Branch: RELEASE_2027.2",
    "' Release: 2027.2",
    'Dim Jrn','Set Jrn = CrsJournalScript',
    'Jrn.Command "Ribbon", "Open an existing project , ID_REVIT_FILE_OPEN"',
    "Jrn.Data ""File Name"", ""IDOK"", ""$inputCopy""",
    "Jrn.RibbonEvent ""Execute external command:${id}:TeacherExport""",
    'Jrn.Command "SystemMenu", "Quit the application; prompts to save projects , ID_APP_EXIT"'
)
[IO.File]::WriteAllLines($journal,$lines,[Text.Encoding]::ASCII)
$previous=[Environment]::GetEnvironmentVariable('TEACHER_EXPORT_DIR')
try {
    $env:TEACHER_EXPORT_DIR=$output.FullName
    $process=Start-Process -FilePath $Revit -ArgumentList ('"'+$journal+'"') -WorkingDirectory $run.FullName -WindowStyle Hidden -PassThru
    $result=Join-Path $output.FullName 'result.txt'
    while (!(Test-Path $result) -and !$process.HasExited) { Start-Sleep 2 }
    if (!(Test-Path $result) -or (Get-Content $result -Raw) -ne 'OK') { throw "Revit export failed. Inspect $run" }
    uv run --script (Join-Path $PSScriptRoot 'PrepareModel.py') $output.FullName
    if ($LASTEXITCODE) { throw 'Unity model conversion failed.' }
} finally { [Environment]::SetEnvironmentVariable('TEACHER_EXPORT_DIR',$previous) }
