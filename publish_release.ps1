Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSCommandPath
$guiProj = Join-Path $root 'src\LogSanitizer.GUI\LogSanitizer.GUI.csproj'
$publishDir = Join-Path $root 'Publish'
$buildOutputDir = Join-Path $root 'BuildOutput'
$zipName = 'LogSanitizer_v2.1_x64.zip'
$zipPath = Join-Path $buildOutputDir $zipName

if (Test-Path $publishDir) { Remove-Item -Path $publishDir -Recurse -Force }
if (Test-Path $buildOutputDir) { Remove-Item -Path $buildOutputDir -Recurse -Force }

New-Item -ItemType Directory -Path $publishDir | Out-Null
New-Item -ItemType Directory -Path $buildOutputDir | Out-Null

dotnet publish $guiProj -c Release -r win-x64 --self-contained true -o $publishDir /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true

if (-not (Test-Path $publishDir)) { throw "Publish directory not found: $publishDir" }

$exeCandidates = @(Get-ChildItem -Path $publishDir -File -Filter '*.exe')
if ($exeCandidates.Count -lt 1) { throw "No .exe found in publish output: $publishDir" }

$exe = $exeCandidates | Where-Object { $_.Name -like 'LogSanitizer*.exe' } | Select-Object -First 1
if (-not $exe) { $exe = $exeCandidates | Select-Object -First 1 }

if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
Compress-Archive -Path $exe.FullName -DestinationPath $zipPath -Force

Write-Host ("Success: {0}" -f $zipPath)
