# Frequently Asked Questions (FAQ)

## Platform Compatibility

### Q: Does the client work on macOS?
**A: Yes!** The client (CashClient.py) is fully compatible with macOS (and Linux).

- ✅ **Client**: Works on Windows, macOS, and Linux
- ❌ **Server**: Requires Windows (or Linux) for COM port hardware control

**Why?**
- Client is pure Python with tkinter (cross-platform)
- Server needs serial port access (Windows COM ports)

**Setup on macOS:**
```bash
# Install Python via Homebrew
brew install python@3.11

# Run client
cd ~/CashApp/Client
python3 CashClient.py
```

See `MACOS_COMPATIBILITY.md` for complete macOS guide.

---

## Hardware Timing

### Q: How long is the relay pin set high?
**A: 0.5 seconds (500 milliseconds) by default - but it's configurable!**

**Configuration:**
1. Open `server_config.ini`
2. Find `RelayDuration` setting
3. Change value (in seconds):
   ```ini
   [Server]
   RelayDuration = 0.5  # Default
   ```

**Or use the GUI:**
1. Run `ServerConfig.py` (or `ConfigureServer.bat`)
2. Go to Hardware tab
3. Adjust "Duration (seconds)" field
4. Test with "Test Relay" button

**Typical Values:**
- **0.3 seconds**: Fast drawers
- **0.5 seconds**: Standard (default)
- **1.0 seconds**: Older/slower drawers

**Technical Details:**
```python
# In CashServer.py open_drawer() method:
relay_duration = float(self.config.get('Server', 'RelayDuration', fallback='0.5'))

if relay_pin == 'DTR':
    self.serial_port.dtr = True
    time.sleep(relay_duration)  # Configurable delay
    self.serial_port.dtr = False
```

---

## Server Configuration

### Q: Can the server be configured with a GUI?
**A: Yes! We've included a graphical configuration tool.**

**ServerConfig.py** provides:
- ✅ Easy COM port selection (with detection)
- ✅ Relay settings (pin type, duration)
- ✅ Server identity and network settings
- ✅ Logging configuration
- ✅ Security settings
- ✅ **Test relay button** (open drawer to verify)
- ✅ Test COM port button
- ✅ All settings in tabbed interface

**How to Use:**

**Windows:**
```bash
cd C:\CashApp\Server
python ServerConfig.py
# Or double-click: ConfigureServer.bat
```

**Features:**
1. **Server Settings Tab**
   - Server ID
   - Port number
   - Peer server configuration
   - Shows local IP address

2. **Hardware Tab**
   - COM port dropdown (auto-detects ports)
   - Relay pin selection (DTR/RTS)
   - Relay duration configuration
   - "Test Relay" button - actually opens drawer!
   - "Refresh Ports" button

3. **Logging Tab**
   - Network log path
   - Local log path (fallback)
   - Email alert configuration

4. **Security Tab**
   - Max failed login attempts
   - Account lockout duration
   - Session timeout

**All changes saved to `server_config.ini`**

**No more manual INI editing!** Though you still can if you prefer.

---

## Quick Answers

### Q: What's the default admin password?
**A:** Username: `admin`, Password: `admin123`
**CHANGE THIS IMMEDIATELY!**

Use `UserManager.py` to change passwords.

### Q: Can I run multiple clients on one computer?
**A:** Yes, but usually not necessary. Each user logs in with their own credentials.

### Q: Do clients need to be on the same network as servers?
**A:** Yes, for auto-discovery. Manual configuration works across networks if routable.

### Q: Can I use this over the internet?
**A:** Not recommended. This is designed for local networks. VPN is possible but not officially supported.

### Q: How many clients can connect to one server?
**A:** Unlimited (within reason). Each connection is handled in a separate thread.

### Q: Can I use USB-to-Serial adapters on macOS for the server?
**A:** Technically yes (use `/dev/tty.usbserial-XXXXX`), but not officially supported. Windows is recommended for servers.

### Q: What happens if both servers fail?
**A:** Client will show connection error. Cash drawers can be opened manually with key/button.

### Q: Can I customize the relay duration per transaction?
**A:** Currently it's a global setting. Would require code modification for per-transaction timing.

