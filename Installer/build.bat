@echo off
REM Cash Drawer System - Installer Build Script
REM Copyright (c) 2026 Daniel Szajkowski. All rights reserved.

echo ===============================================
echo Cash Drawer Management System - Build Installer
echo Copyright (c) 2026 Daniel Szajkowski
echo ===============================================
echo.

REM Check for WiX Toolset
where candle.exe >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: WiX Toolset not found in PATH
    echo Please install WiX Toolset v3.11 or later from:
    echo https://wixtoolset.org/releases/
    echo.
    pause
    exit /b 1
)

echo [1/7] Cleaning previous builds...
if exist Output rmdir /s /q Output
mkdir Output

echo [2/7] Building Release binaries...
cd ..
dotnet clean -c Release
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

cd Installer

echo [3/7] Harvesting Server components...
heat dir ..\CashDrawer.Server\bin\Release\net8.0-windows -cg ServerComponents -dr ServerFolder -gg -sfrag -srd -var var.ServerSource -out ServerComponents.wxs

echo [4/7] Harvesting ServerControl components...
heat dir ..\CashDrawer.ServerControl\bin\Release\net8.0-windows -cg ServerControlComponents -dr ControlFolder -gg -sfrag -srd -var var.ControlSource -out ServerControlComponents.wxs

echo [5/7] Harvesting Client components...
heat dir ..\CashDrawer.Client\bin\Release\net8.0-windows -cg ClientComponents -dr ClientFolder -gg -sfrag -srd -var var.ClientSource -out ClientComponents.wxs

echo [6/7] Harvesting NetworkAdmin components...
heat dir ..\CashDrawer.NetworkAdmin\bin\Release\net8.0-windows -cg NetworkAdminComponents -dr NetworkAdminFolder -gg -sfrag -srd -var var.NetworkAdminSource -out NetworkAdminComponents.wxs

echo [7/7] Compiling and linking installer...
candle Product.wxs ServerComponents.wxs ServerControlComponents.wxs ClientComponents.wxs NetworkAdminComponents.wxs -ext WixUIExtension -ext WixUtilExtension -dServerSource=..\CashDrawer.Server\bin\Release\net8.0-windows -dControlSource=..\CashDrawer.ServerControl\bin\Release\net8.0-windows -dClientSource=..\CashDrawer.Client\bin\Release\net8.0-windows -dNetworkAdminSource=..\CashDrawer.NetworkAdmin\bin\Release\net8.0-windows

if %errorlevel% neq 0 (
    echo ERROR: Compilation failed
    pause
    exit /b 1
)

light -out Output\CashDrawerSetup.msi Product.wixobj ServerComponents.wixobj ServerControlComponents.wixobj ClientComponents.wixobj NetworkAdminComponents.wixobj -ext WixUIExtension -ext WixUtilExtension

if %errorlevel% neq 0 (
    echo ERROR: Linking failed
    pause
    exit /b 1
)

echo.
echo ===============================================
echo SUCCESS! Installer created:
echo Output\CashDrawerSetup.msi
echo ===============================================
echo.
echo To install, run as Administrator:
echo   msiexec /i CashDrawerSetup.msi
echo.
pause
