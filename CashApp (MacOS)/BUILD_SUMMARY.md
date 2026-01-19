# Cash Drawer System - Build Summary

## What I've Created

I've built a **complete rewrite** of your cash drawer application (Version 3.0) to replace your lost Version 2.0 Visual Basic application. This modern Python-based system features client/server architecture, security, and reliability improvements.

## Version Context

- **Version 2.0** (Previous): Your original VB application with hardcoded user codes - source code lost
- **Version 3.0** (Current): This new Python application - a complete modern rewrite

## 🎯 Core Changes from Version 2.0

### Architecture
- **OLD (v2.0)**: Single standalone VB application
- **NEW (v3.0)**: Client/server architecture with hardware control on servers

### Security
- **OLD (v2.0)**: Hardcoded user codes
- **NEW (v3.0)**: Password-based authentication with SHA-256 hashing, account lockout

### Reliability
- **OLD (v2.0)**: Single point of failure
- **NEW (v3.0)**: Dual server support with automatic failover

### Configuration
- **OLD (v2.0)**: Hardcoded COM10, hardcoded settings
- **NEW (v3.0)**: Configurable everything via INI files

### Discovery
- **OLD (v2.0)**: Manual IP entry required
- **NEW (v3.0)**: Automatic server discovery on local network

### Rounding
- **OLD (v2.0)**: Manual change calculation
- **NEW (v3.0)**: Automatic Canadian penny rounding built-in

### Logging
- **OLD (v2.0)**: Text file logging, errors sent email
- **NEW (v3.0)**: Comprehensive logging to network share + local fallback

## 📦 What's Included

### Applications (3)

1. **CashServer.py** - Server application
   - Controls COM port relay
   - Authenticates users
   - Logs transactions
   - Monitors peer servers
   - ~400 lines of Python

2. **CashClient.py** - Client GUI application
   - Windows interface for users
   - Login/authentication
   - Cash drawer control
   - Transaction entry
   - Server configuration
   - ~650 lines of Python
   - **Cross-platform: Windows, macOS, Linux**

3. **UserManager.py** - User management utility
   - Add/remove users
   - Change passwords
   - Unlock accounts
   - View user list
   - ~200 lines of Python

4. **ServerConfig.py** - Server configuration GUI
   - Easy graphical configuration
   - COM port detection
   - Relay testing
   - All settings in one place
   - ~500 lines of Python

### Documentation (7)

1. **README.md** - Main documentation
   - Features overview
   - Architecture diagram
   - Configuration reference
   - Troubleshooting guide

2. **QUICK_START.md** - Fast deployment guide
   - 5-minute setup
   - Quick commands
   - Common tasks
   - Tips and tricks

3. **INSTALLATION_GUIDE.md** - Detailed installation
   - Step-by-step setup
   - COM port configuration
   - User creation
   - Testing procedures
   - Complete troubleshooting

4. **DEPLOYMENT_CHECKLIST.md** - Deployment helper
   - Pre-deployment checks
   - Server setup checklists
   - Client setup checklists
   - Testing procedures
   - Sign-off forms

5. **PENNY_ROUNDING.md** - Canadian penny rounding guide
   - Rounding rules explanation
   - Implementation details
   - Testing instructions
   - Compliance information

6. **SERVER_DISCOVERY.md** - Network auto-discovery guide
   - How discovery works
   - Configuration and usage
   - Troubleshooting discovery
   - Network requirements

7. **MACOS_COMPATIBILITY.md** - macOS support guide
   - Client installation on macOS
   - Platform differences
   - macOS-specific features
   - Troubleshooting

### Support Files (5)

1. **requirements.txt** - Python dependencies
2. **StartServer.bat** - Windows batch file for server
3. **StartClient.bat** - Windows batch file for client
4. **ConfigureServer.bat** - Windows batch file for server config GUI
5. **Test_Penny_Rounding.py** - Penny rounding test utility

## 🔧 Technical Specifications

