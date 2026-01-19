@echo off
cd /d "%~dp0"

python CashClient.py

if errorlevel 1 (
    echo.
    echo ERROR: Failed to start client!
    echo.
    echo Please check:
    echo 1. Python is installed and in PATH
    echo 2. Configuration file is correct
    echo 3. Server is running and accessible
    echo.
    pause
)
