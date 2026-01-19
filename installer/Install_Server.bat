@echo off
echo ========================================
echo Cash Drawer Server - Quick Installer
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
set "INSTALL_DIR=C:\CashApp\Server"

echo Installing to: %INSTALL_DIR%
echo.

:: Create directories
echo [1/7] Creating directories...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Logs" mkdir "%INSTALL_DIR%\Logs"
if not exist "%INSTALL_DIR%\Docs" mkdir "%INSTALL_DIR%\Docs"

:: Copy files
echo [2/7] Copying application files...
copy /Y "..\Server\CashServer.py" "%INSTALL_DIR%\" >nul
copy /Y "..\Server\ServerConfig.py" "%INSTALL_DIR%\" >nul
copy /Y "..\Server\UserManager.py" "%INSTALL_DIR%\" >nul
copy /Y "..\Server\StartServer.bat" "%INSTALL_DIR%\" >nul
copy /Y "..\Server\ConfigureServer.bat" "%INSTALL_DIR%\" >nul
copy /Y "..\requirements.txt" "%INSTALL_DIR%\" >nul

:: Copy documentation
echo [3/7] Copying documentation...
copy /Y "..\*.md" "%INSTALL_DIR%\Docs\" >nul

:: Check Python
echo [4/7] Checking Python installation...
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
)

:: Install Python packages
if %PYTHON_INSTALLED%==1 (
    echo [5/7] Installing Python packages...
    python -m pip install --upgrade pip --quiet
    python -m pip install -r "%INSTALL_DIR%\requirements.txt" --quiet
    if %errorLevel% neq 0 (
        echo WARNING: Failed to install some packages
        echo Please run: pip install pyserial
    ) else (
        echo Python packages installed successfully
    )
) else (
    echo [5/7] Skipping Python packages (Python not found)
)

:: Configure Windows Firewall
echo [6/7] Configuring Windows Firewall...
netsh advfirewall firewall add rule name="Cash Server TCP" dir=in action=allow protocol=TCP localport=5000 >nul 2>&1
netsh advfirewall firewall add rule name="Cash Server Discovery" dir=in action=allow protocol=UDP localport=5001 >nul 2>&1
echo Firewall rules added (TCP 5000, UDP 5001)

:: Create Start Menu shortcuts
echo [7/7] Creating shortcuts...
set "SHORTCUT_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Cash Drawer Server"
if not exist "%SHORTCUT_DIR%" mkdir "%SHORTCUT_DIR%"

:: Create VBS scripts for shortcuts (no console window)
echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%SHORTCUT_DIR%\Cash Server.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "pythonw.exe" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Arguments = """%INSTALL_DIR%\CashServer.py""" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Save >> "%TEMP%\CreateShortcut.vbs"
cscript //nologo "%TEMP%\CreateShortcut.vbs"

echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%SHORTCUT_DIR%\Configure Server.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "pythonw.exe" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Arguments = """%INSTALL_DIR%\ServerConfig.py""" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Save >> "%TEMP%\CreateShortcut.vbs"
cscript //nologo "%TEMP%\CreateShortcut.vbs"

:: Desktop shortcut
set "DESKTOP=%USERPROFILE%\Desktop"
echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%DESKTOP%\Cash Server.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "pythonw.exe" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Arguments = """%INSTALL_DIR%\CashServer.py""" >> "%TEMP%\CreateShortcut.vbs"
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
echo 1. Configure COM Port in Device Manager
echo    - Set USB-Serial adapter to COM10
echo    - Reboot after changing COM port
echo.
echo 2. Run Server Configuration
echo    - Start Menu: Cash Drawer Server ^> Configure Server
echo    - Or run: %INSTALL_DIR%\ConfigureServer.bat
echo.
echo 3. Create User Accounts
echo    - Start Menu: Cash Drawer Server ^> User Manager
echo    - Or run: %INSTALL_DIR%\UserManager.py
echo    - CHANGE default admin password!
echo.
echo 4. Start the Server
echo    - Start Menu: Cash Drawer Server ^> Cash Server
echo    - Or run: %INSTALL_DIR%\StartServer.bat
echo.
echo Documentation: %INSTALL_DIR%\Docs\START_HERE.md
echo.

if %PYTHON_INSTALLED%==0 (
    echo *** IMPORTANT: Install Python first! ***
    echo https://www.python.org/downloads/
    echo.
)

echo Would you like to open the Configuration tool now?
choice /C YN /M "Open Server Configuration"
if %ERRORLEVEL%==1 (
    start pythonw.exe "%INSTALL_DIR%\ServerConfig.py"
)

echo.
pause
