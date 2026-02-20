const { app, BrowserWindow, ipcMain, dialog, shell } = require("electron");
const path = require("path");
const fs = require("fs");
const { spawn } = require("child_process");

// Keep a global reference to prevent garbage collection
let mainWindow;
let serverProcess;

function startSyncServer() {
  // In production, start the sync server for scanner communication
  if (!process.env.ELECTRON_DEV) {
    const serverPath = path.join(__dirname, "..", "server.js");
    if (fs.existsSync(serverPath)) {
      serverProcess = spawn(process.execPath, [serverPath], {
        env: { ...process.env, PORT: "3000" },
        stdio: "pipe",
      });
      serverProcess.stdout.on("data", (d) => console.log("[server]", d.toString().trim()));
      serverProcess.stderr.on("data", (d) => console.error("[server]", d.toString().trim()));
      console.log("Sync server started on port 3000");
    }
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 800,
    minHeight: 600,
    title: "GM Parts Receiving",
    icon: path.join(__dirname, "icon.ico"),
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  // In dev mode, load from Vite dev server
  if (process.env.ELECTRON_DEV) {
    mainWindow.loadURL("http://localhost:5173");
    mainWindow.webContents.openDevTools();
  } else {
    // In production, load from built files
    mainWindow.loadFile(path.join(__dirname, "..", "dist", "index.html"));
  }

  mainWindow.on("closed", () => { mainWindow = null; });
}

app.whenReady().then(() => { startSyncServer(); createWindow(); });
app.on("window-all-closed", () => { if (process.platform !== "darwin") app.quit(); });
app.on("activate", () => { if (mainWindow === null) createWindow(); });
app.on("before-quit", () => { if (serverProcess) serverProcess.kill(); });

// ─── IPC Handlers ──────────────────────────────────────────────

// Send email via Outlook COM (Windows only)
ipcMain.handle("outlook-send", async (event, { to, cc, subject, body, pdfPath, pdfFilename }) => {
  if (process.platform !== "win32") {
    return { success: false, error: "Outlook COM only available on Windows" };
  }
  try {
    // Try edge-js or winax for COM automation
    let createOutlookMail;
    try {
      // Preferred: winax (native COM)
      const winax = require("winax");
      createOutlookMail = async () => {
        const outlook = new winax.Object("Outlook.Application");
        const mail = outlook.CreateItem(0); // olMailItem
        mail.To = to;
        if (cc) mail.CC = cc;
        mail.Subject = subject;
        mail.Body = body;
        if (pdfPath && fs.existsSync(pdfPath)) {
          mail.Attachments.Add(pdfPath);
        }
        mail.Display(); // Show the email for review before sending
        return { success: true };
      };
    } catch (e) {
      // Fallback: PowerShell COM automation
      createOutlookMail = async () => {
        const { execSync } = require("child_process");
        const psScript = `
          $ol = New-Object -ComObject Outlook.Application
          $mail = $ol.CreateItem(0)
          $mail.To = '${to.replace(/'/g, "''")}'
          ${cc ? `$mail.CC = '${cc.replace(/'/g, "''")}'` : ""}
          $mail.Subject = '${subject.replace(/'/g, "''")}'
          $mail.Body = '${body.replace(/'/g, "''").replace(/\n/g, "`n")}'
          ${pdfPath ? `$mail.Attachments.Add('${pdfPath.replace(/'/g, "''")}')` : ""}
          $mail.Display()
        `;
        const tmpPs = path.join(app.getPath("temp"), "outlook_send.ps1");
        fs.writeFileSync(tmpPs, psScript, "utf-8");
        execSync(`powershell -ExecutionPolicy Bypass -File "${tmpPs}"`, { timeout: 15000 });
        fs.unlinkSync(tmpPs);
        return { success: true };
      };
    }
    return await createOutlookMail();
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Save PDF to temp and return path (for Outlook attachment)
ipcMain.handle("save-temp-pdf", async (event, { base64, filename }) => {
  try {
    const tmpDir = app.getPath("temp");
    const filePath = path.join(tmpDir, filename);
    const buffer = Buffer.from(base64, "base64");
    fs.writeFileSync(filePath, buffer);
    return { success: true, path: filePath };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Print PDF directly
ipcMain.handle("print-pdf", async (event, { base64, filename }) => {
  try {
    const tmpDir = app.getPath("temp");
    const filePath = path.join(tmpDir, filename);
    fs.writeFileSync(filePath, Buffer.from(base64, "base64"));
    shell.openPath(filePath); // Opens in default PDF viewer for printing
    return { success: true };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// File dialog for saving
ipcMain.handle("save-dialog", async (event, { defaultName, filters }) => {
  const result = await dialog.showSaveDialog(mainWindow, {
    defaultPath: defaultName,
    filters: filters || [{ name: "All Files", extensions: ["*"] }],
  });
  return result.canceled ? null : result.filePath;
});

// Get app info
ipcMain.handle("get-app-info", async () => {
  return {
    version: app.getVersion(),
    platform: process.platform,
    isElectron: true,
  };
});
