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

// ─── Auto-Import: Folder Watcher ─────────────────────────────────
let watchedFolder = null;
let folderWatcher = null;

ipcMain.handle("watch-folder", async (event, { folderPath }) => {
  try {
    if (folderWatcher) { folderWatcher.close(); folderWatcher = null; }
    if (!folderPath) return { success: true, message: "Watcher stopped" };
    if (!fs.existsSync(folderPath)) fs.mkdirSync(folderPath, { recursive: true });
    watchedFolder = folderPath;
    folderWatcher = fs.watch(folderPath, (eventType, filename) => {
      if (eventType === "rename" && filename && /\.xlsx$/i.test(filename)) {
        const filePath = path.join(folderPath, filename);
        // Small delay to let the file finish writing
        setTimeout(() => {
          if (fs.existsSync(filePath)) {
            try {
              const buf = fs.readFileSync(filePath);
              const b64 = buf.toString("base64");
              if (mainWindow) {
                mainWindow.webContents.send("gc-file-detected", { filename, base64: b64 });
              }
            } catch (err) { console.error("[watch] Error reading", filename, err.message); }
          }
        }, 1500);
      }
    });
    // Also scan existing files on start
    const existing = fs.readdirSync(folderPath).filter(f => /\.xlsx$/i.test(f));
    for (const filename of existing) {
      try {
        const buf = fs.readFileSync(path.join(folderPath, filename));
        const b64 = buf.toString("base64");
        if (mainWindow) {
          mainWindow.webContents.send("gc-file-detected", { filename, base64: b64 });
        }
      } catch (err) { console.error("[watch] Error reading existing", filename, err.message); }
    }
    return { success: true, message: `Watching ${folderPath} (${existing.length} existing files)` };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Browse for folder
ipcMain.handle("browse-folder", async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ["openDirectory"],
    title: "Select GlobalConnect Import Folder",
  });
  return result.canceled ? null : result.filePaths[0];
});

// ─── Auto-Import: Outlook Attachment Extractor ───────────────────
ipcMain.handle("extract-outlook-attachments", async (event, { senderFilter, subjectFilter, targetFolder, daysBack }) => {
  if (process.platform !== "win32") {
    return { success: false, error: "Outlook extraction only available on Windows" };
  }
  try {
    if (!fs.existsSync(targetFolder)) fs.mkdirSync(targetFolder, { recursive: true });
    const psScript = `
$ol = New-Object -ComObject Outlook.Application
$ns = $ol.GetNameSpace("MAPI")
$inbox = $ns.GetDefaultFolder(6) # olFolderInbox
$cutoff = (Get-Date).AddDays(-${daysBack || 1})
$count = 0
foreach ($item in $inbox.Items) {
  if ($item.ReceivedTime -lt $cutoff) { continue }
  $match = $true
  ${senderFilter ? `if ($item.SenderEmailAddress -notlike '*${senderFilter.replace(/'/g, "''")}*') { $match = $false }` : ""}
  ${subjectFilter ? `if ($item.Subject -notlike '*${subjectFilter.replace(/'/g, "''")}*') { $match = $false }` : ""}
  if ($match -and $item.Attachments.Count -gt 0) {
    foreach ($att in $item.Attachments) {
      if ($att.FileName -like "*.xlsx") {
        $savePath = Join-Path '${targetFolder.replace(/'/g, "''")}' $att.FileName
        $att.SaveAsFile($savePath)
        $count++
      }
    }
  }
}
Write-Output "$count"
`;
    const tmpPs = path.join(app.getPath("temp"), "outlook_extract.ps1");
    fs.writeFileSync(tmpPs, psScript, "utf-8");
    const { execSync } = require("child_process");
    const result = execSync(`powershell -ExecutionPolicy Bypass -File "${tmpPs}"`, { timeout: 30000 }).toString().trim();
    fs.unlinkSync(tmpPs);
    return { success: true, count: parseInt(result) || 0 };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// ─── Auto-Import: IMAP Email Fetch ──────────────────────────────
ipcMain.handle("imap-fetch", async (event, { host, port, secure, user, pass, senderFilter, subjectFilter, daysBack, targetFolder }) => {
  try {
    const { ImapFlow } = require("imapflow");
    if (!fs.existsSync(targetFolder)) fs.mkdirSync(targetFolder, { recursive: true });

    const client = new ImapFlow({
      host, port: port || (secure ? 993 : 143),
      secure: secure !== false,
      auth: { user, pass },
      logger: false,
    });

    await client.connect();
    const lock = await client.getMailboxLock("INBOX");
    let count = 0;

    try {
      const since = new Date();
      since.setDate(since.getDate() - (daysBack || 1));

      // Build search criteria
      const searchCriteria = { since };
      if (senderFilter) searchCriteria.from = senderFilter;
      if (subjectFilter) searchCriteria.subject = subjectFilter;

      const messages = client.fetch(searchCriteria, {
        envelope: true,
        bodyStructure: true,
        source: true,
      });

      for await (const msg of messages) {
        // Parse MIME to find .xlsx attachments
        const source = msg.source.toString();
        const parts = parseMimeParts(source);

        for (const part of parts) {
          if (part.filename && /\.xlsx$/i.test(part.filename)) {
            const savePath = path.join(targetFolder, part.filename);
            fs.writeFileSync(savePath, part.content);
            count++;
          }
        }
      }
    } finally {
      lock.release();
    }

    await client.logout();
    return { success: true, count };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Simple MIME parser for extractments
function parseMimeParts(rawEmail) {
  const parts = [];
  // Find boundary from Content-Type header
  const boundaryMatch = rawEmail.match(/boundary="?([^";\r\n]+)"?/i);
  if (!boundaryMatch) return parts;

  const boundary = boundaryMatch[1];
  const sections = rawEmail.split("--" + boundary);

  for (const section of sections) {
    // Look for attachment with .xlsx filename
    const filenameMatch = section.match(/filename="?([^";\r\n]+\.xlsx)"?/i);
    if (!filenameMatch) continue;

    const filename = filenameMatch[1].trim();
    // Find Content-Transfer-Encoding
    const encodingMatch = section.match(/Content-Transfer-Encoding:\s*(\S+)/i);
    const encoding = encodingMatch ? encodingMatch[1].toLowerCase() : "7bit";

    // Extract body (after double newline/CRLF)
    const bodyStart = section.indexOf("\r\n\r\n");
    if (bodyStart === -1) continue;
    let body = section.substring(bodyStart + 4).trim();

    // Remove trailing boundary marker
    const trailingBoundary = body.lastIndexOf("--" + boundary);
    if (trailingBoundary > 0) body = body.substring(0, trailingBoundary).trim();

    let content;
    if (encoding === "base64") {
      content = Buffer.from(body.replace(/\s/g, ""), "base64");
    } else {
      content = Buffer.from(body);
    }

    parts.push({ filename, content });
  }
  return parts;
}

// IMAP connection test
ipcMain.handle("imap-test", async (event, { host, port, secure, user, pass }) => {
  try {
    const { ImapFlow } = require("imapflow");
    const client = new ImapFlow({
      host, port: port || (secure ? 993 : 143),
      secure: secure !== false,
      auth: { user, pass },
      logger: false,
    });
    await client.connect();
    const mailbox = await client.status("INBOX", { messages: true, unseen: true });
    await client.logout();
    return { success: true, messages: mailbox.messages, unseen: mailbox.unseen };
  } catch (err) {
    return { success: false, error: err.message };
  }
});
