# ST Stopwatch PowerShell Build Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       ST Stopwatch - StartToday Desktop Application   " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found at: $msbuild"
    exit 1
}

Write-Host "`n[1/3] Building STStopwatch ($Configuration)..." -ForegroundColor Yellow
& $msbuild "STStopwatch\STStopwatch.csproj" /nologo /v:m /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "STStopwatch build failed."
    exit $LASTEXITCODE
}
Write-Host "Build succeeded: STStopwatch\bin\$Configuration\STStopwatch.exe" -ForegroundColor Green

Write-Host "`n[2/3] Building Test Suite ($Configuration)..." -ForegroundColor Yellow
& $msbuild "STStopwatch.Tests\STStopwatch.Tests.csproj" /nologo /v:m /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Test suite build failed."
    exit $LASTEXITCODE
}
Write-Host "Test build succeeded." -ForegroundColor Green

Write-Host "`n[3/3] Running Automated Tests..." -ForegroundColor Yellow
& "STStopwatch.Tests\bin\$Configuration\STStopwatch.Tests.exe"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed."
    exit $LASTEXITCODE
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " [SUCCESS] Application Ready: STStopwatch\bin\$Configuration\STStopwatch.exe" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan
