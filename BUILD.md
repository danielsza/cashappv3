# Quick Reference — Build & Deploy

## 🔧 Development (Testing Changes)

```bash
# Pull latest
cd C:\gm-parts-receiving
git pull

# Install deps (only needed if package.json changed)
npm install

# Start dev server with hot reload
npm run dev
# → opens http://localhost:5173
```

## 🌐 Web App Build (Scanner + Browser)

```bash
# Build production bundle
npm run build

# Start production server
npm run serve
# → http://localhost:3000 (workstation)
# → http://YOUR_IP:3000 (scanner)
```

## 🖥️ Desktop App (Electron)

```bash
# Dev mode (hot reload + DevTools)
# Terminal 1:
npm run serve
# Terminal 2:
npm run electron-dev

# --- OR start pieces manually: ---
# Terminal 1: npm run dev
# Terminal 2: npm run serve
# Terminal 3: npm run electron-start
```

```bash
# Package for Windows (installer + portable)
npm run electron-build
# → release/GM Parts Receiving Setup X.X.X.exe
# → release/GM Parts Receiving X.X.X.exe (portable)
```

## ⚙️ Windows Service (Auto-Start Scanner Server)

```bash
# Build first
npm run build

# Install service (run as Administrator)
npm run install-service
# → Creates "GM Parts Receiving" in Windows Services
# → Auto-starts on boot, restarts on crash

# Remove service (run as Administrator)
npm run uninstall-service
```

To manage: `Win+R` → `services.msc` → find "GM Parts Receiving"

## 📦 Deploy to a New Workstation

### Option A: From Git
```bash
git clone https://github.com/danielsza/gm-parts-receiving.git
cd gm-parts-receiving
npm install
npm run build
# Then either:
npm run serve                  # manual start
npm run install-service        # auto-start as service (admin)
```

### Option B: SETUP.bat (No Git)
1. Copy the folder to the new PC
2. Double-click `SETUP.bat`
3. Double-click `START.bat` to test
4. Right-click `INSTALL-SERVICE.bat` → Run as Admin

## 📱 Deploy Scanner

1. Connect scanner to same WiFi as workstation
2. Open Chrome → `http://WORKSTATION_IP:3000`
3. Tap **Scanner** mode
4. Configure barcode: Keyboard Wedge + CR suffix
5. Set as homepage / bookmark
6. See `SCANNER.md` for full details

## 🔄 Update After Changes

```bash
# On the workstation
cd C:\gm-parts-receiving
git pull
npm run build

# If running as service, restart it:
# services.msc → "GM Parts Receiving" → Restart
# Or:
net stop "GM Parts Receiving"
net start "GM Parts Receiving"
```

## 📂 Key Files

| File | Purpose |
|------|---------|
| `src/App.jsx` | The entire React app |
| `src/woodstockTemplate.js` | Base64 PDF template |
| `server.js` | Production server + scanner sync API |
| `electron/main.js` | Electron main process |
| `electron/preload.js` | Electron bridge to renderer |
| `service-install.js` | Windows Service installer |
| `vite.config.js` | Build config |
| `package.json` | Dependencies + scripts |

## 🚨 Common Issues

| Issue | Fix |
|-------|-----|
| `npm: command not found` | Install Node.js from nodejs.org |
| Port 3000 in use | `netstat -ano \| findstr :3000` → kill the process |
| Scanner can't connect | Windows Firewall → allow port 3000 TCP inbound |
| Electron not found | `npm install` (electron is in devDependencies) |
| Service won't install | Must run as Administrator |
| Build fails | `npm install` first, check Node.js version ≥ 18 |
