$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# Clean up previous release
if (Test-Path "Release") { Remove-Item "Release" -Recurse -Force }
New-Item -ItemType Directory -Force -Path "Release"

# Publish CLI
Write-Host "Publishing CLI..."
dotnet publish "src\LogSanitizer.CLI\LogSanitizer.CLI.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "bin\Publish\CLI"
Move-Item "bin\Publish\CLI\LogSanitizer.CLI.exe" "Release\LogSanitizer.CLI.exe"

# Publish GUI
Write-Host "Publishing GUI..."
dotnet publish "src\LogSanitizer.GUI\LogSanitizer.GUI.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "bin\Publish\GUI"
Move-Item "bin\Publish\GUI\LogSanitizer.GUI.exe" "Release\LogSanitizer.GUI.exe"

Write-Host "Done! Check the 'Release' folder."
