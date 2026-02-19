@echo off
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo  GM Parts Receiving - Uninstall Windows Service
echo  ===============================================
echo.
cd /d "%~dp0"
node service-uninstall.js
echo.
pause
