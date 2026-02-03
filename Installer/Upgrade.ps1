# Cash Drawer System - Automated Upgrade Script
# Run as Administrator

param(
    [string]$InstallerPath = ".\CashDrawerSetup.msi",
    [switch]$BackupData = $true,
    [switch]$CleanInstall = $false
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Cash Drawer System - Upgrade Script" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

# Check if installer exists
if (-not (Test-Path $InstallerPath)) {
    Write-Host "ERROR: Installer not found at: $InstallerPath" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "[1/8] Checking current installation..." -ForegroundColor Yellow
$serverPath = "C:\Program Files\Daniel Szajkowski\Cash Drawer System\Server"
$isInstalled = Test-Path $serverPath

if ($isInstalled) {
    Write-Host "✓ Existing installation found" -ForegroundColor Green
} else {
    Write-Host "ℹ No previous installation detected - this will be a fresh install" -ForegroundColor Cyan
}

# Backup if requested
if ($BackupData -and $isInstalled) {
    Write-Host ""
    Write-Host "[2/8] Backing up configuration..." -ForegroundColor Yellow
    
    $backupPath = "$env:USERPROFILE\Desktop\CashDrawer_Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    
    try {
        Copy-Item "$serverPath\appsettings.json" "$backupPath\" -ErrorAction Stop
        Copy-Item "$serverPath\users.json" "$backupPath\" -ErrorAction SilentlyContinue
        Copy-Item "$serverPath\logs" "$backupPath\logs" -Recurse -ErrorAction SilentlyContinue
        Write-Host "✓ Backup created at: $backupPath" -ForegroundColor Green
    } catch {
        Write-Host "⚠ Backup failed (non-critical): $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "[2/8] Skipping backup (not requested or no existing install)" -ForegroundColor Gray
}

# Stop services
Write-Host ""
Write-Host "[3/8] Stopping services..." -ForegroundColor Yellow

$services = @("CashDrawerServer", "CashDrawerServerControl")
foreach ($serviceName in $services) {
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq "Running") {
            try {
                Stop-Service -Name $serviceName -Force
                Write-Host "✓ Stopped $serviceName" -ForegroundColor Green
            } catch {
                Write-Host "⚠ Could not stop $serviceName : $($_.Exception.Message)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "ℹ $serviceName already stopped" -ForegroundColor Gray
        }
    } else {
        Write-Host "ℹ $serviceName not found (may not be installed yet)" -ForegroundColor Gray
    }
}

# Delete old users.json to force recreation with new token
Write-Host ""
Write-Host "[4/8] Cleaning old authentication data..." -ForegroundColor Yellow

$usersJsonPath = "$serverPath\users.json"
if (Test-Path $usersJsonPath) {
    try {
        Remove-Item $usersJsonPath -Force
        Write-Host "✓ Deleted old users.json (will be recreated with new token)" -ForegroundColor Green
    } catch {
        Write-Host "⚠ Could not delete users.json: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "ℹ No users.json to clean" -ForegroundColor Gray
}

# Clean client settings if requested
if ($CleanInstall) {
    Write-Host ""
    Write-Host "[5/8] Clean install - removing client settings..." -ForegroundColor Yellow
    
    $clientSettingsPath = "$env:LOCALAPPDATA\CashDrawer"
    if (Test-Path $clientSettingsPath) {
        try {
            Remove-Item $clientSettingsPath -Recurse -Force
            Write-Host "✓ Deleted client settings" -ForegroundColor Green
        } catch {
            Write-Host "⚠ Could not delete client settings: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host ""
    Write-Host "[5/8] Keeping client settings (use -CleanInstall to remove)" -ForegroundColor Gray
}

# Run installer
Write-Host ""
Write-Host "[6/8] Running installer..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Cyan

try {
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList "/i `"$InstallerPath`" /qb /norestart" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "✓ Installation completed successfully" -ForegroundColor Green
    } elseif ($process.ExitCode -eq 3010) {
        Write-Host "✓ Installation completed (reboot required)" -ForegroundColor Yellow
    } else {
        Write-Host "✗ Installation failed with exit code: $($process.ExitCode)" -ForegroundColor Red
        pause
        exit 1
    }
} catch {
    Write-Host "✗ Installation error: $($_.Exception.Message)" -ForegroundColor Red
    pause
    exit 1
}

# Start services
Write-Host ""
Write-Host "[7/8] Starting services..." -ForegroundColor Yellow

Start-Sleep -Seconds 3 # Give services time to register

foreach ($serviceName in $services) {
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($service) {
        try {
            Start-Service -Name $serviceName
            Write-Host "✓ Started $serviceName" -ForegroundColor Green
        } catch {
            Write-Host "⚠ Could not start $serviceName : $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# Verify installation
Write-Host ""
Write-Host "[8/8] Verifying installation..." -ForegroundColor Yellow

$serverRunning = (Get-Service -Name "CashDrawerServer" -ErrorAction SilentlyContinue).Status -eq "Running"
$controlRunning = (Get-Service -Name "CashDrawerServerControl" -ErrorAction SilentlyContinue).Status -eq "Running"
$appsettingsExists = Test-Path "$serverPath\appsettings.json"
$usersJsonCreated = Test-Path "$serverPath\users.json"

Write-Host ""
Write-Host "Verification Results:" -ForegroundColor Cyan
Write-Host "  Server Service:       $(if($serverRunning){'✓ Running'}else{'✗ Not Running'})" -ForegroundColor $(if($serverRunning){'Green'}else{'Red'})
Write-Host "  Control Service:      $(if($controlRunning){'✓ Running'}else{'✗ Not Running'})" -ForegroundColor $(if($controlRunning){'Green'}else{'Red'})
Write-Host "  appsettings.json:     $(if($appsettingsExists){'✓ Present'}else{'✗ Missing'})" -ForegroundColor $(if($appsettingsExists){'Green'}else{'Red'})
Write-Host "  users.json:           $(if($usersJsonCreated){'✓ Created'}else{'⚠ Not Yet Created'})" -ForegroundColor $(if($usersJsonCreated){'Green'}else{'Yellow'})

# Show default credentials if users.json was created
if ($usersJsonCreated) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "           DEFAULT CREDENTIALS" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  Username:   admin" -ForegroundColor White
    Write-Host "  Password:   admin" -ForegroundColor White
    Write-Host "  Auth Token: default-token-change-me" -ForegroundColor White
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠ CHANGE THESE IMMEDIATELY via NetworkAdmin!" -ForegroundColor Yellow
}

# Next steps
Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                NEXT STEPS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "1. Launch Client from Start Menu" -ForegroundColor White
Write-Host "2. Go to Settings (⚙️)" -ForegroundColor White
Write-Host "3. Enter connection details:" -ForegroundColor White
Write-Host "     Server: localhost" -ForegroundColor Gray
Write-Host "     Port: 5000" -ForegroundColor Gray
Write-Host "     Token: default-token-change-me" -ForegroundColor Gray
Write-Host "4. Launch NetworkAdmin" -ForegroundColor White
Write-Host "5. Change admin password" -ForegroundColor White
Write-Host "6. Generate strong auth token" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

if ($CleanInstall) {
    Write-Host ""
    Write-Host "⚠ Clean install completed - all clients must be reconfigured!" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✓ Upgrade completed successfully!" -ForegroundColor Green
Write-Host ""

pause