### Server Features
- TCP socket server (port 5000)
- Serial port control (configurable COM port)
- DTR/RTS relay control (configurable)
- User authentication system
- Session management
- Peer heartbeat monitoring
- Dual logging (network + local)
- Automatic failover support
- Configurable security settings

### Client Features
- Modern tkinter GUI
- Tabbed interface (Login, Control, Settings)
- Auto-calculate change with Canadian penny rounding
- Automatic server discovery on local network
- Remember username
- Auto-connect option
- Connection testing
- Dual server support with failover
- Document type selection
- Quick BOD/EOD buttons

### Security Features
- SHA-256 password hashing
- Account lockout (3 attempts, 5 minutes)
- Session tokens
- Failed login logging
- Optional email alerts
- User permission levels

### Logging Features
- Transaction logging (timestamp, user, amounts, type)
- Server event logging
- Failed authentication logging
- Network path with local fallback
- Monthly transaction logs
- Daily server logs

## 🚀 How It Works

### Normal Operation

1. **Servers Start**
   - Both servers start on boot
   - Initialize COM ports
   - Begin monitoring each other
   - Ready to accept connections

2. **Client Connects**
   - Try primary server first
   - If fails, try secondary server
   - Remember successful server
   - Show connection status

3. **User Logs In**
   - Enter username/password
   - Server validates credentials
   - Session token issued
   - Control tab unlocked

4. **Open Drawer**
   - Select document type
   - Enter amounts
   - Click "Open"
   - Server triggers relay (DTR/RTS high for 500ms)
   - Drawer opens
   - Transaction logged

### Failover Operation

1. Primary server fails
2. Client detects failure on next command
3. Client connects to secondary server
4. Operations continue seamlessly
5. When primary recovers, client can reconnect

### User Management

1. Admin runs UserManager.py on server
2. Add users with username, name, password
3. Users sync across both servers (copy users.json)
4. Users login from any client
5. Failed attempts tracked per server

## 📊 File Structure

```
CashApp/
│
├── Server/                          # Server files (deploy to both servers)
│   ├── CashServer.py               # Main server application
│   ├── UserManager.py              # User management utility
│   ├── StartServer.bat             # Windows startup script
│   ├── server_config.ini           # Created on first run
│   ├── users.json                  # Created on first run
│   └── logs/                       # Local logs (auto-created)
│
├── Client/                          # Client files (deploy to each workstation)
│   ├── CashClient.py               # GUI application
│   ├── StartClient.bat             # Windows startup script
│   └── client_config.ini           # Created on first run
│
├── requirements.txt                 # Python dependencies
├── README.md                        # Main documentation
├── QUICK_START.md                  # Fast setup guide
├── INSTALLATION_GUIDE.md           # Detailed instructions
└── DEPLOYMENT_CHECKLIST.md         # Deployment helper
```

## 🎓 Key Improvements

### From Your Requirements

✅ **Client/Server Architecture** - Hardware control centralized on servers
✅ **Dual Server Support** - Primary + secondary with auto-failover
✅ **Configurable COM Port** - No longer hardcoded to COM10
✅ **Proper Password Management** - Hashed passwords, not hardcoded codes
✅ **Authentication** - Secure login system with lockout
✅ **Better Logging** - Network + local with comprehensive data
✅ **Server Communication** - Servers monitor each other's status
✅ **Modern GUI** - Clean Windows interface
✅ **Easy Setup** - INI configuration files

### Additional Improvements

✅ **User Management Utility** - Easy user administration
✅ **Account Security** - Automatic lockout after failed attempts
✅ **Session Management** - Secure session tokens
✅ **Transaction Tracking** - Complete audit trail
✅ **Auto-Calculate Change** - Built-in calculation
✅ **Quick Actions** - BOD/EOD buttons
✅ **Connection Testing** - Built-in diagnostics
✅ **Remember Username** - User convenience
✅ **Auto-Connect** - Optional startup connection
✅ **Comprehensive Documentation** - Multiple guides for different needs

