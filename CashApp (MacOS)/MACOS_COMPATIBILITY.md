# macOS Compatibility Guide

## Overview

The **Cash Drawer Client** (CashClient.py) is fully compatible with macOS! The client application uses tkinter which is included with Python on macOS, and all networking code is cross-platform.

The **Cash Drawer Server** (CashServer.py) requires Windows (or Linux) since it controls hardware via COM ports, which are primarily used on Windows systems.

## System Requirements

### macOS Client

**Minimum:**
- macOS 10.13 (High Sierra) or later
- Python 3.8 or higher
- Network connectivity

**Recommended:**
- macOS 12 (Monterey) or later
- Python 3.11 or higher

## Installation on macOS

### Step 1: Install Python

Python 3 is included with macOS, but you may want a newer version:

**Check current version:**
```bash
python3 --version
```

**Install via Homebrew (recommended):**
```bash
# Install Homebrew if not already installed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install Python
brew install python@3.11
```

**Or download from python.org:**
- Visit https://www.python.org/downloads/macos/
- Download Python 3.11+ installer
- Run installer package

### Step 2: Setup Client

```bash
# Create directory
mkdir -p ~/CashApp/Client
cd ~/CashApp/Client

# Copy CashClient.py to this directory

# Make executable (optional)
chmod +x CashClient.py

# Run the client
python3 CashClient.py
```

### Step 3: Configure

The client will create `client_config.ini` on first run. Configuration is identical to Windows:

1. **Auto-Discovery**: Client will automatically search for servers
2. **Manual Config**: Enter server IPs in Settings tab
3. **Discover Button**: Click "🔍 Discover Servers" in Settings

### Step 4: Create Application Bundle (Optional)

For easier access, create a macOS app:

**Create launcher script:**
```bash
cat > ~/CashApp/Client/launch.sh << 'EOF'
#!/bin/bash
cd "$(dirname "$0")"
python3 CashClient.py
EOF

chmod +x ~/CashApp/Client/launch.sh
```

**Or use Automator:**
1. Open Automator
2. New Document → Application
3. Add "Run Shell Script" action
4. Enter: `cd ~/CashApp/Client && python3 CashClient.py`
5. Save as "Cash Drawer.app" to Applications folder
6. Optional: Add custom icon

## Platform Differences

### What Works the Same

✅ All GUI features
✅ Server discovery
✅ Authentication
✅ Transaction entry
✅ Canadian penny rounding
✅ Network communication
✅ Configuration files
✅ Auto-connect
✅ Settings management

### macOS-Specific Features

🍎 **Native Look**: Uses macOS native widgets
🍎 **Menu Bar**: Can add macOS menu bar integration
🍎 **Dock Icon**: Shows in macOS Dock
🍎 **Notifications**: Can use macOS notification center

### Known Differences

📝 **File Paths**: Uses Unix-style paths (`/Users/...` vs `C:\Users\...`)
📝 **Config Location**: `~/CashApp/Client/` vs `C:\CashApp\Client\`
📝 **Line Endings**: LF vs CRLF (handled automatically)
📝 **Case Sensitivity**: macOS filesystem can be case-sensitive

## Usage on macOS

### Running the Client

**Terminal:**
```bash
cd ~/CashApp/Client
python3 CashClient.py
```

**Dock (if created app bundle):**
- Click Cash Drawer.app in Applications

**Finder:**
- Navigate to ~/CashApp/Client/
- Double-click launch.sh

### Keyboard Shortcuts

macOS-specific shortcuts:
- **⌘ + Q**: Quit application
- **⌘ + W**: Close window (if implemented)
- **⌘ + ,**: Preferences (if implemented)

Standard shortcuts work the same:
- **Tab**: Navigate between fields
- **Enter**: Submit/login
- **Esc**: Cancel dialogs

## Networking on macOS

### Firewall Configuration

**Check firewall status:**
```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
```

**Allow Python (if firewall blocks):**
1. System Preferences → Security & Privacy → Firewall
2. Click lock to make changes
3. Firewall Options
4. Add Python (or CashClient.py)
5. Allow incoming connections

### Network Discovery

macOS supports UDP broadcast discovery:
- Works on WiFi and Ethernet
- Works on VPN (some configurations)
- May require permission on first use

**Test connectivity:**
```bash
# Ping server
ping 192.168.1.10

# Test port
nc -zv 192.168.1.10 5000

