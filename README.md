# Cash Drawer Control System

A modern, secure client/server application for controlling cash drawers with user authentication, dual-server failover, and comprehensive logging.

## 🌟 Features

- **Client/Server Architecture**: Centralized hardware control with multiple client support
- **Dual Server Failover**: Automatic switchover to secondary server if primary fails
- **Automatic Server Discovery**: Finds servers on local network automatically
- **User Authentication**: Secure password-based authentication with SHA-256 hashing
- **Account Security**: Auto-lockout after failed login attempts
- **Hardware Control**: Serial port (COM) relay control for cash drawer
- **Comprehensive Logging**: All transactions logged to network share and local storage
- **Modern GUI**: Clean, intuitive Windows interface with tabbed design
- **Flexible Configuration**: Easy-to-edit INI configuration files
- **Auto-Calculate**: Automatic change calculation
- **Canadian Penny Rounding**: Proper cash rounding per Canadian rules (.01/.02→.00, .03/.04→.05, .06/.07→.05, .08/.09→.10)
- **Quick Actions**: One-click BOD (Beginning of Day) and EOD (End of Day) operations
- **Transaction Tracking**: Complete audit trail with timestamps, users, and amounts

## 📋 System Requirements

- **Operating System**: Windows 11 or Windows 10
- **Python**: Version 3.8 or higher
- **Hardware**: USB-to-Serial adapter (configured as COM port)
- **Network**: TCP/IP connectivity between clients and servers
- **Storage**: Access to network share for logging (optional, falls back to local)

## 📦 Components

### Server Application (`CashServer.py`)
- Handles hardware control (COM port relay)
- Manages user authentication
- Processes client requests
- Logs all transactions
- Monitors peer server health
- Supports simultaneous connections

### Client Application (`CashClient.py`)
- Windows GUI for user interaction
- Connects to primary or secondary server
- User login/logout
- Cash drawer control
- Transaction entry
- Configuration management

### User Management (`UserManager.py`)
- Add/remove users
- Change passwords
- Unlock accounts
- View user status

## 🚀 Quick Start

### Installation

```bash
# Install Python dependency
pip install -r requirements.txt

# Or manually
pip install pyserial
```

### Server Setup

```bash
cd Server

# First run - creates configuration
python CashServer.py

# Edit server_config.ini with your settings

# Create users
python UserManager.py

# Start server
python CashServer.py
```

### Client Setup

```bash
cd Client

# Start client
python CashClient.py

# Configure servers in Settings tab
# Login with your credentials
```

### COM Port Configuration

1. Open **Device Manager** (Win + X, then M)
2. Locate your USB-Serial device under "Ports (COM & LPT)"
3. Right-click → Properties → Port Settings → Advanced
4. Change COM Port Number to **COM10** (or your preferred port)
5. Click OK and **REBOOT** the computer
6. Verify the COM port appears correctly after reboot

**Note**: Windows Update will automatically find and install the correct drivers.

## 📖 Documentation

