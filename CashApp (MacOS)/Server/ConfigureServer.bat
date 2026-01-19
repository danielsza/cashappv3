@echo off
echo Starting Cash Server Configuration...
echo.

cd /d "%~dp0"

python ServerConfig.py

if errorlevel 1 (
    echo.
    echo ERROR: Failed to start configuration tool!
    echo.
    echo Please check:
    echo 1. Python is installed and in PATH
    echo 2. pyserial is installed: pip install pyserial
    echo.
    pause
)