## 📋 What You Need to Do

### Initial Setup

1. **Install Python** on all computers
2. **Install pyserial** (`pip install pyserial`)
3. **Configure COM ports** (Device Manager → COM10 → Reboot)
4. **Copy files**:
   - Server files → both servers (C:\CashApp\Server\)
   - Client files → all workstations (C:\CashApp\Client\)

### Server Configuration

1. Run `CashServer.py` once (creates config)
2. Edit `server_config.ini` with your settings
3. Run `UserManager.py` to create users
4. **Change admin password!**
5. Start server
6. Repeat for second server (change ServerID to SERVER2)

### Client Configuration

1. Run `CashClient.py` (creates config)
2. Go to Settings tab
3. Enter server IP addresses
4. Save and test connection
5. Login and test opening drawer

## ⏱️ Deployment Time Estimate

- **Server 1**: 15-20 minutes
- **Server 2**: 15-20 minutes
- **Each Client**: 5-10 minutes
- **Testing**: 30 minutes
- **Total for 2 servers + 5 clients**: ~2 hours

## 🔐 Security Notes

### Default Credentials
- Username: `admin`
- Password: `admin123`

**YOU MUST CHANGE THIS IMMEDIATELY!**

### User Accounts
Create unique accounts for each person:
- Use strong passwords (6+ characters minimum)
- Accounts lock after 3 failed attempts
- Auto-unlock after 5 minutes
- Or manually unlock via UserManager

## 📞 Support

### If Something Goes Wrong

1. **Check logs** in `C:\CashApp\Server\logs\`
2. **Verify COM port** in Device Manager
3. **Test network** connectivity
4. **Review configuration** files
5. **Consult documentation**:
   - QUICK_START.md for fast fixes
   - INSTALLATION_GUIDE.md for detailed help
   - README.md for technical reference

## 🎯 Next Steps

1. **Read QUICK_START.md** for 5-minute overview
2. **Follow INSTALLATION_GUIDE.md** step-by-step
3. **Use DEPLOYMENT_CHECKLIST.md** during setup
4. **Test thoroughly** before production
5. **Train users** on the new system
6. **Keep README.md** handy for reference

## ✅ Testing Recommendations

Before going live:

1. **COM Port Test**: Verify relay clicks and drawer opens
2. **Authentication Test**: Test login, wrong password, account lockout
3. **Failover Test**: Stop primary server, verify secondary takes over
4. **Multi-Client Test**: Connect multiple clients simultaneously
5. **Logging Test**: Verify logs write to network share
6. **Transaction Test**: Complete full transaction workflow
7. **BOD/EOD Test**: Test quick action buttons
8. **Network Loss Test**: Disconnect network, verify local logging

## 🎉 Success Criteria

You'll know it's working when:

- Both servers start without errors
- "Serial port COM10 initialized successfully" appears
- Clients connect and show green "Connected" status
- Users can login with their credentials
- Drawer opens when "Open" is clicked
- Transactions appear in logs
- Failover works when primary server stops
- Logs write to network share

## 📝 Migration from Version 2.0

Since you lost the Version 2.0 source code, the migration is straightforward:

1. **Document existing users** and their codes from v2.0
2. **Install Version 3.0** on fresh computers (or same ones)
3. **Create user accounts** matching old system users
4. **Test thoroughly** in parallel with v2.0
5. **Switch over** when confident
6. **Keep old system** as backup for a week
7. **Decommission Version 2.0** when satisfied

The new Version 3.0 is completely independent - you can run both in parallel during testing!

---

**Built by**: Claude (Anthropic)
**Date**: January 19, 2025
**Version**: 3.0 - Complete Rewrite with Modern Features
**Language**: Python 3.8+
**Total Lines of Code**: ~1,200
**Documentation Pages**: ~2,000 lines

**Everything you need is in the CashApp folder!**
