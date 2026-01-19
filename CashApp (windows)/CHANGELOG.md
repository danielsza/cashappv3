# Changelog

All notable changes to the Cash Drawer Control System are documented in this file.

## [3.0.0] - 2025-01-19

### 🎉 Major Release - Complete Rewrite

This is a complete rewrite of the cash drawer application, replacing the previous Visual Basic application (Version 2.0) with a modern Python-based client/server system.

### ✨ Added

**Core Architecture**
- Client/server architecture with centralized hardware control
- Dual server support with automatic failover
- Server health monitoring and peer status tracking
- Threaded connection handling for multiple simultaneous clients

**Automatic Server Discovery**
- UDP broadcast discovery on local networks
- Smart IP scanning for common server addresses
- Automatic configuration on first run
- Manual discovery button in Settings tab
- Works across standard network configurations

**User Authentication & Security**
- Password-based authentication with SHA-256 hashing
- User management utility (add/remove/modify users)
- Account lockout after 3 failed login attempts (5-minute lockout)
- Session token management
- Failed login attempt logging
- User permission levels (admin/user)

**Canadian Penny Rounding**
- Automatic penny rounding for cash transactions
- Follows Canadian fair rounding rules:
  - .01/.02 → .00
  - .03/.04 → .05
  - .06/.07 → .05
  - .08/.09 → .10
- Visual indicator in GUI
- Test utility included
- Comprehensive documentation

**Modern GUI**
- Windows tkinter-based interface
- Tabbed design (Login, Control, Settings)
- Auto-calculate change with real-time rounding
- Quick action buttons (BOD/EOD)
- Document type selection with checkboxes
- Connection status indicators
- Remember username option
- Auto-connect on startup option

**Configuration Management**
- INI-based configuration files (no more hardcoded values!)
- Configurable COM port (was hardcoded to COM10)
- Configurable relay pin (DTR or RTS)
- Configurable server addresses
- Configurable security settings
- Easy-to-edit text files

**Comprehensive Logging**
- Network share logging with local fallback
- Transaction logs with full audit trail
- Server event logs
- Failed authentication logs
- Monthly transaction logs
- Daily server logs
- Automatic log rotation

**Documentation**
- Complete README with all features
- Quick Start guide (5-minute setup)
- Detailed Installation Guide
- Deployment Checklist with sign-offs
- Penny Rounding documentation
- Server Discovery documentation
- Build Summary overview

**Testing & Utilities**
- Penny rounding test script
- User management utility
- Connection testing in GUI
- Server discovery testing
- Windows batch files for easy startup

### 🔄 Changed

**From Version 2.0**
- Architecture: Standalone → Client/Server
- Authentication: Hardcoded codes → Password-based
- Configuration: Hardcoded → INI files
- COM Port: Fixed COM10 → Configurable
- User Management: Recompile → Runtime utility
- Language: Visual Basic → Python 3.8+
- Logging: Network only → Network + Local fallback
- Server Setup: Single machine → Dual servers with failover
- Discovery: Manual IPs → Automatic discovery

### 🛠️ Technical Details

**Server Features**
- TCP socket server on port 5000
- UDP discovery listener on port 5001
- Serial port control (pyserial)
- DTR/RTS relay triggering
- JSON-based communication protocol
- Peer heartbeat (30-second interval)
- Graceful shutdown handling

**Client Features**
- Automatic server discovery
- Primary/secondary server failover
- Session management
- Real-time change calculation
- Canadian penny rounding
- Configuration persistence
- Network diagnostics

**Dependencies**
- Python 3.8+
- pyserial (for COM port control)

### 📦 Files Included

**Applications**
- `CashServer.py` - Server application (~400 lines)
- `CashClient.py` - Client GUI (~650 lines)
- `UserManager.py` - User management utility (~200 lines)

**Documentation**
- `README.md` - Main documentation
- `QUICK_START.md` - 5-minute setup guide
- `INSTALLATION_GUIDE.md` - Detailed installation
- `DEPLOYMENT_CHECKLIST.md` - Deployment helper
- `PENNY_ROUNDING.md` - Rounding documentation
- `SERVER_DISCOVERY.md` - Discovery documentation
- `BUILD_SUMMARY.md` - Overview of the system
- `CHANGELOG.md` - This file

**Support Files**
- `requirements.txt` - Python dependencies
- `StartServer.bat` - Server startup script
- `StartClient.bat` - Client startup script
- `Test_Penny_Rounding.py` - Penny rounding test utility

### 🔒 Security Improvements

- Passwords hashed with SHA-256 (vs. plaintext codes)
- Account lockout protection
- Session token authentication
- Failed attempt logging
- Configurable security settings
- User permission levels

### 📊 Statistics

- Total Lines of Code: ~1,200
- Documentation Pages: ~2,000 lines
- Test Coverage: Penny rounding fully tested
- Supported Platforms: Windows 10/11
- Python Version: 3.8+

### 🎓 Migration from Version 2.0

Since the Version 2.0 source code was lost, this is a complete rewrite based on requirements:

**What Stayed the Same**
- Cash drawer hardware (relay-controlled via COM port)
- Network logging location (\\partsrv2\Parts\Cash)
- Basic workflow (login, enter transaction, open drawer)

**What Changed**
- Everything else! Complete modern rewrite

**Migration Steps**
1. Document existing Version 2.0 users and their access codes
2. Install Version 3.0 on server computers
3. Create user accounts matching old system
4. Install clients and configure/discover servers
5. Test thoroughly in parallel with old system
6. Switch over when confident
7. Decommission Version 2.0

### ⚠️ Breaking Changes from Version 2.0

- **Authentication**: User codes no longer work; users need new passwords
- **Configuration**: Old hardcoded settings must be entered in INI files
- **COM Port**: May need to reconfigure if not COM10
- **Network**: Requires both TCP 5000 and UDP 5001 (vs. just TCP in v2.0)
- **Compatibility**: Version 3.0 clients cannot connect to Version 2.0 servers

### 🐛 Known Issues

- None at release

### 🔮 Planned Features (Future Versions)

- Email alerts for security events
- Web-based admin interface
- Database backend (vs. JSON files)
- SSL/TLS encryption
- Multi-level permissions
- Transaction reports/analytics
- Receipt printer integration
- Barcode scanner support
- Mobile client app
- Cloud logging/backup

---

## [2.0.0] - Date Unknown (Lost Source Code)

### Visual Basic Application

**Features**
- Single standalone application
- Hardcoded user codes for authentication
- COM10 relay control (hardcoded)
- Network share logging
- Email alerts on errors
- Text file transaction logs

**Known Limitations**
- Single point of failure
- Required recompilation for any changes
- Hardcoded COM port
- No failover support
- Source code no longer available

---

## Versioning

This project uses [Semantic Versioning](https://semver.org/):
- **Major version** (3.x.x): Incompatible API/architecture changes
- **Minor version** (x.1.x): New features, backward compatible
- **Patch version** (x.x.1): Bug fixes, backward compatible

---

## Support

For questions about Version 3.0:
- Check documentation in `/CashApp/` folder
- Review `INSTALLATION_GUIDE.md` for setup help
- See `QUICK_START.md` for common tasks
- Consult `README.md` for technical details

For issues:
- Check logs in `C:\CashApp\Server\logs\`
- Review configuration files
- Test network connectivity
- Verify COM port configuration

---

**Current Version**: 3.0.0  
**Release Date**: January 19, 2025  
**Status**: ✅ Production Ready  
**License**: Proprietary - Internal Use Only
