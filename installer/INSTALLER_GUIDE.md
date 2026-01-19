# Cash Drawer System - Installer Guide

## Overview

We provide **two types of installers** for easy deployment on Windows:

1. **Batch Installers** (.bat) - Simple, no prerequisites
2. **Inno Setup Installers** (.iss) - Professional Windows installers

Choose the method that works best for your environment.

---

## 🚀 Quick Start (Recommended)

### For Most Users: Batch Installers

**Easiest method - no additional software needed!**

1. Right-click `Install_Server.bat` → **Run as administrator**
2. Follow the prompts
3. Done!

---

## 📦 Option 1: Batch Installers (Simple)

### Features

✅ No prerequisites (except Python)  
✅ Works immediately  
✅ Creates shortcuts automatically  
✅ Configures firewall  
✅ Simple step-by-step process  
✅ ~1 minute installation  

### Server Installation

**File**: `Install_Server.bat`

**What it does:**
1. Creates `C:\CashApp\Server\` directory
2. Copies all server files
3. Checks for Python
4. Installs Python packages (pyserial)
5. Configures Windows Firewall (ports 5000, 5001)
6. Creates Start Menu shortcuts
7. Creates Desktop shortcut
8. Offers to open Configuration tool

**Steps:**
```
1. Right-click Install_Server.bat
2. Select "Run as administrator"
3. Follow prompts
4. When complete, run Server Configuration
```

**Post-Installation:**
- Configure COM port in Device Manager
- Run `ServerConfig.py` to configure settings
- Run `UserManager.py` to create users
- Start the server

### Client Installation

**File**: `Install_Client.bat`

**What it does:**
1. Creates `C:\CashApp\Client\` directory
2. Copies client files
3. Checks for Python
4. Copies documentation
5. Creates Start Menu shortcuts
6. Creates Desktop shortcut
7. Offers to launch client

**Steps:**
```
1. Right-click Install_Client.bat
2. Select "Run as administrator"
3. Follow prompts
4. Launch Cash Drawer Client
```

**Post-Installation:**
- Launch client
- Auto-discover servers (or enter manually)
- Login and start using

### Troubleshooting Batch Installers

**"Python not found"**
- Install Python from https://www.python.org/downloads/
- Check "Add Python to PATH" during installation
- Run installer again

**"Access denied"**
- Must run as Administrator
- Right-click → "Run as administrator"

**Shortcuts don't work**
- Check Python is in PATH: `python --version`
- Reinstall Python with "Add to PATH" checked

---

## 🎯 Option 2: Inno Setup Installers (Professional)

### Features

✅ Professional Windows installer  
✅ Graphical interface  
✅ Custom configuration during install  
✅ Add/Remove Programs integration  
✅ Clean uninstall  
✅ Progress bars and status  

### Prerequisites

**To CREATE installers** (one-time, for IT):
1. Install Inno Setup from: https://jrsoftware.org/isdl.php
2. Open .iss file in Inno Setup
3. Click "Compile" to create .exe installer

**To USE installers** (end users):
- No prerequisites - just run the .exe!

### Creating the Installers

**For IT/Admin: Build the installer executables**

1. **Install Inno Setup**
   ```
   Download from: https://jrsoftware.org/isdl.php
   Install (next, next, finish)
   ```

2. **Compile Server Installer**
   ```
   1. Open Inno Setup Compiler
   2. File → Open: Server_Setup.iss
   3. Build → Compile
   4. Output: CashDrawer_Server_v3.0_Setup.exe
   ```

3. **Compile Client Installer**
   ```
   1. Open Inno Setup Compiler
   2. File → Open: Client_Setup.iss
   3. Build → Compile
   4. Output: CashDrawer_Client_v3.0_Setup.exe
   ```

### Using the Inno Setup Installers

**For End Users: Run the .exe installer**

**Server:**
1. Run `CashDrawer_Server_v3.0_Setup.exe`
2. Follow installation wizard
3. Choose options:
   - ✅ Desktop icon
   - ✅ Run at startup (optional)
   - ✅ Configure firewall
   - ✅ Create initial users
4. Configure settings when prompted
5. Done!

**Client:**
1. Run `CashDrawer_Client_v3.0_Setup.exe`
2. Follow installation wizard
3. Choose options:
   - ✅ Desktop icon
   - ✅ Auto-discover servers
   - ✅ Auto-connect on startup
4. Enter server IPs (or leave blank for auto-discovery)
5. Done!

### Inno Setup Features

**Server Installer:**
- Checks for Python (offers to install if missing)
- Configures Windows Firewall automatically
- Creates default configuration
- Offers to run configuration tool
- Offers to create user accounts
- Clean uninstall support

**Client Installer:**
- Checks for Python (offers to install if missing)
- Auto-discovery option
- Server configuration (or skip for auto-discovery)
- Auto-connect option
- Desktop shortcut
- Clean uninstall support

### Customizing Inno Setup Scripts

**Edit Server_Setup.iss or Client_Setup.iss:**

```pascal
; Change company name
#define MyAppPublisher "Your Company Name"

