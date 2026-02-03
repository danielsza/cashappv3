@echo off
echo ========================================
echo Install Cash Drawer Server as Windows Service
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

:: Get current directory
set "INSTALL_DIR=%~dp0"
set "EXE_PATH=%INSTALL_DIR%CashDrawer.Server.exe"

echo Install Directory: %INSTALL_DIR%
echo.

:: Check if EXE exists
if not exist "%EXE_PATH%" (
    echo ERROR: CashDrawer.Server.exe not found!
    echo Please make sure the EXE is in the same folder as this script.
    echo.
    pause
    exit /b 1
)

:: Check if service already exists
sc query CashDrawerServer >nul 2>&1
if %errorLevel% equ 0 (
    echo Service already exists. Removing old service...
    sc stop CashDrawerServer >nul 2>&1
    timeout /t 2 /nobreak >nul
    sc delete CashDrawerServer
    timeout /t 2 /nobreak >nul
    echo.
)

echo Creating Windows Service...
sc create CashDrawerServer binPath= "\"%EXE_PATH%\"" start= auto DisplayName= "Cash Drawer Server"

if %errorLevel% neq 0 (
    echo.
    echo ERROR: Failed to create service!
    pause
    exit /b 1
)

echo Setting service description...
sc description CashDrawerServer "Cash Drawer Server - Manages COM port communication for cash drawer control"

echo Configuring service to run as Local System...
sc config CashDrawerServer obj= LocalSystem

echo.
echo ========================================
echo SUCCESS! Service installed
echo ========================================
echo.
echo Service Name: CashDrawerServer
echo Display Name: Cash Drawer Server
echo Auto-Start: Yes (starts at boot)
echo.
echo To start now: sc start CashDrawerServer
echo To stop:      sc stop CashDrawerServer
echo To remove:    sc delete CashDrawerServer
echo.
echo Would you like to start the service now?
choice /C YN /M "Start service"

if %errorLevel% equ 1 (
    echo.
    echo Starting service...
    sc start CashDrawerServer
    
    timeout /t 3 /nobreak >nul
    
    sc query CashDrawerServer | findstr "RUNNING" >nul
    if %errorLevel% equ 0 (
        echo.
        echo [SUCCESS] Service is running!
    ) else (
        echo.
        echo [WARNING] Service may not have started correctly.
        echo Check Event Viewer for errors.
    )
)

echo.
echo Installation complete!
echo The server will now start automatically at boot.
echo.
pause
