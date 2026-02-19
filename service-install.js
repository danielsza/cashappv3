// Service Installer for Windows
// Run: npm run install-service (from admin Command Prompt)

import { Service } from "node-windows";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { dirname } from "node:path";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const svc = new Service({
  name: "GM Parts Receiving",
  description: "GM Parts Receiving web server for barcode scanning and shipment processing",
  script: join(__dirname, "server.js"),
  nodeOptions: [],
  env: [{ name: "PORT", value: "3000" }],
});

svc.on("install", () => {
  console.log("");
  console.log("  Service installed successfully!");
  console.log("  Starting service...");
  svc.start();
});

svc.on("start", () => {
  console.log("  Service is running.");
  console.log("");
  console.log("  The app will now start automatically when Windows boots.");
  console.log("  Open http://localhost:3000 on the workstation");
  console.log("  Open http://YOUR_PC_IP:3000 on the scanner");
  console.log("");
  console.log("  To manage: Win+R > services.msc > 'GM Parts Receiving'");
  console.log("");
});

svc.on("alreadyinstalled", () => {
  console.log("  Service is already installed. Starting...");
  svc.start();
});

svc.on("error", (err) => {
  console.error("  Error:", err);
});

svc.install();
