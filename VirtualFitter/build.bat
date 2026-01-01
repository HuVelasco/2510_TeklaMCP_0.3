@echo off
echo ============================================================
echo Building Virtual Fitter - Automatic Dimension Creator
echo ============================================================
echo.

cd /d "%~dp0"

echo Restoring packages...
dotnet restore VirtualFitter.csproj

echo.
echo Building Release version...
dotnet build VirtualFitter.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================================
    echo BUILD SUCCESSFUL!
    echo ============================================================
    echo.
    echo Executable: bin\Release\net48\VirtualFitter.exe
    echo.
    echo To run:
    echo   1. Open Tekla Structures with a drawing
    echo   2. Double-click bin\Release\net48\VirtualFitter.exe
    echo.
) else (
    echo.
    echo ============================================================
    echo BUILD FAILED!
    echo ============================================================
    echo.
    echo Check errors above and fix before running.
    echo.
)

pause
