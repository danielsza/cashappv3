# Cash Drawer System - Installation Guide

## System Overview

This is a complete rewrite of your cash drawer application with a modern client/server architecture:

- **Server Application**: Controls hardware (COM port relay), handles authentication, logging
- **Client Application**: Windows GUI for users to interact with the system
- **Dual Server Support**: Automatic failover between primary and secondary servers
- **User Management**: Secure password-based authentication with account lockout
- **Comprehensive Logging**: All transactions logged to network share and local storage

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Client    │────▶│   Server 1  │◀───▶│   Server 2  │
│   (GUI)     │     │  (Primary)  │     │ (Secondary) │
└─────────────┘     └──────┬──────┘     └──────┬──────┘
                           │                    │
                           ▼                    ▼
                      ┌─────────┐          ┌─────────┐
                      │ COM10   │          │ COM10   │
                      │ Relay   │          │ Relay   │
                      └─────────┘          └─────────┘
```

## Requirements

### Server Requirements
- Windows 11 (or Windows 10)
- Python 3.8 or higher
- USB-to-Serial adapter configured as COM10 (or your choice)
- Network connectivity
- Access to network share: `\\partsrv2\Parts\Cash`

### Client Requirements
- Windows 11 (or Windows 10)
- Python 3.8 or higher
- Network connectivity to server(s)

## Installation Instructions

### Step 1: Install Python

1. Download Python 3.11+ from https://www.python.org/downloads/
2. Run installer and **CHECK** "Add Python to PATH"
3. Complete installation

### Step 2: Install Required Packages

Open Command Prompt or PowerShell and run:

```bash
pip install pyserial
```

That's it! No other dependencies needed.

### Step 3: Setup Server (Both Server Machines)

1. **Create server directory**:
   ```
   C:\CashApp\Server\
   ```

2. **Copy server files**:
   - `CashServer.py`
   - `UserManager.py`

3. **Configure COM Port**:
   
   a. Connect USB-to-Serial adapter
   
   b. Open Device Manager (Win + X, then M)
   
   c. Expand "Ports (COM & LPT)"
   
   d. Find your USB Serial device
   
   e. Right-click → Properties → Port Settings → Advanced
   
   f. Change "COM Port Number" to COM10 (or your preferred port)
   
   g. Click OK and **REBOOT** the computer
   
   h. Verify COM10 appears in Device Manager after reboot

4. **First Run - Generate Configuration**:
   ```bash
   cd C:\CashApp\Server
   python CashServer.py
   ```
   
   This will create `server_config.ini` and exit.

5. **Edit Configuration**:
   
   Open `server_config.ini` and configure:
   
   ```ini
   [Server]
   ServerID = SERVER1              # Use SERVER1 for primary, SERVER2 for secondary
   Port = 5000
   PeerServerHost = 192.168.1.X    # IP of OTHER server (leave empty for standalone)
   PeerServerPort = 5000
   COMPort = COM10
   RelayPin = DTR                  # or RTS depending on your relay
   LogPath = \\partsrv2\Parts\Cash
   LocalLogPath = C:\CashApp\Server\logs
   EnableEmailAlerts = false       # Set to true if you configure SMTP
   
   [Security]
   MaxFailedAttempts = 3
   LockoutDuration = 300
   SessionTimeout = 3600
   ```

6. **Create Users**:
   ```bash
   python UserManager.py
   ```
   
   - Select option 2 to add users
   - Default admin user (username: `admin`, password: `admin123`) is created on first run
   - **IMPORTANT**: Change the admin password immediately!

7. **Test Server**:
   ```bash
   python CashServer.py
   ```
   
   You should see:
   ```
   Server SERVER1 is running on port 5000
   Press Ctrl+C to stop
   ```

### Step 4: Setup Client (All Client Machines)

1. **Create client directory**:
   ```
   C:\CashApp\Client\
   ```

2. **Copy client file**:
   - `CashClient.py`

3. **First Run**:
   ```bash
   cd C:\CashApp\Client
   python CashClient.py
   ```
   
   This will create `client_config.ini` and open the GUI.

4. **Configure Settings**:

   **Option A: Auto-Discovery (Recommended)**
   - If no servers are configured, client automatically searches network
   - Review discovered servers
   - Click "Yes" to use them
   - Settings are saved automatically
   
   **Option B: Manual Discovery**
   - Go to Settings tab
   - Click "🔍 Discover Servers" button
   - Review found servers
   - Click "Yes" to populate settings
   - Click "Save Settings"
   
   **Option C: Manual Configuration**
   - Go to Settings tab
   - Primary Server: IP address of Server 1 (e.g., `192.168.1.10`)
   - Primary Port: `5000`
   - Secondary Server: IP address of Server 2 (e.g., `192.168.1.11`)
   - Secondary Port: `5000`
   - Check "Auto-connect on startup" for convenience
   - Click "Save Settings"
   
5. **Test Connection**:
   - Click "Test Connection" button to verify
   - Should show "✓ Connected" for both servers

### Step 5: Create Desktop Shortcuts

**For Server** (optional, if you want to run it manually):
1. Right-click Desktop → New → Shortcut
2. Location: `python C:\CashApp\Server\CashServer.py`
3. Name: `Cash Server`

**For Client**:
1. Right-click Desktop → New → Shortcut
2. Location: `python C:\CashApp\Client\CashClient.py`
3. Name: `Cash Drawer`

### Step 6: Setup Auto-Start (Server Only)

To make the server start automatically:

1. Press Win + R, type `shell:startup`, press Enter
2. Create a shortcut to `CashServer.py` in the Startup folder
3. Or use Task Scheduler for running as a service

## Usage

### Client Application

1. **Login Tab**:
   - Enter username and password
   - Click "Login"

2. **Control Tab** (after login):
   - Select document type(s): Invoice, Petty Cash, Change, Refund, BOD, EOD
   - Enter Total amount
   - Enter IN amount (cash received)
   - OUT will auto-calculate the change
   - Click "Open" to open the drawer
   - Use "BOD" or "EOD" buttons for quick Beginning/End of Day operations

3. **Settings Tab**:
   - Configure server connections
   - Test connections
   - Enable auto-connect

### User Management

Run `UserManager.py` on the server:

```bash
python UserManager.py
```

Options:
1. **List Users**: View all users and their status
2. **Add User**: Create new user account
3. **Change Password**: Update user password
4. **Delete User**: Remove user account
5. **Unlock User**: Unlock locked account (after too many failed attempts)

### Logs

Logs are written to:
- Network: `\\partsrv2\Parts\Cash\`
- Local fallback: `C:\CashApp\Server\logs\`

Log files:
- `CashServer_SERVERID_YYYYMMDD.log` - Server events
- `Transactions_YYYYMM.log` - Transaction log

Transaction log format:
```
2025-01-19T10:30:45 | SERVER1 | daniel | Transaction | Invoice | Total: 45.50 | IN: 50.00 | OUT: 4.50
```

## Troubleshooting

### COM Port Issues

**Problem**: Serial port not found
- Check Device Manager for COM port
- Install drivers via Windows Update
- Verify COM port number matches config
- **REBOOT** after changing COM port number

**Problem**: Permission denied on COM port
- Close other programs using the port
- Run server as Administrator
- Check if port is in use: `mode COM10` in Command Prompt

### Network Issues

**Problem**: Client can't connect to server
- Verify server is running
- Check firewall settings (allow port 5000 TCP and 5001 UDP)
- Ping server IP address
- Use "Test Connection" in client settings
- Try "🔍 Discover Servers" button to auto-find servers

**Problem**: Auto-discovery doesn't find servers
- Verify servers are running (check for "Discovery listener started" message)
- Check same network subnet (client and servers on same network)
- Check firewall allows UDP port 5001
- Use manual configuration as alternative

**Problem**: Can't write to network share
- Verify network path exists
- Check permissions on `\\partsrv2\Parts\Cash`
- Server will fall back to local logs automatically

### Authentication Issues

**Problem**: Account locked
- Run `UserManager.py` on server
- Select option 5 to unlock user

**Problem**: Forgot password
- Run `UserManager.py` on server
- Select option 3 to change password

## Security Notes

1. **Change Default Password**: The default admin password (`admin123`) should be changed immediately
2. **User Lockout**: Accounts lock after 3 failed attempts for 5 minutes
3. **Network Security**: Consider using VPN or firewall rules to restrict access to port 5000
4. **Password Requirements**: Minimum 6 characters (can be increased in code)

## Features

### Implemented Features
✅ Client/Server architecture
✅ Dual server support with automatic failover
✅ User authentication with password hashing
✅ Account lockout after failed attempts
✅ Comprehensive logging (network + local)
✅ Auto-calculate change
✅ Quick BOD/EOD buttons
✅ Transaction tracking
✅ Configurable COM port and relay pin
✅ Server heartbeat/peer status monitoring
✅ Remember username option
✅ Auto-connect option
✅ Connection testing

### Future Enhancements (Optional)
- Email alerts for security events
- Web-based admin interface
- Database instead of JSON files
- SSL/TLS encryption
- Multi-level permissions
- Transaction reports/analytics
- Receipt printing integration

## Support

For issues or questions:
1. Check logs in `C:\CashApp\Server\logs\`
2. Verify configuration files
3. Test network connectivity
4. Check COM port in Device Manager

## File Structure

```
C:\CashApp\
├── Server\
│   ├── CashServer.py           # Main server application
│   ├── UserManager.py          # User management utility
│   ├── server_config.ini       # Server configuration
│   ├── users.json              # User database
│   └── logs\                   # Local log storage
│
└── Client\
    ├── CashClient.py           # Client GUI application
    └── client_config.ini       # Client configuration
```

## Version History

**Version 3.0** (Current)
- Complete rewrite with client/server architecture
- Added user authentication
- Dual server support with automatic failover
- Automatic server discovery on local network
- Canadian penny rounding support
- Improved logging
- Modern GUI interface
- Configurable settings

**Version 2.0** (Previous - VB application)
- Original Visual Basic application
- Hardcoded user codes
- Single machine setup
- Source code lost
