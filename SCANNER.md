# Datalogic Scanner Setup — Complete Guide

## Hardware

We use **Datalogic Memor** (or similar Android-based Datalogic scanners). These have a built-in web browser that connects to the app over WiFi — no RDP, no remote desktop, no special client software.

## Network Requirements

- Scanner and workstation on the **same network/VLAN**
- Workstation running `server.js` on port **3000**
- Port 3000 open in Windows Firewall (see README.md)

## Scanner Configuration

### Step 1: WiFi

Connect the scanner to the dealership WiFi. The scanner needs to reach the workstation IP on port 3000.

To verify connectivity:
1. Open the browser on the scanner
2. Navigate to `http://WORKSTATION_IP:3000`
3. If the app loads, you're good

### Step 2: Barcode Settings

Configure via **Datalogic DXU** (desktop utility), **Aladdin** (on-device), or programming barcodes from the quick-start card.

**Critical settings:**

| Setting | Value | Notes |
|---------|-------|-------|
| Scan Mode | **Keyboard Wedge** | Scanner sends keystrokes to the focused browser input field |
| Suffix | **CR (Enter / 0x0D)** | **Most important setting** — triggers the app to process the scan |
| Prefix | None | No prefix needed |
| Inter-character delay | **0 ms** | Fastest keystroke injection |

**Symbologies to enable:**

| Symbology | Why |
|-----------|-----|
| Code 128 | Canadian PDC barcodes (standard GM) |
| Interleaved 2 of 5 | Some US PDC labels |
| EAN-13 / UPC-A | Some aftermarket/ACDelco parts |
| Code 39 | Older GM labels |

Disable everything else to prevent false reads.

### Step 3: Browser Setup

1. Open **Chrome** (or built-in browser) on the scanner
2. Navigate to: `http://WORKSTATION_IP:3000`
3. Tap **📱 Scanner** to enter Scanner mode
4. Tap the **input field** at the top so it has focus
5. Scan a barcode — it should appear and process immediately

### Step 4: Bookmark / Homepage

**Set as homepage** so the scanner is ready on boot:

- Chrome → ⋮ → Settings → Homepage → `http://WORKSTATION_IP:3000`
- Or: Chrome → ⋮ → Add to Home Screen (creates an app shortcut)

**For kiosk mode** (locks scanner to the app):
- Use Datalogic **SureLock** or **DXU** to lock the browser to the URL on boot
- This prevents workers from accidentally navigating away

## Barcode Format Reference

The app auto-detects these GM barcode formats:

### Canadian PDC Labels (Code 128)

Format: `(fixed)-(dealerCode)-(partNumber)-(shippingOrder)-(facility)-(qty)-...`

```
Example: 000000-095207-19327371-2310043-036-0002-...
         ^^^^^^ ^^^^^^ ^^^^^^^^ ^^^^^^^ ^^^ ^^^^
         ignore dealer part     ship#   fac  qty
```

The app extracts:
- **Dealer code** (6 digits, position 2) — compared against settings to detect wrong dealer
- **Part number** (8 digits, position 3)
- **Shipping order** (7 digits, position 4)
- **PDC/Facility** (3 digits, position 5)
- **Quantity** (4 digits, position 6)

### US PDC Labels

Different format, shorter. The app handles both formats automatically.

If a barcode doesn't parse with the Canadian format, it tries US format extraction.

## How Scanning Works

```
Scanner (browser)                    Workstation (server.js)
─────────────────                    ──────────────────────

1. Worker points scanner at barcode
2. Datalogic reads barcode → sends keystrokes to browser
3. Enter suffix → app processes immediately
4. App extracts: part#, SO#, dealer, qty
5. Wrong dealer? → RED popup + sound alert
6. Part added to scan list in browser localStorage
7. Every 5 seconds: POST /api/scans to workstation
8.                                   Workstation polls GET /api/scans
9.                                   Compare tab updates in real-time
```

Scanner and workstation sync via the server.js API:
- **Scanner** → `POST /api/scans` (pushes scan data every 5 seconds)
- **Workstation** → `GET /api/scans` (polls for updates every 5 seconds)
- **Clear** → `DELETE /api/scans` (resets scan buffer)

## Scan Data Persistence

| Location | Data | Survives |
|----------|------|----------|
| Scanner localStorage | All scanned items | Browser refresh, WiFi drop |
| Server memory | Last sync buffer | Server restart clears |
| Workstation localStorage | Merged scan data | Browser refresh |

**Important**: Scanned items persist in the scanner's browser even if WiFi drops. When connectivity returns, the next sync pushes everything to the workstation.

## Troubleshooting

| Problem | Diagnosis | Fix |
|---------|-----------|-----|
| Can't reach URL | `ping WORKSTATION_IP` from scanner | Check WiFi, firewall, same network |
| Barcode doesn't process | Scan appears in input but nothing happens | Configure **CR suffix** in scanner settings |
| Barcode text garbled | Special characters or missing digits | Check symbology is enabled, reduce inter-char delay |
| Wrong dealer not detecting | All scans show as correct dealer | Verify dealer code in Settings matches your actual code |
| Scan list empty on workstation | Scanner has data but workstation doesn't | Check server.js is running, both on same network |
| Page slow first load | Initial bundle download | Normal (~300KB), cached after first load |
| Scanner screen too small | Text hard to read | Scanner mode has large fonts and buttons designed for handheld use |

## DXU Configuration Export

If you configure a scanner with DXU, **export the profile** and save it to the repo so you can push it to replacement scanners quickly.

Save as: `scanner-profiles/datalogic-memor-config.dxu`

## Multiple Scanners

Multiple scanners can connect to the same workstation simultaneously. Each scanner:
- Has its own localStorage (independent scan lists)
- Syncs to the same server.js endpoint
- The workstation merges all incoming scans by part# + SO#

## Factory Reset Recovery

If a scanner needs reconfiguration from scratch:
1. Connect to WiFi
2. Open Chrome → `http://WORKSTATION_IP:3000`
3. Configure barcode settings (Keyboard Wedge + CR suffix)
4. Set as homepage
5. Test a scan
