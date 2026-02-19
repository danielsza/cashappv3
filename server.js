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

const server = createServer((req, res) => {
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
  console.log("  Bookmark the Scanner URL on the Datalogic");
  console.log("");
});
