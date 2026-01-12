Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSCommandPath
$proj = Join-Path $root 'src\LogSanitizer.GUI\LogSanitizer.GUI.csproj'
$temp = Join-Path $root 'TempBuild'
$zipName = 'LogSanitizer_v2.1_Portable.zip'
$zipPath = Join-Path $root $zipName

if (Test-Path $temp) { Remove-Item -Path $temp -Recurse -Force }
New-Item -ItemType Directory -Path $temp | Out-Null

dotnet publish $proj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o $temp

if (-not (Test-Path $temp)) { throw "Publish failed: $temp not found" }

$exe = Get-ChildItem -Path $temp -File -Filter 'LogSanitizer.GUI.exe' | Select-Object -First 1
if (-not $exe) { throw "Executable not found in $temp" }

Get-ChildItem -Path $temp -File | Where-Object { $_.Name -ne 'LogSanitizer.GUI.exe' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
Compress-Archive -Path $exe.FullName -DestinationPath $zipPath -Force

Remove-Item -Path $temp -Recurse -Force
Write-Host ("Success: {0}" -f $zipPath)
