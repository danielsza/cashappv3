@echo off
echo ========================================
echo Uninstall Cash Drawer Server Service
echo ========================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Administrator privileges required!
    echo Right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

:: Check if service exists
sc query CashDrawerServer >nul 2>&1
if %errorLevel% neq 0 (
    echo Service 'CashDrawerServer' not found.
    echo Nothing to uninstall.
    echo.
    pause
    exit /b 0
)

echo Found service: CashDrawerServer
echo.

choice /C YN /M "Remove service"
if %errorLevel% neq 1 (
    echo Cancelled.
    pause
    exit /b 0
)

echo.
echo Stopping service...
sc stop CashDrawerServer

:: Wait for service to stop
timeout /t 3 /nobreak >nul

echo Deleting service...
sc delete CashDrawerServer

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo SUCCESS! Service removed
    echo ========================================
    echo.
    echo The Cash Drawer Server service has been uninstalled.
    echo.
) else (
    echo.
    echo ERROR: Failed to delete service.
    echo The service may still be stopping.
    echo Try running this script again in a few seconds.
    echo.
)

pause