### Q: Does it work with all cash drawers?
**A:** Works with drawers that open via relay (12V/24V trigger). Standard APG, Star, MMF, etc.

### Q: Can I test the relay without a drawer?
**A:** Yes! Use ServerConfig.py "Test Relay" button. Check with multimeter or LED.

### Q: What if I don't know which relay pin (DTR/RTS)?
**A:** Try DTR first (most common). If doesn't work, try RTS. Or check your relay documentation.

### Q: Can clients see which server they're connected to?
**A:** Yes, server info shown in top-right of client GUI.

### Q: How do I add a user?
**A:** Run `UserManager.py` on the server, select option 2.

### Q: Can users change their own passwords?
**A:** Not in current version. Admin must use UserManager.py.

### Q: What Python version is required?
**A:** Python 3.8 or higher. Python 3.11 recommended.

### Q: Does it require internet access?
**A:** No, works on isolated local networks.

### Q: Can I run it on Linux?
**A:** Client: Yes. Server: Probably (use `/dev/ttyUSB0` etc), but untested.

### Q: Where are the logs stored?
**A:** 
- Primary: `\\partsrv2\Parts\Cash` (configurable)
- Fallback: `C:\CashApp\Server\logs`

### Q: How do I backup the configuration?
**A:**
- Copy `server_config.ini`
- Copy `client_config.ini`
- Copy `users.json`

### Q: Can I move servers to different IPs?
**A:** Yes! Clients can rediscover servers with "🔍 Discover Servers" button.

### Q: What firewall ports need to be open?
**A:**
- TCP 5000: Main server communication
- UDP 5001: Server discovery

### Q: How secure is it?
**A:** 
- Passwords hashed with SHA-256
- Account lockout after 3 failed attempts
- Session tokens for authentication
- All activity logged
- Not designed for internet exposure (local network only)

---

## Configuration Examples

### Example 1: Fast Drawer (0.3s)
```ini
[Server]
RelayDuration = 0.3
```

### Example 2: Slow/Heavy Drawer (1.0s)
```ini
[Server]
RelayDuration = 1.0
```

### Example 3: RTS Pin Instead of DTR
```ini
[Server]
RelayPin = RTS
RelayDuration = 0.5
```

### Example 4: Different COM Port
```ini
[Server]
COMPort = COM5
```

---

## Troubleshooting

### Drawer Opens Too Slowly
**Solution:** Increase `RelayDuration` to 0.7 or 1.0 seconds

### Drawer Opens Inconsistently
**Solution:** 
- Check relay connections
- Verify power supply to relay
- Try increasing duration slightly

### Can't Find COM Port in Config GUI
**Solutions:**
1. Click "Refresh Ports" button
2. Check Device Manager (Windows)
3. Install USB-Serial drivers
4. Reboot after driver installation

### Test Relay Doesn't Open Drawer
**Check:**
1. Relay is powered
2. Wiring is correct
3. Drawer is connected to relay
4. Drawer solenoid is working
5. Try the other pin (DTR ↔ RTS)
6. Try increasing duration

---

## Platform Support Matrix

| Feature | Windows | macOS | Linux |
|---------|---------|-------|-------|
| Client | ✅ Yes | ✅ Yes | ✅ Yes |
| Server | ✅ Yes | ⚠️ Maybe* | ⚠️ Maybe* |
| Config GUI | ✅ Yes | ✅ Yes | ✅ Yes |
| User Manager | ✅ Yes | ✅ Yes | ✅ Yes |
| Auto-Discovery | ✅ Yes | ✅ Yes | ✅ Yes |
| COM Ports | ✅ Yes | ⚠️ USB Serial | ⚠️ /dev/ttyUSB |

\* = Not officially supported or tested, but may work

---

## More Information

For detailed information, see:
- **README.md** - Complete documentation
- **QUICK_START.md** - Fast setup guide  
- **INSTALLATION_GUIDE.md** - Detailed installation
- **MACOS_COMPATIBILITY.md** - macOS-specific guide
- **SERVER_DISCOVERY.md** - Network discovery
- **PENNY_ROUNDING.md** - Canadian rounding rules
- **CHANGELOG.md** - Version history

---

**Last Updated**: January 19, 2025  
**Version**: 3.0
