# Quick Reference — Build, Test & Deploy

## Prerequisites

- **Node.js ≥ 18** — [nodejs.org](https://nodejs.org) (LTS recommended)
- **Git** — for pulling updates
- **macOS or Windows** — Electron works on both

```bash
# Verify
node --version   # should be 18+
npm --version    # should be 9+
```

---

## 🔧 Step 1: Clone & Install

```bash
# First time only
git clone https://github.com/danielsza/gm-parts-receiving.git
cd gm-parts-receiving
npm install
```

```bash
# Subsequent updates
cd gm-parts-receiving      # or wherever you cloned it
git pull
npm install                 # only needed if package.json changed
```

---

## 🌐 Step 2: Build the Web App

```bash
npm run build
```

This compiles `src/App.jsx` → `dist/` (static HTML/JS bundle). Takes ~6 seconds.

---

## 🖥️ Step 3: Run in Electron (Desktop App)

### Quick Start (Dev Mode)

```bash
# One command — starts Vite dev server + Electron together
npm run electron-dev
```

This runs `vite` on port 5173, waits for it, then launches Electron with DevTools open. Hot-reloads on file changes.

### Manual Start (if the above doesn't work)

```bash
# Terminal 1: Start the web dev server
npm run dev

# Terminal 2: Start Electron pointed at dev server
npm run electron-start
```

### Production Mode (no DevTools)

```bash
npm run build
npx electron .
```

---

## ⚡ Step 4: Test GlobalConnect API

1. Launch the Electron app (Step 3)
2. Click the **⚙ Settings** gear icon
3. Go to **Import** tab
4. Scroll to **⚡ GlobalConnect Direct API**
5. Check **Enable API Fetch**
6. Set **Customer Code** to `095207`
7. Click **🔑 Login to PWB+**
   - A Microsoft login popup opens
   - Sign in with your GM/VSP credentials (same as PWB+ website)
   - After login, the popup shows "🔐 Signing in..." then closes
8. Green status bar should appear: "🟢 Authenticated as SZAJKOWSKI, DANIEL"
9. Click **⚡ Test Fetch** to pull today's data
10. Click **Save** to persist settings

### After First Login

- **Refresh token** saved to `~/Library/Application Support/gm-parts-receiving/gc-tokens.json` (macOS) or `%APPDATA%/gm-parts-receiving/gc-tokens.json` (Windows)
- **Microsoft cookies** persist in Electron session
- On next app start, session restores silently — no login needed
- Token auto-refreshes for ~90 days before needing fresh login
- Click **🔓 Logout** to clear saved credentials + cookies

### Using the Fetcher

Once logged in, go to the **Compare** tab:
- **⚡ Fetch GC** button appears (green when authenticated)
- Click it → pulls shipments + answerbacks for today
- Data merges into PO list automatically
- Pink sheet generates with all enrichment (bin, descriptions, shipment numbers)
- Ship Direct lines (DS-xxx) treated as shipped for matching

---

## 📱 Step 5: Test Scanner (Optional)

```bash
# Terminal 1: Start the sync server
npm run serve
# → http://localhost:3000

# Terminal 2: Start Electron app
npm run electron-start
```

On the scanner device:
1. Connect to same WiFi as workstation
2. Open Chrome → `http://WORKSTATION_IP:3000`
3. Tap **Scanner** mode
4. Scan a barcode — it should appear in the Electron app's scan list

---

## 📦 Package for Distribution

### Windows Installer

```bash
npm run electron-build
# → release/GM Parts Receiving Setup X.X.X.exe
# → release/GM Parts Receiving X.X.X.exe (portable)
```

### macOS (add to package.json build section if needed)

```bash
npx electron-builder --mac
# → release/GM Parts Receiving-X.X.X.dmg
```

Note: macOS builds need to run on macOS. Windows builds need Windows (or CI).

---

## ⚙️ Windows Service (Auto-Start Scanner Server)

```bash
npm run build

# Install service (run as Administrator)
npm run install-service
# → Creates "GM Parts Receiving" in Windows Services

# Remove service
npm run uninstall-service
```

Manage: `Win+R` → `services.msc` → "GM Parts Receiving"

---

## 📂 Key Files

| File | Purpose |
|------|---------|
| `src/App.jsx` | The entire React app (UI, parsers, pink sheet, matching) |
| `src/woodstockTemplate.js` | Base64 Woodstock PDF template |
| `electron/main.js` | Electron main process (IPC handlers, folder watcher, IMAP) |
| `electron/gc-api.js` | **GlobalConnect API** (Azure AD auth, PKCE, token refresh, REST calls) |
| `electron/preload.js` | Electron bridge — exposes APIs to renderer |
| `server.js` | Production server + scanner sync WebSocket API |
| `vite.config.js` | Build config (React, proxy, base path) |
| `package.json` | Dependencies, scripts, electron-builder config |

### Token Storage Locations

| OS | Path |
|----|------|
| macOS | `~/Library/Application Support/gm-parts-receiving/gc-tokens.json` |
| Windows | `%APPDATA%/gm-parts-receiving/gc-tokens.json` |
| Linux | `~/.config/gm-parts-receiving/gc-tokens.json` |

---

## 🚨 Troubleshooting

| Issue | Fix |
|-------|-----|
| `npm: command not found` | Install Node.js from nodejs.org |
| `electron: command not found` | Run `npm install` (electron is a devDependency) |
| Port 3000 in use | `lsof -i :3000` (macOS) or `netstat -ano \| findstr :3000` (Windows) |
| Scanner can't connect | Firewall → allow port 3000 TCP inbound |
| GC Login popup closes instantly | Check console for auth errors; try **🔓 Logout** then login again |
| "Token expired" on fetch | Click **🔑 Login** again — refresh token may have expired |
| "AADSTS..." error in login popup | Scope mismatch — share the full error message |
| Service won't install | Must run terminal as Administrator |
| Build fails | `rm -rf node_modules && npm install` then `npm run build` |
| `ELECTRON_DEV` not working | Install `cross-env`: `npm install cross-env --save-dev` |

---

## 🔄 Quick Update Workflow

```bash
cd gm-parts-receiving
git pull
npm install          # if deps changed
npm run build        # rebuild web bundle
# Restart Electron or service
```
