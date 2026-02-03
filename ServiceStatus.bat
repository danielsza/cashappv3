@echo off
echo ========================================
echo Cash Drawer Server - Service Status
echo ========================================
echo.

sc query CashDrawerServer >nul 2>&1
if %errorLevel% neq 0 (
    echo [NOT INSTALLED] Service not found
    echo.
    echo To install: Run InstallService.bat as Administrator
    echo.
    pause
    exit /b 0
)

echo Service Status:
echo ---------------
sc query CashDrawerServer

echo.
echo Service Configuration:
echo ---------------------
sc qc CashDrawerServer

echo.
echo.
echo Commands:
echo   Start:   sc start CashDrawerServer
echo   Stop:    sc stop CashDrawerServer
echo   Restart: sc stop CashDrawerServer ^& sc start CashDrawerServer
echo.
pause