; Change default install location
DefaultDirName={autopf}\YourPath\Server

; Add/remove tasks
[Tasks]
Name: "mytask"; Description: "My custom task"
```

**Then recompile to create new .exe**

---

## 📋 Installation Comparison

| Feature | Batch Installer | Inno Setup Installer |
|---------|----------------|---------------------|
| **Ease of Use** | Very Easy | Very Easy |
| **Prerequisites** | None | None (for users) |
| **Build Time** | Instant | ~1 min compile |
| **File Size** | <10 KB | ~2-3 MB |
| **Uninstall** | Manual | Automatic |
| **GUI** | Command line | Graphical wizard |
| **Customization** | Simple | Advanced |
| **Add/Remove Programs** | No | Yes |
| **Best For** | Quick deployment | Professional deployment |

---

## 🎯 Recommended Installation Method

### For Small Deployments (1-5 computers)
→ **Use Batch Installers**
- Fastest
- No build step
- Works immediately

### For Large Deployments (10+ computers)
→ **Use Inno Setup Installers**
- Professional appearance
- Easier for non-technical users
- Proper uninstall
- Add/Remove Programs integration

### For Mixed/Remote Locations
→ **Provide Both**
- Batch for quick/emergency installs
- Inno Setup for regular deployments

---

## 📂 Installer Files

```
Installers/
├── Install_Server.bat              # Batch installer for server
├── Install_Client.bat              # Batch installer for client
├── Server_Setup.iss                # Inno Setup script for server
├── Client_Setup.iss                # Inno Setup script for client
└── INSTALLER_GUIDE.md              # This file
```

**After compiling Inno Setup scripts:**
```
Installers/
├── CashDrawer_Server_v3.0_Setup.exe    # Server installer executable
└── CashDrawer_Client_v3.0_Setup.exe    # Client installer executable
```

---

## 🔧 What Gets Installed

### Server Installation

**Files:**
- CashServer.py
- ServerConfig.py
- UserManager.py
- StartServer.bat
- ConfigureServer.bat
- requirements.txt
- Documentation (Docs folder)

**Directories:**
- `C:\CashApp\Server\` - Application
- `C:\CashApp\Server\Logs\` - Log files
- `C:\CashApp\Server\Docs\` - Documentation

**Shortcuts:**
- Start Menu: Cash Drawer Server → Cash Server
- Start Menu: Cash Drawer Server → Configure Server
- Start Menu: Cash Drawer Server → User Manager
- Desktop: Cash Server (optional)
- Startup: Cash Server (optional)

**Configuration:**
- Windows Firewall: TCP 5000, UDP 5001
- Default config: server_config.ini

### Client Installation

**Files:**
- CashClient.py
- StartClient.bat
- requirements.txt
- Documentation (Docs folder)

**Directories:**
- `C:\CashApp\Client\` - Application
- `C:\CashApp\Client\Docs\` - Documentation

**Shortcuts:**
- Start Menu: Cash Drawer → Cash Drawer
- Desktop: Cash Drawer (optional)

**Configuration:**
- Default config: client_config.ini (with auto-discovery)

---

## 🚀 Mass Deployment

### Deploy to Multiple Computers

**Option 1: Network Share**
```batch
:: Put installers on network share
\\server\Installers\Install_Client.bat

:: Run from each computer
\\server\Installers\Install_Client.bat
```

**Option 2: Group Policy**
1. Compile Inno Setup installers to .exe
2. Create GPO for software installation
3. Deploy .exe via Group Policy

**Option 3: Remote PowerShell**
```powershell
# Copy installer to remote computer
Copy-Item "Install_Client.bat" "\\Computer1\C$\Temp\"

