# GM Parts Receiving — Desktop App (Electron)

## Quick Start (Dev Mode)

```bash
# 1. Install dependencies (includes Electron)
npm install

# 2. Start the sync server (for scanner communication) in one terminal
npm run serve

# 3. Start the Electron app in dev mode (another terminal)
npm run electron-dev
```

This starts:
- **Vite dev server** on `http://localhost:5173` (hot reload)
- **Sync server** on `http://localhost:3000` (scanner ↔ workstation)
- **Electron window** loading from Vite with DevTools open

### Alternative: Start Electron manually
```bash
# If electron-dev has issues with concurrently, start separately:
# Terminal 1: Vite
npm run dev

# Terminal 2: Sync server
npm run serve

# Terminal 3: Electron
npm run electron-start
```

## Production Build

```bash
# Build the web app + package as Windows installer
npm run electron-build
```

Output in `release/` folder:
- `GM Parts Receiving Setup X.X.X.exe` — NSIS installer
- `GM Parts Receiving X.X.X.exe` — Portable (no install)

## Features in Desktop App

### Outlook COM Integration
When running as a desktop app on Windows, the **✉ Outlook** button will:
1. Save the PDF to a temp file
2. Open Outlook via COM automation (PowerShell fallback)
3. Create a new email with the PDF already attached
4. Display the email for review before sending

Falls back to `.eml` download if Outlook COM is unavailable.

### Print Integration
The **🖨 Print** button opens the PDF in your default PDF viewer for printing.

### Scanner Sync
The built-in sync server starts automatically when running the packaged app.
Scanners connect to the workstation IP on port 3000.

## Architecture

```
electron/
  main.js      — Main process (window, IPC, Outlook COM, sync server)
  preload.js   — Bridge between main & renderer (contextBridge)
src/
  App.jsx      — React app (detects Electron via window.electronAPI)
server.js      — Scanner sync server (started by Electron in prod)
```

## Settings

### Highlight Color
Settings → General → **Pink Sheet Highlight Color**
Color picker + hex input for the cell fill used on supersession and qty-diff cells.

### Custom PDF Template
Settings → General → **Woodstock PDF Template**
Upload a different dealer's Woodstock form PDF. The app fills in the same field positions.
Click "Reset to default" to revert to the built-in John Bear template.

## Troubleshooting

### `electron` not found
```bash
npm install --save-dev electron
```

### Outlook COM fails
- Ensure Microsoft Outlook is installed and configured
- The app tries native COM first, then PowerShell fallback
- If both fail, it downloads a `.eml` file instead

### Scanner can't connect
- Ensure the sync server is running (port 3000)
- Scanner and workstation must be on the same network
- Check Windows Firewall allows port 3000
