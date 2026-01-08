Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSCommandPath
$guiProj = Join-Path $root 'src\LogSanitizer.GUI\LogSanitizer.GUI.csproj'
$cliProj = Join-Path $root 'src\LogSanitizer.CLI\LogSanitizer.CLI.csproj'
Write-Host 'Cleaning previous builds...'
dotnet clean $guiProj -c Release
dotnet clean $cliProj -c Release
Write-Host 'Publishing GUI...'
dotnet publish $guiProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
Write-Host 'Publishing CLI...'
dotnet publish $cliProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
$guiOut = Join-Path $root 'src\LogSanitizer.GUI\bin\Release\net8.0-windows\win-x64\publish\LogSanitizer.GUI.exe'
$cliOut = Join-Path $root 'src\LogSanitizer.CLI\bin\Release\net8.0\win-x64\publish\LogSanitizer.CLI.exe'
Write-Host 'Publish outputs:'
Write-Host $guiOut
Write-Host $cliOut
if (Test-Path $guiOut) { $g=(Get-Item $guiOut).Length; Write-Host ("GUI size: {0:N0} bytes" -f $g) }
if (Test-Path $cliOut) { $c=(Get-Item $cliOut).Length; Write-Host ("CLI size: {0:N0} bytes" -f $c) }
