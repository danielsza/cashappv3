# Cash Drawer System - Quick Start Guide

## ⚡ 5-Minute Setup

### Prerequisites
1. Python 3.8+ installed
2. USB-to-Serial adapter connected

### Server Setup (2 minutes per server)

```bash
# 1. Install dependency
pip install pyserial

# 2. Navigate to server directory
cd C:\CashApp\Server

# 3. First run (creates config)
python CashServer.py
# (It will create config and exit)

# 4. Edit server_config.ini:
#    - Set ServerID (SERVER1 or SERVER2)
#    - Set COMPort (usually COM10)
#    - Set PeerServerHost (IP of other server)

# 5. Create users
python UserManager.py
# Choose option 2 to add users
# CHANGE the default admin password!

# 6. Start server
python CashServer.py
```

### Client Setup (1 minute per client)

```bash
# 1. Navigate to client directory
cd C:\CashApp\Client

# 2. Start client
python CashClient.py

# 3a. AUTO-DISCOVERY (if no servers configured):
#     Client automatically searches network
#     Displays found servers
#     Click "Yes" to use them
#     Done!

# 3b. MANUAL CONFIGURATION:
#     In Settings tab:
#     - Enter server IP addresses
#     - OR click "🔍 Discover Servers" button
#     - Save Settings
#     - Test Connection

# 4. Login with your credentials
```

## 🎯 Daily Operation

### Opening Cash Drawer

1. **Login**: Enter username and password
2. **Select document type**: Invoice, Petty Cash, Change, etc.
3. **Enter amounts**:
   - Total: Sale amount
   - IN: Cash received
   - OUT: (auto-calculated with Canadian penny rounding)
4. **Click "Open"**: Drawer opens and transaction is logged

**Note**: 🍁 Change is automatically rounded per Canadian penny rounding rules

### Quick Actions
- **BOD** button: Beginning of Day
- **EOD** button: End of Day

## 🔧 COM Port Setup

**Critical Steps:**
1. Open Device Manager
2. Find USB Serial device under "Ports"
3. Properties → Port Settings → Advanced
4. Change COM Port Number to COM10
5. **REBOOT** computer
6. Verify COM10 in Device Manager

**Note**: Windows Update will install drivers automatically.

## 🔐 User Management

```bash
python UserManager.py
```

Quick commands:
- **1**: List all users
- **2**: Add new user
- **3**: Change password
- **5**: Unlock locked account

## 📝 Default Credentials

**Username**: `admin`  
**Password**: `admin123`

⚠️ **CHANGE THIS IMMEDIATELY!**

## 🔄 Server Failover

The system automatically switches to secondary server if primary fails:
1. Client tries Primary Server
2. If fails → tries Secondary Server
3. Connection indicator shows active server

Both servers can run simultaneously!

## 📊 Logs Location

- **Network**: `\\partsrv2\Parts\Cash\`
- **Local**: `C:\CashApp\Server\logs\`

If network is unavailable, automatically uses local.

## ⚠️ Troubleshooting

### "Serial port not found"
→ Check COM port in Device Manager, reboot if just changed

### "Connection failed"
→ Verify server is running, check firewall (port 5000)

### "Account locked"
→ Run UserManager.py, option 5 to unlock

### "Can't write to network"
→ Check network path permissions, will use local logs automatically

## 🚀 Startup Options

### Double-click to start:
- Server: `StartServer.bat`
- Client: `StartClient.bat`

### Auto-start server on boot:
1. Press Win + R
2. Type: `shell:startup`
3. Copy shortcut to `StartServer.bat`

### Client auto-connect:
Enable in Settings tab: "Auto-connect on startup"

## 📞 Quick Reference

| Action | Location | Command |
|--------|----------|---------|
| Start Server | Server folder | `python CashServer.py` |
| Start Client | Client folder | `python CashClient.py` |
| Manage Users | Server folder | `python UserManager.py` |
| View Logs | Logs folder | Check `.log` files |
| Test Connection | Client Settings | "Test Connection" button |

## 🎓 Usage Tips

1. **Remember to login**: All actions require authentication
2. **Document types**: Select appropriate type for tracking
3. **Change calculation**: Automatic when you enter Total and IN
4. **Quick BOD/EOD**: Use buttons for daily operations
5. **Check logs**: Regular review of transaction logs
6. **Server redundancy**: Both servers log independently

## 📋 Configuration Files

**Server**: `server_config.ini`
- ServerID, Port, COM settings
- Peer server configuration
- Log paths

**Client**: `client_config.ini`
- Primary/Secondary server IPs
- Auto-connect settings
- Last username

## ✅ Success Indicators

**Server Running**:
```
Server SERVER1 is running on port 5000
Serial port COM10 initialized successfully
```

**Client Connected**:
- Status: "Connected" (green)
- Server shown in top-right
- Login tab accessible

**Transaction Success**:
- Bottom status bar shows timestamp
- Form clears automatically
- Entry appears in logs

## 🆘 Need Help?

1. Check the logs first
2. Verify COM port in Device Manager
3. Test network connectivity
4. Review configuration files
5. See full INSTALLATION_GUIDE.md for details
