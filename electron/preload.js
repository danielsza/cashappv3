const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
  // Outlook integration
  sendOutlookEmail: (opts) => ipcRenderer.invoke("outlook-send", opts),
  saveTempPdf: (opts) => ipcRenderer.invoke("save-temp-pdf", opts),

  // Printing
  printPdf: (opts) => ipcRenderer.invoke("print-pdf", opts),

  // File dialogs
  saveDialog: (opts) => ipcRenderer.invoke("save-dialog", opts),
  browseFolder: () => ipcRenderer.invoke("browse-folder"),

  // App info
  getAppInfo: () => ipcRenderer.invoke("get-app-info"),

  // Auto-import: folder watcher
  watchFolder: (opts) => ipcRenderer.invoke("watch-folder", opts),
  onGcFileDetected: (callback) => ipcRenderer.on("gc-file-detected", (event, data) => callback(data)),

  // Auto-import: Outlook attachment extractor
  extractOutlookAttachments: (opts) => ipcRenderer.invoke("extract-outlook-attachments", opts),

  // Auto-import: IMAP email fetch
  imapFetch: (opts) => ipcRenderer.invoke("imap-fetch", opts),
  imapTest: (opts) => ipcRenderer.invoke("imap-test", opts),

  // Proxy fetch (CORS bypass for GM API)
  proxyFetch: (opts) => ipcRenderer.invoke("proxy-fetch", opts),

  // GlobalConnect Direct API (MSAL auth + REST)
  gcLogin: () => ipcRenderer.invoke("gc-login"),
  gcTokenStatus: () => ipcRenderer.invoke("gc-token-status"),
  gcLogout: () => ipcRenderer.invoke("gc-logout"),
  gcFetchShipments: (opts) => ipcRenderer.invoke("gc-fetch-shipments", opts),
  gcFetchAnswerbacks: (opts) => ipcRenderer.invoke("gc-fetch-answerbacks", opts),
  gcFetchAll: (opts) => ipcRenderer.invoke("gc-fetch-all", opts),

  // Check if running in Electron
  isElectron: true,
});