# Execute remotely
Invoke-Command -ComputerName Computer1 -ScriptBlock {
    Start-Process "C:\Temp\Install_Client.bat" -Verb RunAs
}
```

**Option 4: Manual USB**
1. Copy installer to USB drive
2. Run on each computer
3. Takes ~2 minutes per computer

---

## 🔄 Updating/Reinstalling

### Batch Installers
- Just run installer again
- Overwrites existing files
- Preserves configuration

### Inno Setup Installers
- Uninstall old version first (optional)
- Run new installer
- Configuration preserved automatically

---

## 🗑️ Uninstalling

### Batch Installers
**Manual uninstall:**
1. Delete `C:\CashApp\Server\` or `C:\CashApp\Client\`
2. Delete shortcuts from Start Menu
3. Delete desktop shortcut
4. Remove firewall rules (optional):
   ```
   netsh advfirewall firewall delete rule name="Cash Server TCP"
   netsh advfirewall firewall delete rule name="Cash Server Discovery"
   ```

### Inno Setup Installers
**Automatic uninstall:**
1. Settings → Apps & Features
2. Find "Cash Drawer Server" or "Cash Drawer Client"
3. Click Uninstall
4. Follow prompts
5. Everything removed automatically

---

## ⚙️ Silent Installation (Advanced)

### Batch Installers
```batch
:: Create response file for unattended install
echo Y | Install_Server.bat
```

### Inno Setup Installers
```batch
:: Silent install with default options
CashDrawer_Server_v3.0_Setup.exe /VERYSILENT /NORESTART

:: Silent install with log
CashDrawer_Server_v3.0_Setup.exe /VERYSILENT /LOG="install.log"

:: Silent with custom directory
CashDrawer_Server_v3.0_Setup.exe /VERYSILENT /DIR="D:\MyPath"
```

**Inno Setup parameters:**
- `/SILENT` - Silent with progress
- `/VERYSILENT` - Completely silent
- `/NORESTART` - Don't restart computer
- `/LOG="file"` - Create installation log
- `/DIR="path"` - Custom installation directory
- `/TASKS="task1,task2"` - Select specific tasks

---

## ✅ Post-Installation Checklist

### Server
- [ ] Python installed and in PATH
- [ ] pyserial package installed
- [ ] COM port configured to COM10
- [ ] Computer rebooted after COM port change
- [ ] ServerConfig.py run and settings configured
- [ ] Admin password changed
- [ ] User accounts created
- [ ] Server starts without errors
- [ ] Firewall allows connections
- [ ] Relay tested (drawer opens)

### Client
- [ ] Python installed and in PATH
- [ ] Client launches successfully
- [ ] Servers discovered (or manually configured)
- [ ] Login successful
- [ ] Drawer opens when commanded
- [ ] Penny rounding working correctly

---

## 🐛 Troubleshooting

### Python Not Found
**Problem:** Installer can't find Python

**Solutions:**
1. Install Python: https://www.python.org/downloads/
2. Check "Add Python to PATH" during install
3. Verify: Open Command Prompt, type `python --version`
4. Reinstall if needed

### Permission Denied
**Problem:** Can't create directories or files

**Solution:**
- Right-click installer → "Run as administrator"

### Firewall Not Configured
**Problem:** Firewall rules not added

**Manual fix:**
```batch
netsh advfirewall firewall add rule name="Cash Server TCP" dir=in action=allow protocol=TCP localport=5000
netsh advfirewall firewall add rule name="Cash Server Discovery" dir=in action=allow protocol=UDP localport=5001
```

### Shortcuts Don't Work
**Problem:** Shortcuts created but don't launch

**Solutions:**
1. Check Python in PATH: `python --version`
2. Check file exists: `C:\CashApp\Server\CashServer.py`
3. Manually run: `python C:\CashApp\Server\CashServer.py`
4. Reinstall Python with PATH option

---

## 📞 Support

**Installation Issues:**
1. Check Python installation: `python --version`
2. Check file permissions (run as admin)
3. Review installation log/output
4. See FAQ.md for common issues

**Application Issues:**
1. See INSTALLATION_GUIDE.md
2. See FAQ.md
3. Check logs in `C:\CashApp\Server\Logs\`

---

## 🎁 Summary

### Batch Installers
✅ Simplest method  
✅ No build required  
✅ Works immediately  
✅ Best for: Quick deployments, small teams  

### Inno Setup Installers
✅ Professional installers  
✅ Better user experience  
✅ Automatic uninstall  
✅ Best for: Large deployments, professional environments  

**Both methods install the same application - choose what works for you!**

---

**Version**: 3.0  
**Last Updated**: January 2025  
**Status**: Production Ready
