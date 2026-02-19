// Service Uninstaller for Windows
// Run: npm run uninstall-service (from admin Command Prompt)

import { Service } from "node-windows";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { dirname } from "node:path";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const svc = new Service({
  name: "GM Parts Receiving",
  script: join(__dirname, "server.js"),
});

svc.on("uninstall", () => {
  console.log("");
  console.log("  Service uninstalled. It will no longer auto-start.");
  console.log("");
});

svc.on("error", (err) => {
  console.error("  Error:", err);
});

svc.uninstall();
