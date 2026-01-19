# 💵 Cash Drawer Control System v3.0

**Complete Package - Ready to Deploy**

---

## 📦 What's In This Package

This is a complete cash drawer control system with client/server architecture, designed to replace your Version 2.0 Visual Basic application.

### 🎯 Quick Start

**First time?** → Read `QUICK_START.md` (5 minutes)

**Need help?** → Read `FAQ.md` (answers all common questions)

**Ready to install?** → Follow `INSTALLATION_GUIDE.md`

---

## 📂 Directory Structure

```
CashApp/
│
├── 📄 START_HERE.md                 ← You are here!
├── 📄 README.md                     ← Complete documentation
├── 📄 QUICK_START.md                ← 5-minute setup guide
├── 📄 INSTALLATION_GUIDE.md         ← Detailed installation
├── 📄 DEPLOYMENT_CHECKLIST.md       ← Deployment helper
├── 📄 FAQ.md                        ← Frequently asked questions
├── 📄 BUILD_SUMMARY.md              ← What was built
├── 📄 CHANGELOG.md                  ← Version history
├── 📄 PENNY_ROUNDING.md             ← Canadian rounding rules
├── 📄 SERVER_DISCOVERY.md           ← Auto-discovery guide
├── 📄 MACOS_COMPATIBILITY.md        ← macOS client guide
├── 📄 requirements.txt              ← Python dependencies
├── 📄 Test_Penny_Rounding.py        ← Penny rounding tests
│
├── 📁 Server/                       ← Server files (2 servers)
│   ├── CashServer.py               ← Main server application
│   ├── ServerConfig.py             ← GUI configuration tool
│   ├── UserManager.py              ← User management utility
│   ├── StartServer.bat             ← Start server (Windows)
│   └── ConfigureServer.bat         ← Configure server (Windows)
│
└── 📁 Client/                       ← Client files (all workstations)
    ├── CashClient.py               ← Client GUI application
    └── StartClient.bat             ← Start client (Windows)
```

---

## 🚀 Installation Overview

### Server Setup (15 minutes per server)

