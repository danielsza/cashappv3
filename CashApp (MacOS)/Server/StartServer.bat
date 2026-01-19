@echo off
echo Starting Cash Drawer Server...
echo.

cd /d "%~dp0"

python CashServer.py

if errorlevel 1 (
    echo.
    echo ERROR: Failed to start server!
    echo.
    echo Please check:
    echo 1. Python is installed and in PATH
    echo 2. pyserial is installed: pip install pyserial
    echo 3. COM port is properly configured
    echo 4. Configuration file is correct
    echo.
    pause
)