- **[QUICK_START.md](QUICK_START.md)**: 5-minute setup guide
- **[INSTALLATION_GUIDE.md](INSTALLATION_GUIDE.md)**: Complete installation and configuration
- **[Technical Details](#technical-details)**: Architecture and implementation details

## 🔐 Default Credentials

**Username**: `admin`  
**Password**: `admin123`

⚠️ **IMPORTANT**: Change this password immediately after first login!

## 🏗️ Architecture

```
┌──────────────────┐
│  Client Computer │
│   (GUI App)      │
└────────┬─────────┘
         │
         │ TCP/IP (Port 5000)
         │
    ┌────▼────────────────┐
    │  Auto-Failover      │
    │  Primary/Secondary  │
    └────┬────────┬───────┘
         │        │
    ┌────▼────┐  │  ┌─────▼────┐
    │ Server1 │  │  │ Server2  │
    │ COM10   │◄─┴─►│ COM10    │
    │ Relay   │     │ Relay    │
    └─────────┘     └──────────┘
         │               │
         ▼               ▼
    Cash Drawer    Cash Drawer
```

## 💾 File Structure

```
CashApp/
├── Server/
│   ├── CashServer.py          # Main server application
│   ├── UserManager.py         # User management utility
│   ├── StartServer.bat        # Windows startup script
│   ├── server_config.ini      # Server configuration (created on first run)
│   ├── users.json             # User database (created on first run)
│   └── logs/                  # Local log storage
│
├── Client/
│   ├── CashClient.py          # Client GUI application
│   ├── StartClient.bat        # Windows startup script
│   └── client_config.ini      # Client configuration (created on first run)
│
├── requirements.txt           # Python dependencies
├── README.md                  # This file
├── QUICK_START.md            # Quick setup guide
└── INSTALLATION_GUIDE.md     # Detailed installation guide
```

## 🔧 Configuration

### Server Configuration (`server_config.ini`)

```ini
[Server]
ServerID = SERVER1                    # Unique server identifier
Port = 5000                           # Server listening port
PeerServerHost = 192.168.1.X          # IP of peer server
PeerServerPort = 5000                 # Peer server port
COMPort = COM10                       # Serial port for relay
RelayPin = DTR                        # DTR or RTS
LogPath = \\partsrv2\Parts\Cash      # Network log path
LocalLogPath = ./logs                 # Local log fallback

[Security]
MaxFailedAttempts = 3                 # Lockout threshold
LockoutDuration = 300                 # Lockout time (seconds)
SessionTimeout = 3600                 # Session timeout
```

### Client Configuration (`client_config.ini`)

```ini
[Client]
PrimaryServer = 192.168.1.10          # Primary server IP (or leave empty to auto-discover)
PrimaryPort = 5000                    # Primary server port
SecondaryServer = 192.168.1.11        # Secondary server IP (or leave empty)
SecondaryPort = 5000                  # Secondary server port
AutoConnect = false                   # Auto-connect on startup
RememberUsername = true               # Remember last username
```

**Note**: If no servers are configured, the client will automatically search the network for available servers on first run. You can also manually discover servers using the "🔍 Discover Servers" button in the Settings tab.

## 📊 Logging

All transactions are logged with the following information:
- Timestamp
- Server ID
- Username
- Reason (Transaction, BOD, EOD, etc.)
- Document type (Invoice, Petty Cash, etc.)
- Total amount
- Amount IN
- Amount OUT (change)

### Log Locations

**Primary**: `\\partsrv2\Parts\Cash\`
- `CashServer_SERVERID_YYYYMMDD.log` - Server events
- `Transactions_YYYYMM.log` - Transaction log

**Fallback**: `C:\CashApp\Server\logs\` (if network unavailable)

## 🔐 Security Features

1. **Password Hashing**: SHA-256 hashing for all passwords
2. **Account Lockout**: Automatic lockout after 3 failed attempts (5 minutes)
3. **Session Tokens**: Secure session management
4. **Audit Trail**: Complete logging of all actions
5. **User Levels**: Admin and User permission levels
6. **Failed Login Alerts**: Optional email alerts for security events

## 🎯 Usage Examples

### Opening Drawer for Sale

1. Login with credentials
2. Select "Invoice"
3. Enter Total: `45.50`
4. Enter IN: `50.00`
5. OUT auto-calculates: `4.50`
6. Click "Open"

**Note**: Change is automatically rounded using Canadian penny rounding rules:
- Total: $45.53, Cash: $50.00 → Change: $4.45 (rounded from $4.47)
- Total: $45.58, Cash: $50.00 → Change: $4.40 (rounded from $4.42)

### Beginning of Day

1. Login
2. Click "BOD" button
3. Drawer opens and logs BOD transaction

### Adding New User

```bash
python UserManager.py
# Select option 2
# Enter username, name, level, password
```

## 🐛 Troubleshooting

### Common Issues

**COM Port Not Found**
- Verify USB adapter is connected
- Check Device Manager for COM port number
- Install drivers via Windows Update
- **Reboot** after changing COM port assignment

**Connection Failed**
- Verify server is running
- Check Windows Firewall (allow port 5000)
- Ping server IP address
- Use "Test Connection" in client

**Account Locked**
- Run `UserManager.py`
- Select option 5 to unlock
- Or wait 5 minutes for auto-unlock

**Network Logs Not Writing**
- Check network path exists: `\\partsrv2\Parts\Cash`
- Verify write permissions
- System automatically falls back to local logs

## 🔄 Auto-Startup

### Server Auto-Start

**Method 1: Startup Folder**
1. Press Win + R, type `shell:startup`
2. Create shortcut to `StartServer.bat`

**Method 2: Task Scheduler**
1. Open Task Scheduler
2. Create Basic Task
3. Trigger: At startup
4. Action: Start program → `python.exe`
5. Arguments: `C:\CashApp\Server\CashServer.py`
6. Start in: `C:\CashApp\Server`

### Client Auto-Connect

Enable in Settings tab:
- ☑ Auto-connect on startup
- Save Settings

## 📈 Future Enhancements

Potential improvements for future versions:
- [ ] Email alerts for security events
- [ ] Web-based admin interface
- [ ] Database backend (SQLite/PostgreSQL)
- [ ] SSL/TLS encryption
- [ ] Multi-level user permissions
- [ ] Transaction reports and analytics
- [ ] Receipt printer integration
- [ ] Barcode scanner support
- [ ] Mobile client app
- [ ] Cloud logging/backup

## 📜 Version History

### Version 3.0 (January 2025) - Current
**Major rewrite with modern features**
- ✅ Complete client/server architecture
- ✅ Dual server support with automatic failover
- ✅ **Automatic server discovery on local network**
- ✅ **Canadian penny rounding for cash transactions**
- ✅ User authentication with password hashing
- ✅ Account lockout after failed attempts
- ✅ Comprehensive logging (network + local)
- ✅ Modern Windows GUI with tabbed interface
- ✅ Configurable everything (COM port, relay, servers)
- ✅ Quick BOD/EOD buttons
- ✅ Auto-calculate change
- ✅ Server health monitoring
- ✅ Complete documentation suite

### Version 2.0 (Previous - Lost Source)
**Original Visual Basic application**
- Single standalone application
- Hardcoded user codes
- Hardcoded COM10 port
- Text file logging
- Email alerts on errors
- Source code no longer available

### Version 3.0 Key Improvements Over 2.0
- **Architecture**: Client/server vs. standalone
- **Security**: Password authentication vs. hardcoded codes
- **Reliability**: Dual server failover vs. single point of failure
- **Configuration**: INI files vs. hardcoded values
- **Discovery**: Auto-find servers vs. manual IP entry
- **Rounding**: Canadian penny rules built-in
- **User Management**: Add/remove users vs. code recompilation
- **Logging**: Network + local fallback vs. network only
- **Platform**: Python (cross-platform) vs. VB (Windows only)

## 🤝 Contributing

This is a custom application for specific hardware. If you need modifications:
1. Edit source files directly
2. Test thoroughly before deployment
3. Update configuration as needed
4. Document changes in comments

## 📝 Technical Details

### Communication Protocol

Client and server communicate via JSON over TCP sockets:

```json
// Authentication Request
{
  "command": "authenticate",
  "username": "daniel",
  "password": "password123"
}

// Authentication Response
{
  "status": "success",
  "session_token": "abc123...",
  "user_info": {
    "name": "Daniel",
    "level": "admin"
  },
  "server_id": "SERVER1"
}

// Open Drawer Request
{
  "command": "open_drawer",
  "reason": "Transaction",
  "document_type": "Invoice",
  "total": 45.50,
  "amount_in": 50.00,
  "amount_out": 4.50
}

// Open Drawer Response
{
  "status": "success",
  "message": "Drawer opened successfully"
}
```

### Serial Port Control

The relay is controlled via DTR or RTS pin:

```python
serial_port.dtr = True   # Energize relay
time.sleep(0.5)          # Hold for 500ms
serial_port.dtr = False  # De-energize relay
```

### Failover Logic

1. Client attempts Primary Server
2. If connection fails, tries Secondary Server
3. Remembers last successful server
4. Retries primary on next command

### Server Heartbeat

Servers ping each other every 30 seconds:
- Updates peer status
- Logs connectivity
- Displayed in client status

## 📄 License

This is proprietary software created for internal use. Not licensed for redistribution.

## 👨‍💻 Author

Created for automotive parts retail business with multiple location support.

## 📞 Support

For issues:
1. Check logs in `C:\CashApp\Server\logs\`
2. Review configuration files
3. Test network connectivity
4. Verify COM port configuration
5. Consult INSTALLATION_GUIDE.md

---

**Version 3.0** - Complete rewrite with client/server architecture  
**Last Updated**: January 2025
