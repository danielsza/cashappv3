# CashDrawer Management System

A multi-location cash drawer management system for retail environments. Tracks cash transactions, safe drops, petty cash, and provides end-of-day reconciliation across multiple servers.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green.svg)

## Features

### Core Functionality
- **Cash Drawer Control** - Open drawer via USB relay (CH340 serial)
- **Transaction Logging** - Track invoices, refunds, petty cash with full audit trail
- **Safe Drops** - Record cash deposits to safe with confirmation workflow
- **BOD/EOD** - Beginning and End of Day float counting and reconciliation
- **Receipt Printing** - Automatic transaction receipts via thermal printer

### Multi-Server Sync
- **Auto-Discovery** - Servers automatically find each other on the network
- **Two-Way Sync** - Users, transactions, safe drops, and settings sync between locations
- **Conflict Resolution** - Last-modified-wins for user updates
- **Offline Capable** - Each server operates independently, syncs when connected

### Security
- **User Authentication** - Password-protected access with BCrypt hashing
- **Role-Based Access** - Admin and User permission levels
- **Account Lockout** - Automatic lockout after failed login attempts
- **Audit Logging** - All transactions logged with timestamps and user info

### Administration
- **Remote Management** - Configure servers remotely via admin panel
- **User Management** - Add, edit, delete users across all locations
- **Real-Time Notifications** - Push alerts to all connected clients
- **Log Viewer** - View transaction and error logs remotely

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Location A    │     │   Location B    │     │   Location C    │
│                 │     │                 │     │                 │
│  ┌───────────┐  │     │  ┌───────────┐  │     │  ┌───────────┐  │
│  │  Client   │  │     │  │  Client   │  │     │  │  Client   │  │
│  └─────┬─────┘  │     │  └─────┬─────┘  │     │  └─────┬─────┘  │
│        │        │     │        │        │     │        │        │
│  ┌─────┴─────┐  │     │  ┌─────┴─────┐  │     │  ┌─────┴─────┐  │
│  │  Server   │◄─┼─────┼─►│  Server   │◄─┼─────┼─►│  Server   │  │
│  └─────┬─────┘  │     │  └─────┬─────┘  │     │  └─────┬─────┘  │
│        │        │     │        │        │     │        │        │
│  ┌─────┴─────┐  │     │  ┌─────┴─────┐  │     │  ┌─────┴─────┐  │
│  │USB Relay  │  │     │  │USB Relay  │  │     │  │USB Relay  │  │
│  └───────────┘  │     │  └───────────┘  │     │  └───────────┘  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        ▲                       ▲                       ▲
        └───────────────────────┴───────────────────────┘
                    Automatic Peer Sync (30s)
```

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- USB Relay Module (CH340-based, e.g., LCUS-1)
- Thermal Receipt Printer (optional, ESC/POS compatible)

## Installation

### Quick Install (MSI)
1. Download the latest installer from [Releases](../../releases)
2. Run `CashDrawerInstaller.msi`
3. Follow the installation wizard
4. The server service starts automatically

### Manual Install
```powershell
# Clone the repository
git clone https://github.com/yourusername/CashDrawerCS.git
cd CashDrawerCS

# Build
dotnet build -c Release

# Install server as Windows service
sc create CashDrawerServer binPath= "C:\Path\To\CashDrawer.Server.exe"
sc start CashDrawerServer

