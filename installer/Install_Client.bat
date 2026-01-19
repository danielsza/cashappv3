@echo off
echo ========================================
echo Cash Drawer Client - Quick Installer
echo Version 3.0
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

:: Set installation directory
set "INSTALL_DIR=C:\CashApp\Client"

echo Installing to: %INSTALL_DIR%
echo.

:: Create directories
echo [1/5] Creating directories...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Docs" mkdir "%INSTALL_DIR%\Docs"

:: Copy files
echo [2/5] Copying application files...
copy /Y "..\Client\CashClient.py" "%INSTALL_DIR%\" >nul
copy /Y "..\Client\StartClient.bat" "%INSTALL_DIR%\" >nul
copy /Y "..\requirements.txt" "%INSTALL_DIR%\" >nul

:: Copy documentation
echo [3/5] Copying documentation...
copy /Y "..\README.md" "%INSTALL_DIR%\Docs\" >nul
copy /Y "..\QUICK_START.md" "%INSTALL_DIR%\Docs\" >nul
copy /Y "..\FAQ.md" "%INSTALL_DIR%\Docs\" >nul
copy /Y "..\PENNY_ROUNDING.md" "%INSTALL_DIR%\Docs\" >nul
copy /Y "..\START_HERE.md" "%INSTALL_DIR%\Docs\" >nul

:: Check Python
echo [4/5] Checking Python installation...
python --version >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo WARNING: Python not found in PATH!
    echo.
    echo Please install Python 3.8 or higher from:
    echo https://www.python.org/downloads/
    echo.
    echo Make sure to check "Add Python to PATH" during installation!
    echo.
    set PYTHON_INSTALLED=0
) else (
    python --version
    set PYTHON_INSTALLED=1
    
    :: Note: Client doesn't need pyserial, but we'll upgrade pip
    echo Upgrading pip...
    python -m pip install --upgrade pip --quiet
)

:: Create shortcuts
echo [5/5] Creating shortcuts...
set "SHORTCUT_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Cash Drawer"
if not exist "%SHORTCUT_DIR%" mkdir "%SHORTCUT_DIR%"

:: Create VBS script for shortcut (no console window)
echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%SHORTCUT_DIR%\Cash Drawer.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "pythonw.exe" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Arguments = """%INSTALL_DIR%\CashClient.py""" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Save >> "%TEMP%\CreateShortcut.vbs"
cscript //nologo "%TEMP%\CreateShortcut.vbs"

:: Desktop shortcut
set "DESKTOP=%USERPROFILE%\Desktop"
echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%DESKTOP%\Cash Drawer.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "pythonw.exe" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Arguments = """%INSTALL_DIR%\CashClient.py""" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Save >> "%TEMP%\CreateShortcut.vbs"
cscript //nologo "%TEMP%\CreateShortcut.vbs"

del "%TEMP%\CreateShortcut.vbs" >nul 2>&1

echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo Installation directory: %INSTALL_DIR%
echo.
echo NEXT STEPS:
echo.
echo 1. Launch Cash Drawer Client
echo    - Desktop: Double-click "Cash Drawer" icon
echo    - Start Menu: Cash Drawer ^> Cash Drawer
echo    - Or run: %INSTALL_DIR%\StartClient.bat
echo.
echo 2. Configure Servers (if needed)
echo    - Client will auto-discover servers on first run
echo    - Or manually enter server IPs in Settings tab
echo.
echo 3. Login
echo    - Enter your username and password
echo    - Default admin: admin / admin123
echo    - (Admins: Change this password!)
echo.
echo 4. Start Using
echo    - Enter transactions
echo    - Open cash drawer
echo    - Enjoy automatic penny rounding!
echo.
echo Documentation: %INSTALL_DIR%\Docs\START_HERE.md
echo.

if %PYTHON_INSTALLED%==0 (
    echo *** IMPORTANT: Install Python first! ***
    echo https://www.python.org/downloads/
    echo.
)

echo Would you like to launch the Cash Drawer Client now?
choice /C YN /M "Launch Cash Drawer"
if %ERRORLEVEL%==1 (
    start pythonw.exe "%INSTALL_DIR%\CashClient.py"
)

echo.
pause
