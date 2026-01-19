# Cash Drawer Installers

## 🚀 Quick Start

### For Server Installation
**Right-click → Run as administrator:**
```
Install_Server.bat
```

### For Client Installation
**Right-click → Run as administrator:**
```
Install_Client.bat
```

**That's it!**

---

## 📦 What's Included

### Ready-to-Use Installers (Batch)
- **Install_Server.bat** - Server installer ⭐ **Use This**
- **Install_Client.bat** - Client installer ⭐ **Use This**

### Professional Installers (Inno Setup - Optional)
- **Server_Setup.iss** - Server installer script
- **Client_Setup.iss** - Client installer script

### Documentation
- **INSTALLER_GUIDE.md** - Complete installer guide

---

## 🎯 Which Installer Should I Use?

### Most Users → Batch Installers (.bat)
✅ Simplest  
✅ No prerequisites  
✅ Works immediately  

Just right-click and "Run as administrator"!

### IT Departments → Inno Setup (.iss)
Requires Inno Setup to compile, but creates professional .exe installers:
- Graphical installation wizard
- Add/Remove Programs integration
- Clean uninstall
- Custom branding

See `INSTALLER_GUIDE.md` for details.

---

## 📋 Installation Steps

### Server

1. **Right-click** `Install_Server.bat`
2. Select **"Run as administrator"**
3. Wait for installation (~30 seconds)
4. Configure COM port in Device Manager → COM10
5. **Reboot** computer
6. Run **Server Configuration** tool
7. Create user accounts
8. Start server

### Client

1. **Right-click** `Install_Client.bat`
2. Select **"Run as administrator"**  
3. Wait for installation (~20 seconds)
4. Launch **Cash Drawer** from Desktop
5. Let it auto-discover servers
6. Login with your credentials
7. Start using!

---

## ⚙️ What Gets Installed

### Server Installation
📁 **Location**: `C:\CashApp\Server\`

**Files:**
- CashServer.py (main server)
- ServerConfig.py (configuration GUI)
- UserManager.py (user management)
- Documentation

**Shortcuts:**
- Start Menu → Cash Drawer Server
- Desktop → Cash Server

**Configuration:**
- Firewall: TCP 5000, UDP 5001
- Logs: C:\CashApp\Server\Logs\

### Client Installation
📁 **Location**: `C:\CashApp\Client\`

**Files:**
- CashClient.py (client GUI)
- Documentation

**Shortcuts:**
- Start Menu → Cash Drawer
- Desktop → Cash Drawer

**Configuration:**
- Auto-discovery enabled
- Auto-connect enabled

---

## ✅ Requirements

- **Windows 10 or 11**
- **Python 3.8 or higher** (installer will prompt if missing)
- **Administrator rights** (for installation only)
- **Internet** (to download Python if needed)

---

## 🔧 Troubleshooting

### "Python not found"
→ Install Python: https://www.python.org/downloads/  
→ Check "Add Python to PATH"  
→ Run installer again

### "Access denied"
→ Right-click → "Run as administrator"

### Shortcuts don't work
→ Verify Python installed: `python --version`  
→ Reinstall with PATH option

### More help
→ See `INSTALLER_GUIDE.md`  
→ See `FAQ.md` in main folder

---

## 📖 Documentation

- **INSTALLER_GUIDE.md** - Complete installer documentation
- **../QUICK_START.md** - Quick setup guide
- **../FAQ.md** - Common questions
- **../INSTALLATION_GUIDE.md** - Manual installation

---

## 🎁 Features

### Batch Installers
- ✅ One-click installation
- ✅ Automatic Python package installation
- ✅ Firewall configuration
- ✅ Shortcut creation
- ✅ Post-install configuration helper
- ✅ Works immediately

### What It Does
1. Creates program directories
2. Copies all files
3. Checks Python installation
4. Installs Python packages
5. Configures Windows Firewall
6. Creates shortcuts
7. Offers to launch configuration tools

### What It Doesn't Do
- ❌ Install Python (prompts you to install)
- ❌ Configure COM port (you do this in Device Manager)
- ❌ Create users (use UserManager.py after install)

---

## 💡 Tips

### For Server Installation
1. Install Python **first** (saves time)
2. Configure COM port to COM10 **before** running server
3. **Reboot** after changing COM port
4. Use **ServerConfig.py** to configure settings
5. Change **admin password** immediately

### For Client Installation
1. Servers should be running first
2. Let auto-discovery find servers (easiest)
3. Or manually enter server IPs in Settings

### For Multiple Computers
1. Copy installer to network share
2. Run from each computer
3. Takes ~2 minutes per computer
4. Or use remote deployment (see INSTALLER_GUIDE.md)

---

## 📊 Installation Time

| Task | Time |
|------|------|
| Server install | 1-2 minutes |
| COM port config | 2-3 minutes |
| User creation | 1-2 minutes |
| **Total Server** | **5-7 minutes** |
| | |
| Client install | 1 minute |
| Configuration | Auto (30 seconds) |
| **Total Client** | **1-2 minutes** |

---

## 🔄 Updating

### To Update to Newer Version
1. Just run installer again
2. Files will be overwritten
3. Configuration preserved
4. No uninstall needed

---

## 🗑️ Uninstalling

### Batch Installers
**Manual removal:**
```
1. Delete C:\CashApp\Server\ or C:\CashApp\Client\
2. Delete shortcuts from Start Menu
3. Delete desktop shortcuts
```

### Firewall Rules (Optional)
```batch
netsh advfirewall firewall delete rule name="Cash Server TCP"
netsh advfirewall firewall delete rule name="Cash Server Discovery"
```

---

## 🎯 Success Checklist

### After Server Installation
- [ ] Python installed (check: `python --version`)
- [ ] pyserial installed (installer does this)
- [ ] Shortcuts created (Start Menu, Desktop)
- [ ] Firewall configured (ports 5000, 5001)
- [ ] COM port set to COM10
- [ ] Computer rebooted
- [ ] ServerConfig.py runs successfully
- [ ] Admin password changed
- [ ] User accounts created

### After Client Installation
- [ ] Python installed
- [ ] Shortcuts created
- [ ] Client launches
- [ ] Servers discovered
- [ ] Login works
- [ ] Can open drawer

---

## ℹ️ Additional Information

### File Locations
- Server: `C:\CashApp\Server\`
- Client: `C:\CashApp\Client\`
- Logs: `C:\CashApp\Server\Logs\`
- Docs: `C:\CashApp\Server\Docs\` or `C:\CashApp\Client\Docs\`

### Shortcuts
- Start Menu: Windows key → Type "Cash"
- Desktop: Look for icon
- Quick Launch: Pin to taskbar if desired

### Configuration Files
- Server: `server_config.ini` (created on first run)
- Client: `client_config.ini` (created on first run)
- Users: `users.json` (created by UserManager)

---

## 📞 Support

**Installation help:** See `INSTALLER_GUIDE.md`  
**Usage help:** See `../FAQ.md`  
**Setup help:** See `../QUICK_START.md`  

---

**Version:** 3.0  
**Type:** Windows Batch Installers  
**Status:** ✅ Production Ready  
**Tested:** Windows 10 & 11
