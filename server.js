import { createServer } from "node:http";
import { readFileSync, existsSync, statSync } from "node:fs";
import { join, extname } from "node:path";
import { fileURLToPath } from "node:url";
import { dirname } from "node:path";
import { networkInterfaces } from "node:os";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const DIST = join(__dirname, "dist");
const PORT = process.env.PORT || 3000;

const MIME = {
  ".html": "text/html", ".js": "application/javascript", ".css": "text/css",
  ".json": "application/json", ".png": "image/png", ".jpg": "image/jpeg",
  ".svg": "image/svg+xml", ".ico": "image/x-icon",
  ".woff": "font/woff", ".woff2": "font/woff2",
};

if (!existsSync(DIST)) {
  console.error("ERROR: dist/ folder not found. Run 'npm run build' first.");
  process.exit(1);
}

function getLocalIP() {
  const nets = networkInterfaces();
  for (const name of Object.keys(nets)) {
    for (const net of nets[name]) {
      if (net.family === "IPv4" && !net.internal) return net.address;
    }
  }
  return "localhost";
}

// --- Sync Store ---
let syncData = { scans: [], version: 0 };

function handleAPI(req, res) {
  const cors = {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, POST, DELETE, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
  };
  if (req.method === "OPTIONS") {
    res.writeHead(204, cors);
    res.end();
    return true;
  }
  const url = req.url.split("?")[0];

  if (url === "/api/scans" && req.method === "GET") {
    res.writeHead(200, { ...cors, "Content-Type": "application/json" });
    res.end(JSON.stringify({ scans: syncData.scans, version: syncData.version }));
    return true;
  }

  if (url === "/api/scans" && req.method === "POST") {
    let body = "";
    req.on("data", (chunk) => (body += chunk));
    req.on("end", () => {
      try {
        const data = JSON.parse(body);
        if (Array.isArray(data.scans)) {
          syncData.scans = data.scans;
          syncData.version++;
          console.log("  [SYNC] " + data.scans.length + " scans (v" + syncData.version + ")");
        }
        res.writeHead(200, { ...cors, "Content-Type": "application/json" });
        res.end(JSON.stringify({ ok: true, version: syncData.version }));
      } catch (e) {
        res.writeHead(400, { ...cors, "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: "Invalid JSON" }));
      }
    });
    return true;
  }

  if (url === "/api/scans" && req.method === "DELETE") {
    syncData.scans = [];
    syncData.version++;
    res.writeHead(200, { ...cors, "Content-Type": "application/json" });
    res.end(JSON.stringify({ ok: true, version: syncData.version }));
    return true;
  }

  if (url === "/api/version" && req.method === "GET") {
    res.writeHead(200, { ...cors, "Content-Type": "application/json" });
    res.end(JSON.stringify({ version: syncData.version, count: syncData.scans.length }));
    return true;
  }

  return false;
}

const server = createServer((req, res) => {
  if (req.url.startsWith("/api/")) {
    if (handleAPI(req, res)) return;
  }
  let url = req.url.split("?")[0];
  let filePath = join(DIST, url);
  if (!extname(filePath) || !existsSync(filePath)) {
    filePath = join(DIST, "index.html");
  }
  try {
    if (existsSync(filePath) && statSync(filePath).isFile()) {
      const ext = extname(filePath);
      const data = readFileSync(filePath);
      res.writeHead(200, {
        "Content-Type": MIME[ext] || "application/octet-stream",
        "Cache-Control": ext === ".html" ? "no-cache" : "public, max-age=31536000",
      });
      res.end(data);
    } else {
      const data = readFileSync(join(DIST, "index.html"));
      res.writeHead(200, { "Content-Type": "text/html" });
      res.end(data);
    }
  } catch (err) {
    res.writeHead(500);
    res.end("Internal Server Error");
  }
});

server.listen(PORT, "0.0.0.0", () => {
  const ip = getLocalIP();
  console.log("");
  console.log("  GM Parts Receiving - Server Running");
  console.log("  ============================================");
  console.log("  Workstation:  http://localhost:" + PORT);
  console.log("  Scanner:      http://" + ip + ":" + PORT);
  console.log("  ============================================");
  console.log("  Scanner > Workstation sync: ENABLED");
  console.log("  Bookmark the Scanner URL on the Datalogic");
  console.log("");
});
