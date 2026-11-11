# ST TimerQt PowerShell Build Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       ST TimerQt - StartToday Desktop Application     " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found at: $msbuild"
    exit 1
}

# Helper function to sign application binaries
function Sign-Binary {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) { return }
    
    # Grab the first matching certificate
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | 
            Where-Object { $_.Subject -match "LocalDevCert" } | 
            Select-Object -First 1

    if (-not $cert) {
        Write-Warning "Code signing certificate 'LocalDevCert' not found in Cert:\CurrentUser\My."
        return
    }

    $result = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert
    if ($result.Status -ne "Valid") {
        Write-Warning "Signing $FilePath returned status: $($result.Status)"
    } else {
        Write-Host "Successfully signed: $FilePath" -ForegroundColor Gray
    }
}

# [1/3] Build & Sign Application
Write-Host "`n[1/3] Building ST TimerQt ($Configuration)..." -ForegroundColor Yellow
& $msbuild "STStopwatch\STStopwatch.csproj" /nologo /v:m /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "ST TimerQt build failed."
    exit $LASTEXITCODE
}
$appExe = "STStopwatch\bin\$Configuration\STTimerQt.exe"
$appCoreDll = "STStopwatch\bin\$Configuration\STStopwatch.Core.dll"
Sign-Binary -FilePath $appExe
Sign-Binary -FilePath $appCoreDll
Write-Host "Build succeeded: $appExe" -ForegroundColor Green

# [2/3] Build & Sign Test Suite
Write-Host "`n[2/3] Building Test Suite ($Configuration)..." -ForegroundColor Yellow
& $msbuild "STStopwatch.Tests\STStopwatch.Tests.csproj" /nologo /v:m /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Test suite build failed."
    exit $LASTEXITCODE
}
$testExe = "STStopwatch.Tests\bin\$Configuration\STStopwatch.Tests.exe"
$testCoreDll = "STStopwatch.Tests\bin\$Configuration\STStopwatch.Core.dll"
Sign-Binary -FilePath $testExe
Sign-Binary -FilePath $testCoreDll
Write-Host "Test build succeeded." -ForegroundColor Green

# [3/3] Run Automated Tests
Write-Host "`n[3/3] Running Automated Tests..." -ForegroundColor Yellow

# Using cmd.exe to bypass PowerShell's strict local execution blocks
cmd.exe /c "`"$PWD\$testExe`""
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed."
    exit $LASTEXITCODE
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " [SUCCESS] Application Ready: $appExe" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan
