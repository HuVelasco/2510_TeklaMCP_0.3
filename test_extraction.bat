@echo off
echo ===================================================
echo TeklaDrawingExtractor Automated Test
echo ===================================================
echo.
echo Starting extraction from selected drawings...
echo Output: C:\Tekla\Extractions
echo.

cd TeklaDrawingExtractor\bin\Release\net48

REM Run extraction with automation flag
TeklaDrawingExtractor.exe --auto-extract

echo.
echo ===================================================
echo Test complete!
echo ===================================================
pause