1. **Install Python 3.11+** from python.org
2. **Install pyserial**: `pip install pyserial`
3. **Copy Server folder** to `C:\CashApp\Server\`
4. **Configure COM port** to COM10 in Device Manager
5. **Run** `ConfigureServer.bat` to configure settings
6. **Create users** with `UserManager.py`
7. **Start server** with `StartServer.bat`

### Client Setup (5 minutes per client)

1. **Install Python 3.11+** from python.org
2. **Copy Client folder** to `C:\CashApp\Client\`
3. **Run** `StartClient.bat`
4. **Auto-discovery** finds servers automatically!
5. **Login** and start using

---

## 📚 Documentation Guide

### For Quick Setup
1. ⚡ **QUICK_START.md** - Fast 5-minute setup
2. ❓ **FAQ.md** - Common questions answered

### For Complete Installation
1. 📖 **INSTALLATION_GUIDE.md** - Step-by-step guide
2. ✅ **DEPLOYMENT_CHECKLIST.md** - Don't miss anything

### For Understanding Features
1. 🍁 **PENNY_ROUNDING.md** - Canadian rounding explained
2. 🔍 **SERVER_DISCOVERY.md** - Auto-discovery explained
3. 🍎 **MACOS_COMPATIBILITY.md** - Using client on macOS

### For Reference
1. 📘 **README.md** - Complete technical documentation
2. 📝 **CHANGELOG.md** - Version history
3. 📋 **BUILD_SUMMARY.md** - What's included

---

## 🎯 Key Features

✅ **Client/Server Architecture** - Hardware control centralized  
✅ **Dual Server Failover** - Automatic backup server  
✅ **Auto-Discovery** - Finds servers automatically  
✅ **Canadian Penny Rounding** - Built-in rounding  
✅ **Password Authentication** - Secure user accounts  
✅ **GUI Configuration** - Easy server setup  
✅ **Cross-Platform Client** - Windows, macOS, Linux  
✅ **Comprehensive Logging** - Network + local fallback  

---

## 🔧 What You Need

### Hardware
- 2 Windows computers for servers
- USB-to-Serial adapters (COM ports)
- Relay modules
- Cash drawers
- Any computers for clients (Windows/macOS/Linux)

### Software
- Python 3.8+ (3.11 recommended)
- pyserial library (`pip install pyserial`)
- Network connectivity

### Network
- Same local network for auto-discovery
- Firewall: Allow TCP 5000 and UDP 5001
- Optional: Static IPs for servers

---

## ⚙️ Configuration Tools

### ServerConfig.py (NEW!)
**Graphical configuration for servers** - No more manual INI editing!

**Features:**
- ✅ COM port detection and selection
- ✅ Relay settings (pin type, duration)
- ✅ Test relay button (opens drawer!)
- ✅ Network configuration
- ✅ Logging paths
- ✅ Security settings

**Run:** `ConfigureServer.bat` or `python ServerConfig.py`

### UserManager.py
**Manage users from command line**

**Features:**
- Add/remove users
- Change passwords
- Unlock accounts
- View user list

**Run:** `python UserManager.py`

---

## 🎓 Training Materials

### For End Users
1. How to login
2. How to open cash drawer
3. Using BOD/EOD buttons
4. Understanding penny rounding

### For Administrators
1. Creating user accounts
2. Changing passwords
3. Unlocking accounts
4. Viewing logs
5. Testing COM ports
6. Configuring servers

### For IT Staff
1. Server installation
2. COM port configuration
3. Network configuration
4. Troubleshooting
5. Firewall setup
6. Auto-discovery setup

---

## 🔐 Security

**Default Credentials:**
- Username: `admin`
- Password: `admin123`

⚠️ **CHANGE IMMEDIATELY AFTER FIRST LOGIN!**

**Security Features:**
- SHA-256 password hashing
- Account lockout (3 attempts, 5 minutes)
- Session tokens
- All activity logged
- Configurable security settings

---

## 📞 Support

### Something Not Working?

1. **Check FAQ.md** first (answers 90% of questions)
2. **Check logs** in `C:\CashApp\Server\logs\`
3. **Verify COM port** in Device Manager
4. **Test connectivity** with built-in tools
5. **Review documentation** for your issue

### Common Issues & Solutions

**"No COM ports found"**
→ Install USB-Serial drivers via Windows Update

**"Can't connect to server"**
→ Click "🔍 Discover Servers" in Settings tab

**"Account locked"**
→ Run `UserManager.py`, option 5 to unlock

**"Drawer won't open"**
→ Use ServerConfig.py "Test Relay" button

**"Wrong change amount"**
→ Penny rounding is working correctly! See PENNY_ROUNDING.md

---

## 🎁 What's New in v3.0

Compared to your old Version 2.0 VB application:

### Architecture
- ❌ Single app → ✅ Client/server
- ❌ Hardcoded codes → ✅ Password authentication
- ❌ Manual IP entry → ✅ Auto-discovery
- ❌ Single server → ✅ Dual server failover

### Features
- ✅ Canadian penny rounding
- ✅ GUI configuration tools
- ✅ Cross-platform client (macOS!)
- ✅ Configurable relay timing
- ✅ Comprehensive logging
- ✅ User management utility

### Usability
- ✅ Automatic server discovery
- ✅ No more hardcoded settings
- ✅ Easy configuration
- ✅ Complete documentation
- ✅ Testing tools included

---

## 📋 Pre-Deployment Checklist

Before deploying to production:

### Hardware
- [ ] USB-Serial adapters purchased
- [ ] Adapters configured as COM10
- [ ] Relay modules connected
- [ ] Cash drawers tested

### Software
- [ ] Python installed on all computers
- [ ] pyserial installed (`pip install pyserial`)
- [ ] Files copied to correct locations
- [ ] COM ports configured and tested

### Configuration
- [ ] Server configs created (use ServerConfig.py)
- [ ] Users created (use UserManager.py)
- [ ] Admin password changed
- [ ] Network paths accessible
- [ ] Firewall rules added

### Testing
- [ ] Servers start without errors
- [ ] Clients can discover servers
- [ ] Login works for all users
- [ ] Drawer opens when commanded
- [ ] Transactions logged correctly
- [ ] Failover works (stop primary server)

### Documentation
- [ ] Users trained on new system
- [ ] Admins know how to manage users
- [ ] IT staff familiar with troubleshooting
- [ ] Server IPs documented
- [ ] Emergency contacts listed

**Use DEPLOYMENT_CHECKLIST.md for detailed walkthrough**

---

## 🚦 Deployment Order

### Phase 1: Server Setup (Day 1)
1. Install Server 1 (primary)
2. Configure and test
3. Create user accounts
4. Install Server 2 (secondary)
5. Test server failover

### Phase 2: Client Rollout (Day 2)
1. Install 1-2 test clients
2. Train test users
3. Verify all functions
4. Install remaining clients
5. Train all users

### Phase 3: Go Live (Day 3)
1. Run in parallel with old system (if still available)
2. Monitor logs for issues
3. Collect user feedback
4. Address any problems
5. Decommission old system

### Phase 4: Stabilization (Week 1)
1. Daily log review
2. User support
3. Fine-tune settings
4. Document lessons learned
5. Celebrate success! 🎉

---

## 📊 File Manifest

### Documentation (8 files)
- START_HERE.md (this file)
- README.md
- QUICK_START.md
- INSTALLATION_GUIDE.md
- DEPLOYMENT_CHECKLIST.md
- FAQ.md
- BUILD_SUMMARY.md
- CHANGELOG.md
- PENNY_ROUNDING.md
- SERVER_DISCOVERY.md
- MACOS_COMPATIBILITY.md

### Server Applications (3 files)
- CashServer.py (main server)
- ServerConfig.py (GUI configuration)
- UserManager.py (user management)

### Client Application (1 file)
- CashClient.py (GUI client)

### Support Files (5 files)
- requirements.txt
- StartServer.bat
- StartClient.bat
- ConfigureServer.bat
- Test_Penny_Rounding.py

**Total: 18 files** + comprehensive documentation

---

## 💡 Tips for Success

1. **Start Small**: Install 1 server + 1 client first
2. **Test Thoroughly**: Use "Test Relay" button extensively
3. **Document Everything**: Write down IPs, settings, passwords
4. **Train Users**: Show them the new interface
5. **Monitor Logs**: Check daily for the first week
6. **Be Patient**: Allow time for users to adjust
7. **Keep Backup**: Don't delete old system immediately

---

## 🎯 Success Criteria

You'll know it's working when:

- ✅ Servers start with "Serial port COM10 initialized successfully"
- ✅ Clients show green "Connected" status
- ✅ Users can login with their passwords
- ✅ Drawer opens on command
- ✅ Transactions appear in logs
- ✅ Failover works when primary server stops
- ✅ Logs write to network share
- ✅ Change is calculated with penny rounding
- ✅ No errors in server console
- ✅ Users are happy! 😊

---

## 📧 Feedback

This is Version 3.0 - a complete rewrite. If you find issues or have suggestions:

1. Check FAQ.md first
2. Review documentation
3. Check logs for error messages
4. Document the issue clearly
5. Note any error messages

---

## 🏆 Version Information

- **Version**: 3.0.0
- **Release Date**: January 19, 2025
- **Status**: Production Ready
- **Platform**: Windows (server), Windows/macOS/Linux (client)
- **Language**: Python 3.8+
- **License**: Proprietary - Internal Use

---

## 🙏 Acknowledgments

Built to replace Version 2.0 Visual Basic application (source code lost).

Key improvements:
- Modern architecture
- Better security
- Easier configuration
- Auto-discovery
- Cross-platform support
- Comprehensive documentation

---

## 🎬 Ready to Start?

### Quickest Path to Success:

1. **Read FAQ.md** (10 minutes) - Answers all your questions
2. **Follow QUICK_START.md** (15 minutes) - Get one server running
3. **Install one client** (5 minutes) - Test the whole system
4. **Scale up** - Install remaining servers and clients

### Need More Detail?

→ **INSTALLATION_GUIDE.md** for step-by-step instructions  
→ **DEPLOYMENT_CHECKLIST.md** to ensure nothing is missed

---

## 📖 Documentation Quick Reference

| Document | When to Read |
|----------|-------------|
| **START_HERE.md** | Right now (you're reading it!) |
| **FAQ.md** | Before asking questions |
| **QUICK_START.md** | Want to start in 5 minutes |
| **INSTALLATION_GUIDE.md** | Need detailed instructions |
| **DEPLOYMENT_CHECKLIST.md** | During deployment |
| **README.md** | Need technical details |
| **PENNY_ROUNDING.md** | Understanding rounding |
| **SERVER_DISCOVERY.md** | Auto-discovery not working |
| **MACOS_COMPATIBILITY.md** | Using macOS clients |
| **BUILD_SUMMARY.md** | Want overview of what was built |
| **CHANGELOG.md** | Want version history |

---

**Welcome to Cash Drawer Control System v3.0!** 🎉

**Everything you need is in this package. Let's get started!**

---

*Last Updated: January 19, 2025*  
*Package Version: 3.0.0*  
*Ready for Production Deployment*
