@echo off
:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo  GM Parts Receiving - Install Windows Service
echo  =============================================
echo.
cd /d "%~dp0"
node service-install.js
echo.
pause