# Run client
.\CashDrawer.Client\bin\Release\net8.0-windows\CashDrawer.Client.exe
```

## Configuration

### Server Configuration
Location: `C:\ProgramData\CashDrawer\config.json`

```json
{
  "ServerID": "STORE1",
  "TcpPort": 5000,
  "UdpPort": 5001,
  "ControlPort": 5003,
  "SerialPort": "COM3",
  "BaudRate": 9600,
  "LocalLogPath": "C:\\ProgramData\\CashDrawer\\Logs",
  "LogPath": "\\\\server\\share\\CashDrawerLogs",
  "SafeDropThreshold": 300.00
}
```

### Client Configuration
Location: `%APPDATA%\CashDrawer\client_config.json`

```json
{
  "ServerHost": "localhost",
  "ServerPort": 5000,
  "PrinterName": "EPSON TM-T88V",
  "AutoPrint": true
}
```

## Usage

### Daily Workflow

1. **Start of Day (BOD)**
   - Count the cash drawer float
   - Enter the amount in the BOD dialog
   - System records starting balance

2. **During the Day**
   - Process transactions (Invoice, Refund)
   - Record petty cash withdrawals
   - Make safe drops when drawer exceeds threshold

3. **End of Day (EOD)**
   - Count all cash in drawer
   - System calculates expected vs actual
   - Review any discrepancies
   - Print EOD report

### Transaction Types

| Type | Description | Cash Flow |
|------|-------------|-----------|
| Invoice | Customer payment | IN (+) |
| Refund | Return to customer | OUT (-) |
| Petty Cash | Business expense | OUT (-) |
| Safe Drop | Deposit to safe | Tracked separately |
| BOD | Opening float | Starting balance |
| EOD | Closing count | Verification |

## Network Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 5000 | TCP | Main server communication |
| 5001 | UDP | Server discovery broadcast |
| 5003 | TCP | Control service (start/stop/status) |

## Data Storage

All data is stored in `C:\ProgramData\CashDrawer\`:

```
CashDrawer/
├── config.json          # Server configuration
├── users.json           # User accounts (BCrypt hashed passwords)
├── bod_float.json       # Daily starting floats
├── safe_drops.json      # Safe drop records
├── pettycash.json       # Petty cash recipients/reasons
└── Logs/
    ├── CashDrawer_2026-01-30.log    # Transaction logs
    ├── Server_20260130.log          # Server logs
    └── Errors_20260130.log          # Error logs
```

## Sync Details

### What Syncs Between Servers

| Data | Sync Method | Conflict Resolution |
|------|-------------|---------------------|
| Users | Two-way | LastModified wins |
| Transactions | By unique ID | Deduplicated |
| Safe Drops | By unique ID | Deduplicated |
| BOD Float | One-way | First server sets it |
| Petty Cash Config | Two-way | LastModified wins |

### Sync Timing
- **Discovery**: Every 2 minutes via UDP broadcast
- **Data Sync**: Every 30 seconds via TCP

## Troubleshooting

### Drawer Won't Open
1. Check USB relay connection (Device Manager → Ports)
2. Verify COM port in config matches actual port
3. Test relay: `mode COM3` then check server logs

### Sync Not Working
1. Check firewall allows ports 5000, 5001, 5003
2. Verify servers are on same subnet
3. Check server logs for discovery messages

### "Invalid Salt Version" on Login
Password was stored incorrectly. Reset the user's password or delete `users.json` and recreate users.

### Service Won't Start
```powershell
# Check service status
Get-Service CashDrawerServer

# View recent logs
Get-Content "C:\ProgramData\CashDrawer\Logs\Server_$(Get-Date -Format 'yyyyMMdd').log" -Tail 50

# Restart service
Restart-Service CashDrawerServer
```

## Building from Source

### Prerequisites
- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- WiX Toolset v4 (for installer)

### Build Commands
```powershell
# Restore packages
dotnet restore

# Build all projects
dotnet build -c Release

# Run tests
dotnet test

# Create installer (requires WiX)
dotnet build Installer/Installer.wixproj -c Release
```

## Project Structure

```
CashDrawerCS/
├── CashDrawer.Client/       # WinForms client application
│   ├── Forms/               # UI forms (Main, BOD, EOD, Admin)
│   └── Services/            # Client-side services
├── CashDrawer.Server/       # Windows service
│   └── Services/            # TCP server, serial port, sync
├── CashDrawer.Shared/       # Shared models and utilities
│   └── Models/              # User, Transaction, Config, etc.
└── Installer/               # WiX installer project
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) - Password hashing
- [Serilog](https://serilog.net/) - Structured logging
- USB Relay control based on CH340 serial protocol
