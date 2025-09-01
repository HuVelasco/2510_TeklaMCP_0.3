@echo off
REM TeklaMCP Autostart Batch Script
REM Single-click startup for both HTTP backend and MCP server

echo ========================================
echo    TeklaMCP Phase 1 - Quick Start
echo ========================================

cd /d "%~dp0\Server\TeklaMCP"

echo Starting TeklaMCP servers...
python start_mcp_server.py

echo.
echo Press any key to exit...
pause >nul