# Test UDP (requires netcat)
echo "test" | nc -u 192.168.1.10 5001
```

## Troubleshooting

### "Permission Denied" Errors

**Solution 1**: Make script executable
```bash
chmod +x CashClient.py
```

**Solution 2**: Run with python3 explicitly
```bash
python3 CashClient.py
```

### "tkinter not found"

Python from python.org includes tkinter. If using Homebrew:

```bash
brew install python-tk@3.11
```

### "Module not found" Errors

Install missing modules:
```bash
pip3 install --user -r requirements.txt
```

### Cannot Connect to Server

**Check network:**
```bash
# Ping server
ping [server-ip]

# Check if port is open
nc -zv [server-ip] 5000
```

**Check firewall**: System Preferences → Security & Privacy → Firewall

### "App Can't Be Opened" (Gatekeeper)

If created your own app bundle:

```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine ~/Applications/Cash\ Drawer.app
```

Or: Right-click app → Open → Open (first time only)

## Development on macOS

### Setting Up Development Environment

```bash
# Install Python
brew install python@3.11

# Create virtual environment (optional)
python3 -m venv ~/CashApp/venv
source ~/CashApp/venv/bin/activate

# Install dependencies
pip install pyserial  # Not needed for client, but won't hurt

# Run client
cd ~/CashApp/Client
python3 CashClient.py
```

### Running Tests

```bash
# Penny rounding test
cd ~/CashApp
python3 Test_Penny_Rounding.py

# Manual testing
python3 CashClient.py
```

## Advanced: Server on macOS (Not Recommended)

While the client works perfectly on macOS, the server requires hardware access. If you have USB-to-Serial adapters:

**Identify device:**
```bash
ls -l /dev/tty.*
# Look for /dev/tty.usbserial-XXXXX
```

**Update server config:**
```ini
[Server]
COMPort = /dev/tty.usbserial-XXXXX
```

**Note**: This is not officially supported. Use Windows/Linux for servers.

## Performance

### Expected Performance

- **Startup**: ~1-2 seconds
- **Server Discovery**: 2-3 seconds
- **Login**: Instant
- **Transaction**: Instant
- **Network Latency**: <100ms on local network

### Optimization Tips

1. **Enable auto-connect**: Skip manual connection
2. **Remember username**: Faster login
3. **Use Ethernet**: Better than WiFi for reliability
4. **Static IPs**: Avoid discovery delay

## Multiple Locations

If you have multiple stores with servers:

**Option 1: Separate configs**
```bash
~/CashApp/Store1/client_config.ini
~/CashApp/Store2/client_config.ini
~/CashApp/Store3/client_config.ini
```

**Option 2: Discovery**
- Let client auto-discover servers
- Different servers for each location
- No configuration needed

## Dock Integration (Optional)

Add dock features by enhancing CashClient.py:

```python
# Add to CashClient class
def setup_dock_icon(self):
    if platform.system() == 'Darwin':
        try:
            from AppKit import NSApp, NSImage
            # Set custom dock icon
            icon_path = 'icon.png'
            if os.path.exists(icon_path):
                image = NSImage.alloc().initWithContentsOfFile_(icon_path)
                NSApp.setApplicationIconImage_(image)
        except:
            pass
```

## Menu Bar Integration (Optional)

Add native macOS menu:

```python
# Add menubar (requires PyObjC)
def create_menu(self):
    if platform.system() == 'Darwin':
        menubar = tk.Menu(self.root)
        self.root.config(menu=menubar)
        
        # App menu
        app_menu = tk.Menu(menubar, name='apple')
        menubar.add_cascade(menu=app_menu)
        app_menu.add_command(label='About Cash Drawer')
        app_menu.add_separator()
        app_menu.add_command(label='Preferences...', accelerator='Cmd+,')
```

## Support

### macOS-Specific Issues

For macOS-related issues:
1. Check Python version: `python3 --version`
2. Check tkinter: `python3 -m tkinter`
3. Check network: `ping [server-ip]`
4. Check firewall settings
5. Review console logs: `/var/log/system.log`

### General Support

See main documentation:
- `README.md` - Complete reference
- `QUICK_START.md` - Fast setup
- `INSTALLATION_GUIDE.md` - Detailed guide

## Summary

### ✅ What Works

- **Client Application**: 100% compatible
- **All Features**: Discovery, auth, transactions, rounding
- **Network**: Same as Windows
- **Configuration**: Same as Windows

### ❌ What Doesn't Work

- **Server**: Requires Windows/Linux for COM port access
- **Batch Files**: Use shell scripts instead

### 📝 Recommendations

- ✅ Use macOS for **client stations**
- ❌ Use Windows for **servers** (hardware control)
- ✅ Mix platforms on same network
- ✅ Auto-discovery works across platforms

---

**Compatibility**: ✅ Full client support  
**Tested**: macOS 12+ with Python 3.11  
**Status**: Production ready  
**Platform**: Cross-platform client, Windows servers
