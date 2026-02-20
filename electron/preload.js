const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
  // Outlook integration
  sendOutlookEmail: (opts) => ipcRenderer.invoke("outlook-send", opts),
  saveTempPdf: (opts) => ipcRenderer.invoke("save-temp-pdf", opts),

  // Printing
  printPdf: (opts) => ipcRenderer.invoke("print-pdf", opts),

  // File dialogs
  saveDialog: (opts) => ipcRenderer.invoke("save-dialog", opts),

  // App info
  getAppInfo: () => ipcRenderer.invoke("get-app-info"),

  // Check if running in Electron
  isElectron: true,
});
