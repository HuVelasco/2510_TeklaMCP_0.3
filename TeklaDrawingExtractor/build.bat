@echo off
echo ========================================
echo Building Tekla Drawing Extractor
echo ========================================
echo.

REM Clean previous build
echo Cleaning previous build...
dotnet clean --configuration Release

REM Build the project
echo.
echo Building project...
dotnet build --configuration Release

echo.
if %ERRORLEVEL% EQU 0 (
    echo ========================================
    echo Build completed successfully!
    echo ========================================
    echo.
    echo Executable location:
    echo bin\Release\net472\TeklaDrawingExtractor.exe
    echo.
) else (
    echo ========================================
    echo Build FAILED!
    echo ========================================
    echo.
)

pause
