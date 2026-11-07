@echo off
setlocal enabledelayedexpansion
title ST TimerQt - Build Script

echo ========================================================
echo       ST TimerQt - Build and Package Pipeline
echo          StartToday Technologies Desktop App
echo ========================================================
echo.

set MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if not exist %MSBUILD% (
    echo [ERROR] MSBuild not found at: %MSBUILD%
    pause
    exit /b 1
)

echo [1/3] Building Release configuration...
%MSBUILD% STStopwatch\STStopwatch.csproj /nologo /v:m /p:Configuration=Release
if errorlevel 1 (
    echo [FAIL] Release build failed.
    pause
    exit /b 1
)
echo [PASS] Release build succeeded: STStopwatch\bin\Release\STTimerQt.exe
echo.

echo [2/3] Building Test suite...
%MSBUILD% STStopwatch.Tests\STStopwatch.Tests.csproj /nologo /v:m /p:Configuration=Release
if errorlevel 1 (
    echo [FAIL] Test build failed.
    pause
    exit /b 1
)
echo [PASS] Test build succeeded.
echo.

echo [3/3] Executing Automated Verification Suite...
STStopwatch.Tests\bin\Release\STStopwatch.Tests.exe
if errorlevel 1 (
    echo [FAIL] Unit tests failed.
    pause
    exit /b 1
)

echo.
echo ========================================================
echo [SUCCESS] Build & Verification Complete!
echo Run the application at: STStopwatch\bin\Release\STTimerQt.exe
echo ========================================================
echo.

pause
