Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSCommandPath
$src = Join-Path $root 'src'

function Remove-ItemSafe {
    param(
        [Parameter(Mandatory=$true)] [string] $Path
    )
    if (Test-Path -LiteralPath $Path) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            Write-Host "Deleted: $Path"
        }
        catch {
            Write-Host "Failed to delete: $Path - $($_.Exception.Message)"
        }
    }
}

# 1) Remove all bin/obj directories under src/
if (Test-Path -LiteralPath $src) {
    Get-ChildItem -Path $src -Directory -Recurse -Force |
        Where-Object { $_.Name -in @('bin','obj') } |
        ForEach-Object { Remove-ItemSafe -Path $_.FullName }
}

# 2) Remove LogSanitizer_TestData folder
Remove-ItemSafe -Path (Join-Path $root 'LogSanitizer_TestData')

# 3) Remove TestLogs folder
Remove-ItemSafe -Path (Join-Path $root 'TestLogs')

# 4) Remove *.log files in root
Get-ChildItem -Path $root -Filter '*.log' -File -Force -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-ItemSafe -Path $_.FullName }

# 5) Remove previous build artifacts: LogSanitizer_Build, Publish, Release
Remove-ItemSafe -Path (Join-Path $root 'LogSanitizer_Build')
Remove-ItemSafe -Path (Join-Path $root 'Publish')
Remove-ItemSafe -Path (Join-Path $root 'Release')

# 6) Remove generated zip files (LogSanitizer_*.zip) recursively
Get-ChildItem -Path $root -Filter 'LogSanitizer_*.zip' -File -Recurse -Force -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-ItemSafe -Path $_.FullName }

Write-Host "Project cleanup completed."
