@echo off
echo.
echo  GM Parts Receiving - Setup
echo  ==========================================
echo.

:: Check if Node.js is installed
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo  ERROR: Node.js is not installed.
    echo.
    echo  Download it from: https://nodejs.org
    echo  Install the LTS version, then run this script again.
    echo.
    pause
    exit /b 1
)

echo  Node.js found: 
node --version
echo.

:: Install dependencies
echo  Installing dependencies...
call npm install
if %errorlevel% neq 0 (
    echo  ERROR: npm install failed.
    pause
    exit /b 1
)
echo.

:: Build for production
echo  Building app...
call npm run build
if %errorlevel% neq 0 (
    echo  ERROR: Build failed.
    pause
    exit /b 1
)
echo.

echo  ==========================================
echo  Setup complete!
echo.
echo  To test now:     Double-click START.bat
echo  To auto-start:   Right-click INSTALL-SERVICE.bat > Run as Administrator
echo  ==========================================
echo.
pause
