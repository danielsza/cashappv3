# GM Parts Receiving

Barcode scanning and shipment processing for GM dealerships. Replaces the Excel macro + paper form workflow with a web app that runs on your workstation and scanner simultaneously.

**Two modes:**
- **Scanner Mode** - Runs on the Datalogic's built-in browser. Scan, flag DIPP, wrong dealer alerts.
- **Workstation Mode** - Full desktop app. PWB+ comparison, Woodstock forms, Outlook email.

---

## Windows Setup (Step by Step)

### 1. Install Node.js (one time)

Download from **https://nodejs.org** - click the big green **LTS** button.

Run the installer with all defaults. This gives you `node` and `npm` in Command Prompt.

To verify, open Command Prompt and type:
```
node --version
```
You should see something like `v20.x.x`.

### 2. Extract and Setup

1. Extract the `gm-parts-receiving.zip` to wherever you want (e.g. `C:\gm-parts-receiving`)
2. Double-click **SETUP.bat**

This installs dependencies and builds the app. Takes about 30 seconds.

### 3. Test It

Double-click **START.bat**

You will see:
```
  GM Parts Receiving - Server Running
  ============================================
  Workstation:  http://localhost:3000
  Scanner:      http://192.168.1.100:3000
  ============================================
```

Open **http://localhost:3000** in Chrome. That is the app running.

The **Scanner URL** is what you will use on the Datalogic (see below).

### 4. Install as Windows Service (auto-start on boot)

Once testing looks good, right-click **INSTALL-SERVICE.bat** and select **Run as administrator**

This creates a Windows Service called "GM Parts Receiving" that:
- Starts automatically when the PC boots
- Runs in the background (no command window needed)
- Restarts itself if it crashes

To manage it: press Win+R, type `services.msc`, and find "GM Parts Receiving"

To remove: right-click **UNINSTALL-SERVICE.bat** and Run as administrator

---

## Datalogic Scanner Setup (No RDP Needed)

The scanner connects directly to the app over WiFi using its own built-in browser. No RDP, no remote desktop. The scanner is a standalone device hitting the web app.

### Requirements

- Scanner and workstation on the **same WiFi network**
- Scanner has a **built-in web browser** (all modern Datalogic Android units do)

### Setup Steps

**1. Find the workstation IP**

On the workstation, open Command Prompt:
```
ipconfig
```
Find your IPv4 Address (e.g. `192.168.1.100`). Or just look at what START.bat prints.

**2. Open the browser on the scanner**

On the Datalogic (Memor, Skorpio, etc.):

1. Open **Chrome** or the built-in browser
2. Go to: `http://192.168.1.100:3000` (use your actual IP)
3. The app loads - tap **Scanner**
4. **Bookmark it** or set as the homepage

**3. Set as homepage (recommended)**

On Android-based Datalogic (Memor 10/11, Skorpio X5, etc.):
- Chrome > Settings > Homepage > set to `http://192.168.1.100:3000`

For kiosk/single-app mode, use Datalogic SureLock or DXU to lock the browser to that URL on boot.

On older Windows CE Datalogic:
- Internet Explorer > Tools > Internet Options > Home Page > set the URL

**4. Scanner barcode settings**

Configure via Datalogic Aladdin, DXU, or programming barcodes from the quick-start guide:

| Setting | Value | Why |
|---------|-------|-----|
| Scan Mode | Keyboard Wedge | Sends keystrokes to the browser input |
| Suffix | **CR (Enter)** | Triggers the app to process the scan |
| Prefix | None | Keep it clean |
| Code 128 | Enabled | Canadian PDC barcodes |
| Interleaved 2 of 5 | Enabled | Some US PDC labels |
| Inter-char delay | 0 ms | Fastest input |

**The CR suffix is the most important setting.** Without it, scans just sit in the input field and never process.

### How Scanning Works

```
  Datalogic Scanner                    Your Workstation
  (built-in browser)                   (running server.js)

    Scan barcode
    --- WiFi / keyboard wedge --->  text goes into input field
    --- CR (Enter) suffix ------->  app processes barcode instantly
    <-- screen updates -----------  shows part, qty, wrong dealer alert
```

Each scan sends keystrokes into the focused input field. The Enter suffix triggers processing. Wrong dealer popups, DIPP flagging, quantity adjustments - all right on the scanner screen.

### Scanner Troubleshooting

| Problem | Fix |
|---------|-----|
| Cannot reach the URL | Check both devices are on same WiFi. Try pinging workstation IP from scanner. |
| Scan does not process | Verify CR/Enter suffix is configured in scanner settings |
| Scan appears garbled | Reduce inter-character delay, or check barcode symbology is enabled |
| Page loads slow first time | Normal - 300KB initial download, cached after that |
| WiFi drops | Bookmark the URL. On reconnect, refresh. Scanned items persist in browser storage. |

---

## Windows Firewall

If the scanner cannot reach the workstation, allow port 3000 through Windows Firewall:

1. Open **Windows Defender Firewall with Advanced Settings** (search in Start menu)
2. Click **Inbound Rules** then **New Rule**
3. Select **Port** then Next
4. **TCP**, Specific port: **3000**, then Next
5. **Allow the connection**, then Next
6. Check all profiles (Domain, Private, Public), then Next
7. Name: `GM Parts Receiving`, then Finish

Or run this in PowerShell as admin:
```powershell
New-NetFirewallRule -DisplayName "GM Parts Receiving" -Direction Inbound -Port 3000 -Protocol TCP -Action Allow
```

---

## File Overview

```
gm-parts-receiving/
  SETUP.bat               <-- Run first (installs + builds)
  START.bat               <-- Start the server for testing
  INSTALL-SERVICE.bat     <-- Install as auto-start Windows Service
  UNINSTALL-SERVICE.bat   <-- Remove the Windows Service
  server.js               <-- Production web server
  service-install.js      <-- Service installer logic
  service-uninstall.js    <-- Service uninstaller logic
  package.json            <-- Dependencies
  vite.config.js          <-- Build config
  index.html              <-- Entry HTML
  src/
    main.jsx              <-- React entry
    App.jsx               <-- Full application
```

After running SETUP.bat, a `dist/` folder appears with the built app.

After INSTALL-SERVICE.bat, a `daemon/` folder appears with the service wrapper.

---

## Data Persistence

| Data | Storage | Persists? |
|------|---------|-----------|
| Settings (dealer code, theme, etc.) | Browser localStorage | Yes |
| Scanned items | Browser localStorage | Yes |
| PO / XLSX uploads | Browser session memory | No - re-upload each session |

Note: localStorage is per-browser per-device. The scanner and workstation each have their own scan list.

---

## GitHub

### First Time Setup

```
cd gm-parts-receiving
git init
git add .
git commit -m "Initial commit - GM Parts Receiving v1"
```

Create a **Private** repo at https://github.com/new named `gm-parts-receiving`.
Do not check "initialize with README" (we already have one).

```
git remote add origin https://github.com/YOUR_USERNAME/gm-parts-receiving.git
git branch -M main
git push -u origin main
```

### After Making Changes

```
git add .
git commit -m "Description of changes"
git push
```

---

## Future Enhancements

- [ ] Scan data sync between scanner and workstation over WebSocket
- [ ] Electron wrapper for native Outlook COM integration
- [ ] Export scan session to CSV
- [ ] GM GlobalConnect API for automatic PWB+ data
- [ ] Multi-store support
- [ ] Linux server deployment
