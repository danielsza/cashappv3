@echo off
echo.
echo  GM Parts Receiving - Publish to GitHub
echo  ==========================================
echo.
echo  Before running this, make sure you have:
echo    1. A GitHub account
echo    2. Git installed (https://git-scm.com/download/win)
echo    3. Created a NEW PRIVATE repo called "gm-parts-receiving" at:
echo       https://github.com/new
echo       (Do NOT initialize with README or .gitignore)
echo    4. A Personal Access Token with "repo" scope from:
echo       https://github.com/settings/tokens
echo.
echo  Press any key when ready, or close this window to cancel.
pause >nul

:: Get GitHub username
echo.
set /p GHUSER="  Enter your GitHub username: "

:: Setup credential helper so token is saved
git config --global credential.helper manager

:: Initialize and push
cd /d "%~dp0"
git init
git add .
git commit -m "Initial commit - GM Parts Receiving v1"
git branch -M main
git remote add origin https://github.com/%GHUSER%/gm-parts-receiving.git

echo.
echo  Pushing to GitHub...
echo  (Git will ask for your username and token - paste the token as password)
echo.
git push -u origin main

if %errorlevel% equ 0 (
    echo.
    echo  ==========================================
    echo  SUCCESS! Code is live at:
    echo  https://github.com/%GHUSER%/gm-parts-receiving
    echo  ==========================================
) else (
    echo.
    echo  Push failed. Check your username and token.
)
echo.
pause
