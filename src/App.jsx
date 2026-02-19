import { useState, useRef, useEffect, useCallback } from "react";
import * as XLSX from "xlsx";
import { jsPDF } from "jspdf";
import "jspdf-autotable";

// ─── Settings Persistence ────────────────────────────────────────
const STORAGE_KEY = "gm-parts-receiving-settings";
const SCANS_KEY = "gm-parts-receiving-scans";

function loadSettings() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return { ...DEFAULTS, ...JSON.parse(raw) };
  } catch (e) {}
  return DEFAULTS;
}
function saveSettingsToStorage(s) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(s)); } catch (e) {}
}
function loadScans() {
  try {
    const raw = localStorage.getItem(SCANS_KEY);
    if (raw) return JSON.parse(raw);
  } catch (e) {}
  return [];
}
function saveScans(items) {
  try { localStorage.setItem(SCANS_KEY, JSON.stringify(items)); } catch (e) {}
}

// ─── Constants ───────────────────────────────────────────────────
const KNOWN_DEALERS_DEFAULT = [
  { code: "095207", name: "John Bear Hamilton", contact: "", email: "" },
  { code: "095182", name: "Grimsby Chevrolet", contact: "Christian Ly", email: "cly@grimsbychev.com" },
];

const DEFAULTS = {
  dealerCode: "095207", dealerName: "JOHN BEAR", area: "80", station: "587",
  phone: "905-575-9400", wdkEmail: "wdk.courtesy@gm.com", theme: "light",
  knownDealers: KNOWN_DEALERS_DEFAULT,
};

// ─── Barcode Helpers ─────────────────────────────────────────────
function parseCanadianBarcode(raw, dc) {
  const s = raw.length === 34 ? "0" + raw : raw;
  if (s.length !== 35) return null;
  if (!/^\d+$/.test(s)) return null;
  return { type: "CA", pdc: s.substring(0, 3), shippingOrder: s.substring(3, 10), dealerCode: s.substring(10, 16), partNumber: s.substring(16, 24), serial: s.substring(24), raw, wrongDealer: s.substring(10, 16) !== dc };
}

function parseUSBarcode(combined, dc) {
  if (combined.length < 24) return null;
  return { type: "US", pdc: combined.substring(0, 3), shippingOrder: combined.substring(3, 10), dealerCode: combined.substring(10, 16), partNumber: combined.substring(16, 24), serial: combined.length > 24 ? combined.substring(24) : "", raw: combined, wrongDealer: combined.substring(10, 16) !== dc };
}

function isQuantityScan(val) { const n = parseInt(val, 10); return !isNaN(n) && n >= 2 && n <= 99 && String(n) === val.trim(); }

function classifyScan(val) {
  if (!val || val.length === 0) return "empty";
  if (isQuantityScan(val)) return "quantity";
  if (val.length === 34 || val.length === 35) return "canadian";
  if (val.length === 8 && /^[A-Z0-9]+$/i.test(val)) return "us_part";
  if (val.length >= 10 && val.length <= 18) return "us_header_old";
  if (val.length === 19) return "us_header_new";
  if (val.length === 24) return "us_full";
  if (val.length >= 20 && val.length <= 33) return "incomplete_canadian";
  if (val.length >= 1 && val.length <= 7) return "incomplete_short";
  if (val.length > 35) return "too_long";
  return "unknown";
}

// ─── XLSX / CSV ──────────────────────────────────────────────────
function parseXLSXFile(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const wb = XLSX.read(e.target.result, { type: "array" });
        resolve(XLSX.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]], { defval: "" }));
      } catch (err) { reject(err); }
    };
    reader.readAsArrayBuffer(file);
  });
}

function parseCSVText(text) {
  const lines = text.trim().split("\n"); if (lines.length < 2) return [];
  const headers = lines[0].split(/[,\t]/).map(h => h.trim().replace(/^"|"$/g, ""));
  return lines.slice(1).map(line => { const vals = line.split(/[,\t]/).map(v => v.trim().replace(/^"|"$/g, "")); const obj = {}; headers.forEach((h, i) => { obj[h] = vals[i] || ""; }); return obj; });
}

function parseFilename(name) {
  const clean = name.replace(/\.[^.]+$/, ""), prefix = "po__control__details_";
  if (!clean.startsWith(prefix)) { const p = clean.split("_").filter(Boolean); return { pbsPO: p[0] || "Unknown", gmControl: p[1] || "", dateStr: "" }; }
  const seg = clean.substring(prefix.length).split("_");
  return { pbsPO: seg[0] || "", gmControl: seg[1] || "", dateStr: seg.slice(2).join("-") || "" };
}

// ─── GM Logo (base64 PNG) ────────────────────────────────────────
const GM_LOGO_B64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAZAAAAA7CAYAAABVE9xVAAAKMWlDQ1BJQ0MgUHJvZmlsZQAAeJydlndUU9kWh8+9N71QkhCKlNBraFICSA29SJEuKjEJEErAkAAiNkRUcERRkaYIMijggKNDkbEiioUBUbHrBBlE1HFwFBuWSWStGd+8ee/Nm98f935rn73P3Wfvfda6AJD8gwXCTFgJgAyhWBTh58WIjYtnYAcBDPAAA2wA4HCzs0IW+EYCmQJ82IxsmRP4F726DiD5+yrTP4zBAP+flLlZIjEAUJiM5/L42VwZF8k4PVecJbdPyZi2NE3OMErOIlmCMlaTc/IsW3z2mWUPOfMyhDwZy3PO4mXw5Nwn4405Er6MkWAZF+cI+LkyviZjg3RJhkDGb+SxGXxONgAoktwu5nNTZGwtY5IoMoIt43kA4EjJX/DSL1jMzxPLD8XOzFouEiSniBkmXFOGjZMTi+HPz03ni8XMMA43jSPiMdiZGVkc4XIAZs/8WRR5bRmyIjvYODk4MG0tbb4o1H9d/JuS93aWXoR/7hlEH/jD9ld+mQ0AsKZltdn6h21pFQBd6wFQu/2HzWAvAIqyvnUOfXEeunxeUsTiLGcrq9zcXEsBn2spL+jv+p8Of0NffM9Svt3v5WF485M4knQxQ143bmZ6pkTEyM7icPkM5p+H+B8H/nUeFhH8JL6IL5RFRMumTCBMlrVbyBOIBZlChkD4n5r4D8P+pNm5lona+BHQllgCpSEaQH4eACgqESAJe2Qr0O99C8ZHA/nNi9GZmJ37z4L+fVe4TP7IFiR/jmNHRDK4ElHO7Jr8WgI0IABFQAPqQBvoAxPABLbAEbgAD+ADAkEoiARxYDHgghSQAUQgFxSAtaAYlIKtYCeoBnWgETSDNnAYdIFj4DQ4By6By2AE3AFSMA6egCnwCsxAEISFyBAVUod0IEPIHLKFWJAb5AMFQxFQHJQIJUNCSAIVQOugUqgcqobqoWboW+godBq6AA1Dt6BRaBL6FXoHIzAJpsFasBFsBbNgTzgIjoQXwcnwMjgfLoK3wJVwA3wQ7oRPw5fgEVgKP4GnEYAQETqiizARFsJGQpF4JAkRIauQEqQCaUDakB6kH7mKSJGnyFsUBkVFMVBMlAvKHxWF4qKWoVahNqOqUQdQnag+1FXUKGoK9RFNRmuizdHO6AB0LDoZnYsuRlegm9Ad6LPoEfQ4+hUGg6FjjDGOGH9MHCYVswKzGbMb0445hRnGjGGmsVisOtYc64oNxXKwYmwhtgp7EHsSewU7jn2DI+J0cLY4X1w8TogrxFXgWnAncFdwE7gZvBLeEO+MD8Xz8MvxZfhGfA9+ED+OnyEoE4wJroRIQiphLaGS0EY4X1w8TogrxFXgWnAncFdwE7gZvBLeEO+MD8Xz8MvxZfhGfA9+ED+OnyEoE4wJroRIQiphLaGy0EZ4X1w8TogrxFXgWnAncFdwE7gZvBLeEO+MD8Xz8MvxZfhGfA9+EA==";

// ─── PDF helper: draw underlined text ────────────────────────────
function drawUnderlinedText(doc, text, x, y, opts = {}) {
  const { bold = false, fontSize } = opts;
  if (fontSize) doc.setFontSize(fontSize);
  if (bold) doc.setFont("helvetica", "bold"); else doc.setFont("helvetica", "normal");
  doc.text(text, x, y);
  const tw = doc.getTextWidth(text);
  const sz = doc.getFontSize();
  doc.setLineWidth(0.3);
  doc.line(x, y + sz * 0.01 * 4, x + tw, y + sz * 0.01 * 4);
}

// ─── PDF Generator ───────────────────────────────────────────────
function generateWoodstockPDF({ settings, shortItems, dippItems, dippComments, dippDescriptions = {}, wrongDealerItems, completedBy, formDate, poInfo }) {
  const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "letter" });
  const pw = doc.internal.pageSize.getWidth();   // 215.9
  const ph = doc.internal.pageSize.getHeight();   // 279.4
  const ml = 12, mr = 12;
  const cw = pw - ml - mr;
  let y = 8;

  // ── Outer border (thick) ──
  doc.setDrawColor(0); doc.setLineWidth(0.8);
  doc.rect(6, 6, pw - 12, ph - 12);

  // ── GM Logo (top-left) ──
  try { doc.addImage(GM_LOGO_B64, "PNG", 9, 9, 45, 7); } catch(e) { /* logo optional */ }

  // ── Header text ──
  y = 12;
  doc.setFontSize(16); doc.setFont("helvetica", "bold");
  doc.text("General Motors of Canada Ltd.", pw / 2 + 10, y, { align: "center" });
  y += 6;
  doc.setFontSize(12); doc.setFont("helvetica", "bold");
  doc.text("National Parts Distribution Center", pw / 2, y, { align: "center" });
  y += 5;
  doc.text("Woodstock, Ontario", pw / 2, y, { align: "center" });
  y += 5;
  doc.text("Your Satisfaction Is Our Goal!", pw / 2, y, { align: "center" });
  y += 7;

  // ── Area / Station / Dealer info line ──
  doc.setFontSize(11); doc.setFont("helvetica", "bold");
  const areaLine = `Area:  ${settings.area || "80"}     Station No.:        ${settings.station || "587"}           Dealer Name:             ${settings.dealerName || ""}          Dealer Code: ${settings.dealerCode || ""}`;
  doc.text(areaLine, ml, y);
  y += 6;

  // ── "It was a pleasure..." (bold, NOT italic) ──
  doc.setFontSize(12); doc.setFont("helvetica", "bold");
  doc.text("It was a pleasure to pack your order.  Your satisfaction is our goal!", ml, y);
  y += 7;

  // ── Paragraph 1: mixed normal + bold ──
  doc.setFontSize(10); doc.setFont("helvetica", "normal");
  const p1a = "In order to improve the quality of your shipment, we ask that you complete this sheet when you receive";
  const p1aLines = doc.splitTextToSize(p1a, cw);
  doc.text(p1aLines, ml, y);
  // Calculate where "your order." ends to start bold text
  y += p1aLines.length * 4;
  doc.setFont("helvetica", "normal");
  const p1b = "your order.  ";
  doc.text(p1b, ml, y);
  const p1bW = doc.getTextWidth(p1b);
  doc.setFont("helvetica", "bold");
  const p1c = "This will allow us to start the investigation on any missing parts shipped out of";
  doc.text(p1c, ml + p1bW, y);
  y += 4;
  doc.text("Woodstock.", ml, y);
  y += 6;

  // ── "If you receive an overage..." (bold) ──
  doc.setFontSize(10); doc.setFont("helvetica", "bold");
  doc.text("If you receive an overage or wrong part, please claim on Parts Workbench.", ml, y);
  y += 6;

  // ── Warning paragraph with mixed bold+underline ──
  doc.setFontSize(9); doc.setFont("helvetica", "normal");
  let cx = ml;
  const writeText = (text, bold, underline) => {
    doc.setFont("helvetica", bold ? "bold" : "normal");
    // Handle word wrapping manually for mixed formatting
    const words = text.split(" ");
    for (let i = 0; i < words.length; i++) {
      const word = (i < words.length - 1) ? words[i] + " " : words[i];
      const ww = doc.getTextWidth(word);
      if (cx + ww > pw - mr) { cx = ml; y += 3.8; }
      doc.text(word, cx, y);
      if (underline) {
        const sz = doc.getFontSize();
        doc.setLineWidth(0.25);
        doc.line(cx, y + 0.5, cx + ww - (i < words.length - 1 ? doc.getTextWidth(" ") * 0.3 : 0), y + 0.5);
      }
      cx += ww;
    }
  };

  writeText("All parts received in a non-returnable condition must be reported within ", false, false);
  writeText("24 hours", true, true);
  writeText(" of receiving and ", false, false);
  writeText("claimed as damaged on Parts Workbench", true, true);
  writeText(". These parts will be accepted without penalty to your dealership. ", false, false);
  writeText("If you submit a return on a part in a non-returnable condition or has carrier caused damage that has not been reported within the 24 hours it will be refused.", true, true);
  y += 7; cx = ml;

  // ── Completed by / Date / Phone ──
  doc.setFontSize(12); doc.setFont("helvetica", "bold");
  doc.text(`Completed by: ${completedBy || "________________"}`, ml, y);
  doc.text(`Date: ${formDate || "_________"}`, ml + 75, y);
  doc.text(`Phone Number: ${settings.phone || "__________"}`, ml + 120, y);
  if (poInfo) { doc.setFontSize(7); doc.setFont("helvetica", "normal"); doc.text(`PO: ${poInfo.pbsPO}  GM#: ${poInfo.gmControl}`, pw - mr, y + 4, { align: "right" }); }
  y += 8;

  // ── Table styling (white header bg, black grid) ──
  const tOpts = {
    theme: "grid",
    headStyles: { fillColor: [255, 255, 255], textColor: [0, 0, 0], fontStyle: "bold", fontSize: 9, cellPadding: 1.5, halign: "center" },
    bodyStyles: { fontSize: 9, cellPadding: 1.5, minCellHeight: 6, textColor: [0, 0, 0] },
    styles: { lineColor: [0, 0, 0], lineWidth: 0.3, font: "helvetica" },
    margin: { left: ml, right: mr },
    tableLineColor: [0, 0, 0], tableLineWidth: 0.3
  };

  // ── SHORT INFO table (always shown) ──
  doc.autoTable({
    startY: y, ...tOpts,
    head: [
      [{ content: "SHORT INFO", colSpan: 6, styles: { halign: "center", fontStyle: "normal", fontSize: 10 } }],
      ["Item #", "Part #", "SO #", "QTY\nordered", "QTY\nReceived", "Tote or\nPallet ?"]
    ],
    headStyles: { ...tOpts.headStyles, fontSize: 9, cellPadding: 1.2 },
    body: [
      ...shortItems.map((r, i) => [String(i + 1), r.partNumber, r.shippingOrder, String(r.expectedQty), String(r.scannedQty), ""]),
      ...Array(Math.max(0, 4 - shortItems.length)).fill(["", "", "", "", "", ""])
    ],
    columnStyles: {
      0: { cellWidth: 18, halign: "center" },
      1: { cellWidth: 42 },
      2: { cellWidth: 42 },
      3: { cellWidth: 22, halign: "center" },
      4: { cellWidth: 22, halign: "center" },
      5: { cellWidth: 24, halign: "center" }
    }
  });
  y = doc.lastAutoTable.finalY + 4;

  // ── DIPP section title (bold + underlined text) ──
  doc.setFontSize(10); doc.setFont("helvetica", "bold");
  const dippTitle = "DIPP label Request \u2013  parts received with package damage/carrier damage or non-returnable";
  doc.text(dippTitle, ml, y);
  let dtw = doc.getTextWidth(dippTitle);
  doc.setLineWidth(0.25); doc.line(ml, y + 0.6, ml + dtw, y + 0.6);
  y += 4;
  const dippTitle2 = "condition";
  doc.text(dippTitle2, ml, y);
  const dtw2 = doc.getTextWidth(dippTitle2);
  doc.line(ml, y + 0.6, ml + dtw2, y + 0.6);
  y += 2;

  // ── DIPP table ──
  doc.autoTable({
    startY: y, ...tOpts,
    head: [["Part #", "PDC", "SO #", "Description", "Comments- i.e \u2013\nbox damaged,\nopen package", "DIPP\nrequested\n(Y/N)"]],
    headStyles: { ...tOpts.headStyles, fontSize: 8, cellPadding: 1.2 },
    body: [
      ...dippItems.map(item => [item.partNumber, item.pdc, item.shippingOrder, dippDescriptions[item.id] || "", dippComments[item.id] || "", "Y"]),
      ...Array(Math.max(0, 3 - dippItems.length)).fill(["", "", "", "", "", ""])
    ],
    columnStyles: {
      0: { cellWidth: 28, halign: "center" },
      1: { cellWidth: 18, halign: "center" },
      2: { cellWidth: 22, halign: "center" },
      3: { cellWidth: 38 },
      4: { cellWidth: 42 },
      5: { cellWidth: 22, halign: "center" }
    }
  });
  y = doc.lastAutoTable.finalY + 5;

  // ── Parts Received Belonging to Another Dealership (bold + underlined) ──
  doc.setFontSize(14); doc.setFont("helvetica", "bold");
  const wdTitle = "Parts Received Belonging to Another Dealership:";
  doc.text(wdTitle, ml, y);
  const wdtw = doc.getTextWidth(wdTitle);
  doc.setLineWidth(0.3); doc.line(ml, y + 0.7, ml + wdtw, y + 0.7);
  y += 2;

  // ── Wrong dealer table ──
  doc.autoTable({
    startY: y, ...tOpts,
    head: [["Part #", "Dealer Code", "SO #", "Did you contact\nthe dealer", "Do you require us\nto redirect the\npart?"]],
    headStyles: { ...tOpts.headStyles, fontSize: 8, cellPadding: 1.2 },
    body: [
      ...wrongDealerItems.map(item => [item.partNumber, item.dealerCode, item.shippingOrder, "", ""]),
      ...Array(Math.max(0, 2 - wrongDealerItems.length)).fill(["", "", "", "", ""])
    ],
    columnStyles: {
      0: { cellWidth: 30, halign: "center" },
      1: { cellWidth: 34, halign: "center" },
      2: { cellWidth: 30, halign: "center" },
      3: { cellWidth: 38, halign: "center" },
      4: { cellWidth: 38, halign: "center" }
    }
  });
  y = doc.lastAutoTable.finalY + 6;

  // ── Footer (always at bottom) ──
  doc.setFontSize(11); doc.setFont("helvetica", "bold");
  const footY = Math.max(y + 4, ph - 24);
  doc.text("RETURN BY FAX TO (519) 421-4766 or EMAIL wdk.courtesy@gm.com", pw / 2, footY, { align: "center" });
  doc.text("Dealer Fax Desk Phone (519) 421-4728", pw / 2, footY + 5, { align: "center" });

  return doc;
}

// ─── EML ─────────────────────────────────────────────────────────
function buildEML({ to, cc, subject, bodyText, pdfBase64, pdfFilename }) {
  const b = "----=_Part_" + Date.now().toString(36);
  let e = `From: \r\nTo: ${to}\r\n${cc ? `CC: ${cc}\r\n` : ""}Subject: ${subject}\r\nX-Unsent: 1\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary="${b}"\r\n\r\n--${b}\r\nContent-Type: text/plain; charset="UTF-8"\r\nContent-Transfer-Encoding: 7bit\r\n\r\n${bodyText.replace(/\n/g, "\r\n")}\r\n\r\n--${b}\r\nContent-Type: application/pdf; name="${pdfFilename}"\r\nContent-Transfer-Encoding: base64\r\nContent-Disposition: attachment; filename="${pdfFilename}"\r\n\r\n`;
  for (let i = 0; i < pdfBase64.length; i += 76) e += pdfBase64.substring(i, i + 76) + "\r\n";
  return e + `\r\n--${b}--\r\n`;
}
function buildSubjectParts(si, di, wi) { const p = []; if (di.length) p.push("DIPP"); if (si.length) p.push("Did Not Receive"); if (wi.length) p.push("Parts for Another Dealer"); return p; }
function buildSubject(parts, dc, po) { return `${parts.length ? parts.join(" / ") : "Woodstock Form"} - ${dc}${po ? ` - ${po.gmControl || po.pbsPO}` : ""}`; }

// ─── Theme ───────────────────────────────────────────────────────
function makeTheme(m) {
  const d = m === "dark";
  return { bg0: d ? "#09090b" : "#f4f4f5", bg1: d ? "#0c0c0e" : "#ffffff", bg2: d ? "#111113" : "#ffffff", bg3: d ? "#18181b" : "#f4f4f5", bgInput: d ? "#09090b" : "#ffffff", border: d ? "#27272a" : "#d4d4d8", borderLight: d ? "#18181b" : "#e4e4e7", text: d ? "#e4e4e7" : "#18181b", textStrong: d ? "#fafafa" : "#09090b", textMuted: d ? "#71717a" : "#71717a", textFaint: d ? "#52525b" : "#a1a1aa", accent: "#dc2626", accentBg: d ? "#dc262620" : "#dc262610", accentText: d ? "#fca5a5" : "#dc2626", green: "#22c55e", greenText: d ? "#4ade80" : "#16a34a", greenBg: d ? "#0a2e1a" : "#f0fdf4", red: "#ef4444", redText: d ? "#f87171" : "#dc2626", redBg: d ? "#2e0a0a" : "#fef2f2", yellow: "#f59e0b", yellowText: d ? "#fbbf24" : "#d97706", yellowBg: d ? "#2e1a0a" : "#fffbeb", purple: "#a855f7", purpleText: d ? "#c084fc" : "#7c3aed", purpleBg: d ? "#1a0a2e" : "#faf5ff", blue: "#3b82f6", blueText: d ? "#60a5fa" : "#2563eb", blueBg: d ? "#0a1a2e" : "#eff6ff", shadow: d ? "none" : "0 1px 3px rgba(0,0,0,0.06)" };
}

const DIPP_PRESETS = ["Box damaged", "Open package", "Water damage", "Crushed box", "Torn packaging", "Missing label", "Carrier damage", "Non-returnable"];
const STATUS_CFG = { match: { label: "MATCH" }, overage: { label: "OVERAGE" }, short: { label: "SHORT" } };

// ═══════════════════════════════════════════════════════════════════
export default function App() {
  const [appMode, setAppMode] = useState(null);
  const [settings, setSettings] = useState(() => loadSettings());
  const [showSettings, setShowSettings] = useState(false);
  const [settingsTab, setSettingsTab] = useState("general");
  const [settingsDraft, setSettingsDraft] = useState(DEFAULTS);
  const [newDC, setNewDC] = useState(""); const [newDN, setNewDN] = useState(""); const [newDCo, setNewDCo] = useState(""); const [newDE, setNewDE] = useState("");
  const t = makeTheme(settings.theme);
  const ff = "'JetBrains Mono','Fira Code','SF Mono','Consolas',monospace";

  const [scannedItems, setScannedItems] = useState(() => loadScans());
  const [pendingUS, setPendingUS] = useState(null);
  const [lastFeedback, setLastFeedback] = useState(null);
  const [wrongDealerPopup, setWrongDealerPopup] = useState(null);
  const [qtyEditId, setQtyEditId] = useState(null);
  const [qtyEditVal, setQtyEditVal] = useState("");
  const feedbackTimer = useRef(null);

  const [wsTab, setWsTab] = useState("scan");
  const [purchaseOrders, setPurchaseOrders] = useState([]);
  const [activePO, setActivePO] = useState(null);
  const [selectedShipment, setSelectedShipment] = useState("all");
  const [dippComments, setDippComments] = useState({});
  const [dippDescriptions, setDippDescriptions] = useState({});
  const [completedBy, setCompletedBy] = useState("");
  const [formDate, setFormDate] = useState(new Date().toISOString().split("T")[0]);
  const [pdfGenerating, setPdfGenerating] = useState(false);
  const [lastPdfBase64, setLastPdfBase64] = useState(null);
  const [lastPdfBlob, setLastPdfBlob] = useState(null);
  const [lastPdfName, setLastPdfName] = useState("");
  const [scanInput, setScanInput] = useState("");
  const [csvText, setCsvText] = useState(""); const [csvPO, setCsvPO] = useState("");
  const scanRef = useRef(null);
  const fileInputRef = useRef(null);

  // Persist settings + scans
  useEffect(() => { saveSettingsToStorage(settings); }, [settings]);
  useEffect(() => { saveScans(scannedItems); }, [scannedItems]);

  // ─── Scanner→Workstation Sync ───────────────────────────────
  const syncVersion = useRef(0);
  const syncPushTimer = useRef(null);

  // Scanner mode: push scans to server on every change (debounced)
  useEffect(() => {
    if (appMode !== "scanner" || scannedItems.length === 0) return;
    if (syncPushTimer.current) clearTimeout(syncPushTimer.current);
    syncPushTimer.current = setTimeout(() => {
      fetch("/api/scans", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ scans: scannedItems }) }).catch(() => {});
    }, 500);
    return () => { if (syncPushTimer.current) clearTimeout(syncPushTimer.current); };
  }, [scannedItems, appMode]);

  // Workstation mode: poll server for scanner updates every 2s
  useEffect(() => {
    if (appMode !== "workstation") return;
    const poll = setInterval(async () => {
      try {
        const r = await fetch("/api/version");
        const d = await r.json();
        if (d.version > syncVersion.current) {
          syncVersion.current = d.version;
          const r2 = await fetch("/api/scans");
          const d2 = await r2.json();
          if (d2.scans && d2.scans.length > 0) {
            setScannedItems(prev => {
              const existingIds = new Set(prev.map(i => i.id));
              const newItems = d2.scans.filter(s => !existingIds.has(s.id));
              if (newItems.length === 0) return prev;
              return [...prev, ...newItems];
            });
          }
        }
      } catch (e) {}
    }, 2000);
    return () => clearInterval(poll);
  }, [appMode]);

  const showFB = useCallback((msg, color, duration = 4000) => {
    if (feedbackTimer.current) clearTimeout(feedbackTimer.current);
    setLastFeedback({ msg, color });
    feedbackTimer.current = setTimeout(() => setLastFeedback(null), duration);
  }, []);

  const lookupDealer = (code) => settings.knownDealers.find(d => d.code === code) || null;
  const openSettings = () => { setSettingsDraft(JSON.parse(JSON.stringify(settings))); setShowSettings(true); setSettingsTab("general"); };
  const saveSettingsHandler = () => { setSettings(JSON.parse(JSON.stringify(settingsDraft))); setShowSettings(false); };
  const addKnownDealer = () => { const c = newDC.trim(), n = newDN.trim(), co = newDCo.trim(), em = newDE.trim(); if (!c || !n) return; const entry = { code: c, name: n, contact: co, email: em }; setSettingsDraft(p => ({ ...p, knownDealers: p.knownDealers.find(d => d.code === c) ? p.knownDealers.map(d => d.code === c ? entry : d) : [...p.knownDealers, entry] })); setNewDC(""); setNewDN(""); setNewDCo(""); setNewDE(""); };
  const removeKnownDealer = (code) => setSettingsDraft(p => ({ ...p, knownDealers: p.knownDealers.filter(d => d.code !== code) }));

  // ─── Scan Engine ───────────────────────────────────────────
  const processScan = useCallback((rawInput) => {
    const val = rawInput.trim(); if (!val) return;
    const dc = settings.dealerCode;
    const cls = classifyScan(val);

    if (cls === "quantity") {
      setScannedItems(prev => { if (!prev.length) return prev; const u = [...prev]; u[u.length - 1] = { ...u[u.length - 1], quantity: parseInt(val, 10) }; return u; });
      showFB(`QTY → ${val}`, t.yellow); return;
    }
    if (cls === "canadian") {
      const p = parseCanadianBarcode(val, dc);
      if (p) { addItem(p); if (p.wrongDealer) { const dn = lookupDealer(p.dealerCode); setWrongDealerPopup({ ...p, dealerName: dn?.name || null }); showFB(`⚠ WRONG DEALER: ${p.dealerCode}${dn ? ` (${dn.name})` : ""} — ${p.partNumber}`, t.purple, 8000); } else showFB(`✓ ${p.partNumber}  SO:${p.shippingOrder}  PDC:${p.pdc}`, t.green); setPendingUS(null); return; }
    }
    if (cls === "us_full") {
      const p = parseUSBarcode(val, dc);
      if (p) { addItem(p); if (p.wrongDealer) { const dn = lookupDealer(p.dealerCode); setWrongDealerPopup({ ...p, dealerName: dn?.name || null }); showFB(`⚠ WRONG DEALER: ${p.dealerCode} — ${p.partNumber}`, t.purple, 8000); } else showFB(`✓ US: ${p.partNumber}  SO:${p.shippingOrder}`, t.green); setPendingUS(null); return; }
    }
    if (cls === "us_header_old" || cls === "us_header_new") {
      const headerVal = cls === "us_header_new" ? val.substring(0, 10) : val;
      if (pendingUS && pendingUS.type === "part") {
        const combined = headerVal + dc + pendingUS.value;
        const p = parseUSBarcode(combined, dc); setPendingUS(null);
        if (p) { addItem(p); if (p.wrongDealer) { const dn = lookupDealer(p.dealerCode); setWrongDealerPopup({ ...p, dealerName: dn?.name || null }); showFB(`⚠ WRONG DEALER — ${p.partNumber}`, t.purple, 8000); } else showFB(`✓ US: ${p.partNumber}  SO:${p.shippingOrder}`, t.green); } else showFB(`✗ Could not parse US barcode`, t.red);
        return;
      }
      setPendingUS({ type: "header", value: headerVal, time: Date.now() });
      showFB(`⏳ US header scanned — now scan part label (either order works)`, t.yellow); return;
    }
    if (cls === "us_part") {
      if (pendingUS && pendingUS.type === "header") {
        const combined = pendingUS.value + dc + val;
        const p = parseUSBarcode(combined, dc); setPendingUS(null);
        if (p) { addItem(p); if (p.wrongDealer) { const dn = lookupDealer(p.dealerCode); setWrongDealerPopup({ ...p, dealerName: dn?.name || null }); showFB(`⚠ WRONG DEALER — ${p.partNumber}`, t.purple, 8000); } else showFB(`✓ US: ${p.partNumber}  SO:${p.shippingOrder}`, t.green); } else showFB(`✗ Could not parse US barcode`, t.red);
        return;
      }
      setPendingUS({ type: "part", value: val, time: Date.now() });
      showFB(`⏳ Part # scanned — now scan shipping header`, t.yellow); return;
    }
    if (cls === "incomplete_canadian") { showFB(`⚠ INCOMPLETE — Expected 34-35 chars, got ${val.length}. Rescan.`, t.red, 6000); setPendingUS(null); return; }
    if (cls === "incomplete_short") { showFB(`⚠ TOO SHORT (${val.length} chars) — Incomplete? Rescan.`, t.red, 6000); return; }
    if (cls === "too_long") { showFB(`⚠ TOO LONG (${val.length} chars) — Double scan? Rescan one label.`, t.red, 6000); setPendingUS(null); return; }
    showFB(`? Unknown format (${val.length} chars). Check label.`, t.red, 6000);
  }, [pendingUS, settings, showFB, t]);

  const addItem = (parsed) => {
    setScannedItems(prev => {
      const ex = prev.find(i => i.partNumber === parsed.partNumber && i.shippingOrder === parsed.shippingOrder);
      if (ex) return prev.map(i => i.partNumber === parsed.partNumber && i.shippingOrder === parsed.shippingOrder ? { ...i, quantity: i.quantity + 1, scans: [...i.scans, parsed.raw] } : i);
      return [...prev, { ...parsed, quantity: 1, scans: [parsed.raw], id: Date.now() + Math.random(), dipp: false }];
    });
  };

  const handleScanKeyDown = (e) => { if (e.key === "Enter" || e.key === "Tab") { e.preventDefault(); processScan(scanInput); setScanInput(""); } };
  const toggleDipp = (id) => setScannedItems(prev => prev.map(i => i.id === id ? { ...i, dipp: !i.dipp } : i));
  const delScan = (id) => setScannedItems(prev => prev.filter(i => i.id !== id));
  const adjQty = (id, d) => setScannedItems(prev => prev.map(i => i.id === id ? { ...i, quantity: Math.max(1, i.quantity + d) } : i));
  const setQty = (id, q) => { const n = parseInt(q, 10); if (n >= 1) setScannedItems(prev => prev.map(i => i.id === id ? { ...i, quantity: n } : i)); };
  const getWD = () => scannedItems.filter(i => i.wrongDealer);
  const getDipp = () => scannedItems.filter(i => i.dipp);
  const stats = { total: scannedItems.reduce((s, i) => s + i.quantity, 0), unique: scannedItems.length, wd: getWD().length, dipp: getDipp().length, so: [...new Set(scannedItems.map(i => i.shippingOrder))].length };

  // ─── PWB+ / Comparison ────────────────────────────────────
  const handleFileUpload = async (e) => { for (const file of Array.from(e.target.files)) { try { const raw = await parseXLSXFile(file), info = parseFilename(file.name), norm = normPWB(raw); const po = { id: Date.now() + Math.random(), ...info, filename: file.name, data: norm }; setPurchaseOrders(prev => { const ex = prev.find(p => p.pbsPO === po.pbsPO && p.gmControl === po.gmControl); return ex ? prev.map(p => p.pbsPO === po.pbsPO && p.gmControl === po.gmControl ? po : p) : [...prev, po]; }); if (!activePO) setActivePO(info.pbsPO); showFB(`✓ PO ${info.pbsPO} — ${norm.length} lines`, t.green); } catch (err) { showFB(`Error: ${err.message}`, t.red); } } if (fileInputRef.current) fileInputRef.current.value = ""; };
  const handleCSVPaste = () => { const raw = parseCSVText(csvText); if (!raw.length) { showFB("No data", t.red); return; } const norm = normPWB(raw), name = csvPO.trim() || `paste-${Date.now()}`; setPurchaseOrders(prev => [...prev, { id: Date.now(), pbsPO: name, gmControl: "", dateStr: "", filename: "pasted", data: norm }]); if (!activePO) setActivePO(name); setCsvText(""); setCsvPO(""); showFB(`✓ ${norm.length} lines`, t.green); };
  const normPWB = (data) => data.map(row => ({ status: row["Current Status"] || "", partOrdered: String(row["Part No. Ordered"] || "").replace(/\s/g, ""), partProcessed: String(row["Part No. Processed"] || "").replace(/\s/g, ""), facility: row["Facility"] || "", qtyOrdered: parseInt(row["Qty Ordered"] || 0), qtyProc: parseInt(row["Qty Proc."] || 0), shipmentNo: String(row["Shipment No."] || "").replace(/\s/g, ""), superseded: String(row["Part No. Ordered"] || "").replace(/\s/g, "") !== String(row["Part No. Processed"] || "").replace(/\s/g, "") }));
  const removePO = (id) => { setPurchaseOrders(prev => { const po = prev.find(p => p.id === id), next = prev.filter(p => p.id !== id); if (po && activePO === po.pbsPO) setActivePO(next.length ? next[0].pbsPO : null); return next; }); };
  const getPWB = () => { if (activePO === "__all__") return purchaseOrders.flatMap(p => p.data); return (purchaseOrders.find(p => p.pbsPO === activePO) || {}).data || []; };
  const getShipNums = () => { const n = new Set(); getPWB().forEach(r => { if (r.shipmentNo) n.add(r.shipmentNo); }); scannedItems.forEach(r => { if (r.shippingOrder) n.add(r.shippingOrder); }); return [...n].sort(); };
  const getComp = () => {
    const shipped = getPWB().filter(r => r.status === "Shipped" && (selectedShipment === "all" || r.shipmentNo === selectedShipment)); const results = [], matched = new Set();
    shipped.forEach(pwb => { const part = pwb.partProcessed || pwb.partOrdered; const scan = scannedItems.find(s => s.partNumber === part && (selectedShipment === "all" || s.shippingOrder === pwb.shipmentNo)); if (scan) { matched.add(scan.id); const diff = scan.quantity - pwb.qtyProc; results.push({ partNumber: part, partOrdered: pwb.partOrdered, superseded: pwb.superseded, expectedQty: pwb.qtyProc, scannedQty: scan.quantity, qtyDiff: diff, shippingOrder: pwb.shipmentNo, facility: pwb.facility, status: diff === 0 ? "match" : diff > 0 ? "overage" : "short", wrongDealer: scan.wrongDealer, dealerCode: scan.dealerCode, dipp: scan.dipp }); } else results.push({ partNumber: part, partOrdered: pwb.partOrdered, superseded: pwb.superseded, expectedQty: pwb.qtyProc, scannedQty: 0, qtyDiff: -pwb.qtyProc, shippingOrder: pwb.shipmentNo, facility: pwb.facility, status: "short", wrongDealer: false }); });
    scannedItems.forEach(scan => { if (!matched.has(scan.id) && (selectedShipment === "all" || scan.shippingOrder === selectedShipment)) results.push({ partNumber: scan.partNumber, partOrdered: scan.partNumber, superseded: false, expectedQty: 0, scannedQty: scan.quantity, qtyDiff: scan.quantity, shippingOrder: scan.shippingOrder, facility: scan.pdc, status: "overage", wrongDealer: scan.wrongDealer, dealerCode: scan.dealerCode, dipp: scan.dipp }); });
    return results.sort((a, b) => ({ short: 0, overage: 1, match: 2 }[a.status] ?? 3) - ({ short: 0, overage: 1, match: 2 }[b.status] ?? 3) || a.partNumber.localeCompare(b.partNumber));
  };
  const comp = purchaseOrders.length > 0 ? getComp() : [];
  const nM = comp.filter(r => r.status === "match").length, nS = comp.filter(r => r.status === "short").length, nO = comp.filter(r => r.status === "overage").length;
  const shortItems = comp.filter(r => r.status === "short" && r.expectedQty > 0);
  const poInfo = purchaseOrders.find(p => p.pbsPO === activePO) || null;
  const subParts = buildSubjectParts(shortItems, getDipp(), getWD());
  const emailSubject = buildSubject(subParts, settings.dealerCode, poInfo);
  const getCCStr = () => { const codes = [...new Set(getWD().map(i => i.dealerCode))]; return codes.map(c => lookupDealer(c)).filter(d => d?.email).map(d => `${d.contact ? `"${d.contact}" ` : ""}<${d.email}>`).join(", "); };

  const generatePDFHandler = async () => { setPdfGenerating(true); try { const doc = generateWoodstockPDF({ settings, shortItems, dippItems: getDipp(), dippComments, dippDescriptions, wrongDealerItems: getWD(), completedBy, formDate, poInfo }); const fn = `woodstock_form_${poInfo ? `${poInfo.pbsPO}_${poInfo.gmControl}` : "form"}_${formDate}.pdf`; setLastPdfBlob(doc.output("blob")); setLastPdfBase64(doc.output("datauristring").split(",")[1]); setLastPdfName(fn); doc.save(fn); showFB("✓ PDF generated", t.green); } catch (err) { showFB(`PDF error: ${err.message}`, t.red); } setPdfGenerating(false); };
  const printPDF = () => { if (!lastPdfBlob) return; const w = window.open(URL.createObjectURL(lastPdfBlob)); if (w) w.addEventListener("load", () => setTimeout(() => w.print(), 500)); };
  const emailOutlook = () => { if (!lastPdfBase64) return; const body = `Please find the attached Woodstock form for dealer ${settings.dealerCode} (${settings.dealerName}).\n\nDate: ${formDate}\nCompleted by: ${completedBy || "(not specified)"}\nPhone: ${settings.phone}${poInfo ? `\nPO: ${poInfo.pbsPO} / GM Control: ${poInfo.gmControl}` : ""}\n\nSummary:\n${shortItems.length ? `- ${shortItems.length} short\n` : ""}${getDipp().length ? `- ${getDipp().length} DIPP\n` : ""}${getWD().length ? `- ${getWD().length} wrong dealer\n` : ""}`; const eml = buildEML({ to: settings.wdkEmail, cc: getCCStr(), subject: emailSubject, bodyText: body, pdfBase64: lastPdfBase64, pdfFilename: lastPdfName }); const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([eml], { type: "message/rfc822" })); a.download = `woodstock_${formDate}.eml`; document.body.appendChild(a); a.click(); document.body.removeChild(a); showFB("✓ .eml downloaded — open in Outlook", t.green); };

  // ─── Styles ─────────────────────────────────────────────────
  const S = {
    overlay: { position: "fixed", inset: 0, background: "rgba(0,0,0,0.5)", zIndex: 200, display: "flex", alignItems: "center", justifyContent: "center", padding: 16 },
    modal: { background: t.bg2, border: `1px solid ${t.border}`, borderRadius: 12, width: 540, maxWidth: "100%", maxHeight: "90vh", boxShadow: "0 20px 60px rgba(0,0,0,0.3)", overflow: "auto" },
    btn: (b, c) => ({ padding: "5px 12px", background: b, color: c, border: "none", borderRadius: 4, cursor: "pointer", fontFamily: ff, fontSize: 11, fontWeight: 600 }),
    sm: (b, c) => ({ padding: "2px 7px", background: b, color: c, border: "none", borderRadius: 3, cursor: "pointer", fontFamily: ff, fontSize: 10, fontWeight: 600 }),
    bg: (b, c) => ({ display: "inline-block", padding: "2px 7px", borderRadius: 3, fontSize: 9, fontWeight: 700, background: b, color: c }),
    inp: { padding: "5px 9px", background: t.bgInput, border: `1px solid ${t.border}`, borderRadius: 4, color: t.text, fontFamily: ff, fontSize: 12, boxSizing: "border-box" },
    sel: { padding: "5px 9px", background: t.bgInput, border: `1px solid ${t.border}`, borderRadius: 4, color: t.text, fontFamily: ff, fontSize: 12 },
    tbl: { width: "100%", borderCollapse: "collapse" },
    th: { padding: "6px 10px", textAlign: "left", fontSize: 9, fontWeight: 700, color: t.textMuted, letterSpacing: 1.2, textTransform: "uppercase", borderBottom: `1px solid ${t.border}`, background: t.bg1 },
    td: c => ({ padding: "6px 10px", borderBottom: `1px solid ${t.borderLight}`, fontSize: 12, color: c || t.text }),
    card: { background: t.bg2, border: `1px solid ${t.border}`, borderRadius: 8, marginBottom: 10, overflow: "hidden", boxShadow: t.shadow },
    cH: { padding: "9px 12px", borderBottom: `1px solid ${t.border}`, display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 8 },
    cL: { fontSize: 10, fontWeight: 700, color: t.textMuted, letterSpacing: 1.2, textTransform: "uppercase" },
    lbl: { fontSize: 10, color: t.textMuted, marginBottom: 3, display: "block", letterSpacing: .5, textTransform: "uppercase", fontWeight: 600 },
    dot: c => ({ display: "inline-block", width: 7, height: 7, borderRadius: "50%", background: c }),
    mI: { width: "100%", padding: "8px 10px", background: t.bgInput, border: `1px solid ${t.border}`, borderRadius: 4, color: t.text, fontFamily: ff, fontSize: 13, boxSizing: "border-box" },
    stab: on => ({ padding: "6px 14px", border: "none", borderBottom: on ? `2px solid ${t.accent}` : "2px solid transparent", background: "transparent", color: on ? t.textStrong : t.textMuted, fontFamily: ff, fontSize: 11, fontWeight: on ? 600 : 400, cursor: "pointer" }),
    pill: c => ({ padding: "3px 8px", borderRadius: 4, fontSize: 10, fontWeight: 600, color: c, background: `${c}10`, border: `1px solid ${c}20` }),
    empty: { padding: "32px 16px", textAlign: "center", color: t.textFaint, fontSize: 12 },
    fb: c => ({ padding: "8px 12px", background: `${c}10`, borderLeft: `3px solid ${c}`, color: c, fontSize: 12, fontWeight: 600, marginTop: 6, borderRadius: "0 4px 4px 0" }),
  };
  const sc = (s) => ({ match: { bg: `${t.green}20`, tx: t.greenText, row: t.greenBg }, overage: { bg: `${t.yellow}20`, tx: t.yellowText, row: t.yellowBg }, short: { bg: `${t.red}20`, tx: t.redText, row: t.redBg } }[s] || { bg: t.bg3, tx: t.textMuted, row: "transparent" });
  const Bdg = ({ n, c }) => n > 0 ? <span style={{ padding: "1px 5px", borderRadius: 7, fontSize: 9, fontWeight: 700, background: c, color: "#fff", minWidth: 14, textAlign: "center", display: "inline-block" }}>{n}</span> : null;

  // ═══ Settings Modal ════════════════════════════════════════
  const settingsModal = showSettings && (
    <div style={S.overlay} onClick={() => setShowSettings(false)}>
      <div style={S.modal} onClick={e => e.stopPropagation()}>
        <div style={{ padding: "14px 16px", borderBottom: `1px solid ${t.border}`, display: "flex", justifyContent: "space-between", position: "sticky", top: 0, background: t.bg2, zIndex: 1 }}><span style={{ fontWeight: 700, fontSize: 14, color: t.textStrong }}>Settings</span><button style={S.sm(t.bg3, t.textMuted)} onClick={() => setShowSettings(false)}>✕</button></div>
        <div style={{ display: "flex", borderBottom: `1px solid ${t.border}` }}><button style={S.stab(settingsTab === "general")} onClick={() => setSettingsTab("general")}>General</button><button style={S.stab(settingsTab === "dealers")} onClick={() => setSettingsTab("dealers")}>Dealers</button></div>
        <div style={{ padding: 16 }}>
          {settingsTab === "general" && (<>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Dealer Code</label><input style={S.mI} value={settingsDraft.dealerCode} onChange={e => setSettingsDraft(p => ({ ...p, dealerCode: e.target.value }))} /></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Dealer Name</label><input style={S.mI} value={settingsDraft.dealerName} onChange={e => setSettingsDraft(p => ({ ...p, dealerName: e.target.value }))} /></div>
            <div style={{ display: "flex", gap: 12 }}><div style={{ flex: 1, marginBottom: 14 }}><label style={S.lbl}>Area</label><input style={S.mI} value={settingsDraft.area} onChange={e => setSettingsDraft(p => ({ ...p, area: e.target.value }))} /></div><div style={{ flex: 1, marginBottom: 14 }}><label style={S.lbl}>Station</label><input style={S.mI} value={settingsDraft.station} onChange={e => setSettingsDraft(p => ({ ...p, station: e.target.value }))} /></div></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Phone</label><input style={S.mI} value={settingsDraft.phone} onChange={e => setSettingsDraft(p => ({ ...p, phone: e.target.value }))} /></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Woodstock Email</label><input style={S.mI} value={settingsDraft.wdkEmail} onChange={e => setSettingsDraft(p => ({ ...p, wdkEmail: e.target.value }))} /></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Theme</label><div style={{ display: "flex", gap: 8, marginTop: 4 }}>{["light", "dark"].map(m => <button key={m} onClick={() => setSettingsDraft(p => ({ ...p, theme: m }))} style={{ padding: "8px 20px", borderRadius: 6, border: `2px solid ${settingsDraft.theme === m ? t.accent : t.border}`, background: m === "dark" ? "#18181b" : "#f8f8fa", color: m === "dark" ? "#e4e4e7" : "#18181b", fontFamily: ff, fontSize: 12, fontWeight: settingsDraft.theme === m ? 700 : 400, cursor: "pointer" }}>{m === "light" ? "☀ Light" : "🌙 Dark"}</button>)}</div></div>
          </>)}
          {settingsTab === "dealers" && (<>
            <div style={{ marginBottom: 12, fontSize: 11, color: t.textMuted }}>Dealer directory for wrong-dealer ID and email CC.</div>
            <div style={{ background: t.bg0, border: `1px solid ${t.border}`, borderRadius: 6, padding: 10, marginBottom: 12 }}>
              <div style={{ display: "flex", gap: 6, marginBottom: 6, flexWrap: "wrap" }}><input style={{ ...S.inp, width: 80 }} placeholder="Code" value={newDC} onChange={e => setNewDC(e.target.value)} /><input style={{ ...S.inp, flex: 1, minWidth: 100 }} placeholder="Dealer Name" value={newDN} onChange={e => setNewDN(e.target.value)} /></div>
              <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}><input style={{ ...S.inp, flex: 1 }} placeholder="Contact" value={newDCo} onChange={e => setNewDCo(e.target.value)} /><input style={{ ...S.inp, flex: 1 }} placeholder="Email" value={newDE} onChange={e => setNewDE(e.target.value)} onKeyDown={e => { if (e.key === "Enter") addKnownDealer(); }} /><button style={S.btn(t.accent, "#fff")} onClick={addKnownDealer}>Add</button></div>
            </div>
            {settingsDraft.knownDealers.length === 0 ? <div style={S.empty}>Empty</div> :
              <table style={S.tbl}><thead><tr><th style={S.th}>Code</th><th style={S.th}>Dealer</th><th style={S.th}>Contact</th><th style={S.th}>Email</th><th style={S.th}></th></tr></thead><tbody>
                {settingsDraft.knownDealers.map(d => <tr key={d.code}><td style={S.td(t.textStrong)}><strong>{d.code}</strong>{d.code === settingsDraft.dealerCode && <span style={{ ...S.bg(`${t.green}20`, t.greenText), marginLeft: 6 }}>YOU</span>}</td><td style={S.td()}>{d.name}</td><td style={S.td()}>{d.contact || "—"}</td><td style={S.td()}>{d.email || "—"}</td><td style={S.td()}><button style={S.sm(t.bg3, t.textFaint)} onClick={() => removeKnownDealer(d.code)}>✕</button></td></tr>)}
              </tbody></table>}
          </>)}
        </div>
        <div style={{ padding: "12px 16px", borderTop: `1px solid ${t.border}`, display: "flex", justifyContent: "flex-end", gap: 8, position: "sticky", bottom: 0, background: t.bg2 }}><button style={S.btn(t.bg3, t.textMuted)} onClick={() => setShowSettings(false)}>Cancel</button><button style={S.btn(t.accent, "#fff")} onClick={saveSettingsHandler}>Save</button></div>
      </div>
    </div>
  );

  // ═══ Wrong Dealer Popup ════════════════════════════════════
  const wdPopup = wrongDealerPopup && (
    <div style={S.overlay} onClick={() => setWrongDealerPopup(null)}>
      <div onClick={e => e.stopPropagation()} style={{ background: t.bg2, border: `3px solid ${t.purple}`, borderRadius: 16, width: 400, maxWidth: "90vw", boxShadow: `0 0 40px ${t.purple}40`, overflow: "hidden" }}>
        <div style={{ background: `${t.purple}15`, padding: "16px 20px", textAlign: "center" }}><div style={{ fontSize: 32 }}>⚠</div><div style={{ fontSize: 18, fontWeight: 900, color: t.purpleText, fontFamily: ff }}>WRONG DEALER</div></div>
        <div style={{ padding: "16px 20px", textAlign: "center" }}>
          <div style={{ fontSize: 28, fontWeight: 900, color: t.purpleText, fontFamily: ff, marginBottom: 8 }}>{wrongDealerPopup.dealerCode}</div>
          {wrongDealerPopup.dealerName ? <div style={{ fontSize: 16, fontWeight: 600, color: t.text, marginBottom: 12 }}>{wrongDealerPopup.dealerName}</div> : <div style={{ fontSize: 13, color: t.textFaint, marginBottom: 12, fontStyle: "italic" }}>Unknown dealer — add in Settings</div>}
          <div style={{ fontSize: 13, color: t.textMuted }}>Part: <strong style={{ color: t.textStrong }}>{wrongDealerPopup.partNumber}</strong></div>
          <div style={{ fontSize: 13, color: t.textMuted }}>SO: <strong>{wrongDealerPopup.shippingOrder}</strong> &nbsp; PDC: <strong>{wrongDealerPopup.pdc}</strong></div>
          <div style={{ fontSize: 11, color: t.textFaint, marginTop: 8 }}>Expected: {settings.dealerCode} ({settings.dealerName})</div>
        </div>
        <div style={{ padding: "12px 20px", borderTop: `1px solid ${t.border}`, display: "flex", justifyContent: "center" }}><button style={{ padding: "10px 24px", background: t.purple, color: "#fff", border: "none", borderRadius: 8, fontFamily: ff, fontSize: 14, fontWeight: 700, cursor: "pointer" }} onClick={() => setWrongDealerPopup(null)}>OK — Continue</button></div>
      </div>
    </div>
  );

  // ═══ MODE CHOOSER ══════════════════════════════════════════
  if (appMode === null) {
    return (
      <div style={{ fontFamily: ff, background: t.bg0, color: t.text, minHeight: "100vh", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 24, padding: 24 }}>
        {settingsModal}
        <div style={{ width: 40, height: 40, background: "linear-gradient(135deg,#dc2626,#991b1b)", borderRadius: 8, display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 900, fontSize: 16, color: "#fff" }}>GM</div>
        <div style={{ textAlign: "center" }}><div style={{ fontSize: 18, fontWeight: 700, color: t.textStrong }}>Parts Receiving</div><div style={{ fontSize: 11, color: t.textMuted, marginTop: 4 }}>{settings.dealerName} · {settings.dealerCode}</div></div>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap", justifyContent: "center" }}>
          {[["scanner", "📱", "Scanner", "Handheld / Datalogic", "Scan, flag, count"], ["workstation", "🖥", "Workstation", "Full desktop", "Compare, forms, email"]].map(([mode, icon, title, sub1, sub2]) => (
            <button key={mode} onClick={() => setAppMode(mode)} style={{ padding: "24px 32px", background: t.bg2, border: `2px solid ${t.border}`, borderRadius: 12, cursor: "pointer", fontFamily: ff, textAlign: "center", minWidth: 180, boxShadow: t.shadow }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>{icon}</div><div style={{ fontSize: 14, fontWeight: 700, color: t.textStrong }}>{title}</div><div style={{ fontSize: 10, color: t.textMuted, marginTop: 4 }}>{sub1}<br />{sub2}</div>
            </button>
          ))}
        </div>
        <button style={{ ...S.btn(t.bg3, t.textMuted), marginTop: 8 }} onClick={openSettings}>⚙ Settings</button>
      </div>
    );
  }

  // ═══ SCANNER MODE ══════════════════════════════════════════
  if (appMode === "scanner") {
    return (
      <div style={{ fontFamily: ff, background: t.bg0, color: t.text, minHeight: "100vh", fontSize: 13 }}>
        {settingsModal}{wdPopup}
        {qtyEditId && <div style={S.overlay} onClick={() => setQtyEditId(null)}><div onClick={e => e.stopPropagation()} style={{ background: t.bg2, border: `1px solid ${t.border}`, borderRadius: 12, padding: 20, width: 280, textAlign: "center" }}><div style={{ fontSize: 12, color: t.textMuted, marginBottom: 8, fontWeight: 600 }}>SET QUANTITY</div><input autoFocus type="number" min="1" max="999" value={qtyEditVal} onChange={e => setQtyEditVal(e.target.value)} onKeyDown={e => { if (e.key === "Enter") { setQty(qtyEditId, qtyEditVal); setQtyEditId(null); } }} style={{ width: 100, padding: 12, background: t.bgInput, border: `2px solid ${t.accent}`, borderRadius: 8, color: t.textStrong, fontFamily: ff, fontSize: 28, textAlign: "center", outline: "none" }} /><div style={{ display: "flex", gap: 8, marginTop: 12, justifyContent: "center" }}><button style={{ ...S.btn(t.bg3, t.textMuted), padding: "8px 20px" }} onClick={() => setQtyEditId(null)}>Cancel</button><button style={{ ...S.btn(t.accent, "#fff"), padding: "8px 20px" }} onClick={() => { setQty(qtyEditId, qtyEditVal); setQtyEditId(null); }}>Set</button></div></div></div>}
        <div style={{ background: t.bg1, borderBottom: `1px solid ${t.border}`, padding: "8px 12px", display: "flex", alignItems: "center", justifyContent: "space-between", position: "sticky", top: 0, zIndex: 100 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}><button style={S.sm(t.bg3, t.textMuted)} onClick={() => setAppMode(null)}>←</button><span style={{ fontWeight: 700, fontSize: 13, color: t.textStrong }}>Scanner</span></div>
          <div style={{ display: "flex", gap: 4 }}><span style={S.pill(t.text)}>{stats.total}</span>{stats.wd > 0 && <span style={S.pill(t.purpleText)}>⚠{stats.wd}</span>}{stats.dipp > 0 && <span style={S.pill(t.blueText)}>D{stats.dipp}</span>}<button style={{ ...S.sm(t.bg3, t.textMuted), fontSize: 14 }} onClick={openSettings}>⚙</button></div>
        </div>
        {pendingUS && <div style={{ padding: "8px 12px", background: t.yellowBg, borderBottom: `2px solid ${t.yellow}`, color: t.yellowText, fontSize: 12, fontWeight: 600, display: "flex", justifyContent: "space-between" }}><span>⏳ Waiting for US {pendingUS.type === "header" ? "part" : "header"}…</span><button style={S.sm(`${t.yellow}20`, t.yellowText)} onClick={() => setPendingUS(null)}>Cancel</button></div>}
        <div style={{ padding: 12 }}>
          <input ref={scanRef} autoFocus autoComplete="off" style={{ width: "100%", padding: 14, background: t.bgInput, border: `2px solid ${t.border}`, borderRadius: 8, color: t.textStrong, fontFamily: ff, fontSize: 16, outline: "none", boxSizing: "border-box" }} value={scanInput} onChange={e => setScanInput(e.target.value)} onKeyDown={handleScanKeyDown} onFocus={e => e.target.style.borderColor = t.accent} onBlur={e => e.target.style.borderColor = t.border} placeholder="▸ Scan barcode" />
          {lastFeedback && <div style={S.fb(lastFeedback.color)}>{lastFeedback.msg}</div>}
        </div>
        <div style={{ padding: "0 12px 12px" }}>
          {scannedItems.length === 0 ? <div style={S.empty}>Scan your first barcode.</div> :
            <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
              {[...scannedItems].reverse().map(item => { const dn = item.wrongDealer ? lookupDealer(item.dealerCode) : null; return (
                <div key={item.id} style={{ background: item.wrongDealer ? `${t.purple}08` : t.bg2, border: `1px solid ${item.wrongDealer ? t.purple + "40" : item.dipp ? t.blue + "40" : t.border}`, borderRadius: 8, padding: "10px 12px", boxShadow: t.shadow }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 6 }}>
                    <div><div style={{ fontSize: 16, fontWeight: 900, color: t.textStrong }}>{item.partNumber}</div><div style={{ fontSize: 11, color: t.textMuted }}>SO: {item.shippingOrder} · PDC: {item.pdc} · <span style={S.bg(item.type === "CA" ? `${t.green}15` : `${t.blue}15`, item.type === "CA" ? t.greenText : t.blueText)}>{item.type}</span></div>{item.wrongDealer && <div style={{ fontSize: 11, color: t.purpleText, fontWeight: 700, marginTop: 2 }}>⚠ {item.dealerCode}{dn ? ` — ${dn.name}` : ""}</div>}</div>
                    <button onClick={() => { setQtyEditId(item.id); setQtyEditVal(String(item.quantity)); }} style={{ background: t.bg3, border: `1px solid ${t.border}`, borderRadius: 6, padding: "4px 10px", cursor: "pointer", fontFamily: ff, fontSize: 18, fontWeight: 900, color: t.textStrong, minWidth: 44, textAlign: "center" }}>{item.quantity}</button>
                  </div>
                  <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                    <button style={{ padding: "6px 12px", background: item.dipp ? t.blue : t.bg3, color: item.dipp ? "#fff" : t.textMuted, border: `1px solid ${item.dipp ? t.blue : t.border}`, borderRadius: 6, fontFamily: ff, fontSize: 11, fontWeight: 600, cursor: "pointer" }} onClick={() => toggleDipp(item.id)}>{item.dipp ? "★ DIPP" : "DIPP"}</button>
                    <button style={{ padding: "6px 10px", background: t.bg3, color: t.textMuted, border: `1px solid ${t.border}`, borderRadius: 6, fontFamily: ff, fontSize: 11, cursor: "pointer" }} onClick={() => adjQty(item.id, -1)}>−</button>
                    <button style={{ padding: "6px 10px", background: t.bg3, color: t.textMuted, border: `1px solid ${t.border}`, borderRadius: 6, fontFamily: ff, fontSize: 11, cursor: "pointer" }} onClick={() => adjQty(item.id, 1)}>+</button>
                    <button style={{ padding: "6px 12px", background: t.redBg, color: t.redText, border: `1px solid ${t.red}30`, borderRadius: 6, fontFamily: ff, fontSize: 11, fontWeight: 600, cursor: "pointer", marginLeft: "auto" }} onClick={() => delScan(item.id)}>✕ Delete</button>
                  </div>
                </div>); })}
            </div>}
          {scannedItems.length > 0 && <button style={{ ...S.btn(t.redBg, t.redText), marginTop: 12, width: "100%", padding: "10px" }} onClick={() => { if (confirm("Clear ALL scans?")) { setScannedItems([]); fetch("/api/scans", { method: "DELETE" }).catch(() => {}); } }}>Clear All Scans</button>}
        </div>
      </div>
    );
  }

  // ═══ WORKSTATION MODE ══════════════════════════════════════
  return (
    <div style={{ fontFamily: ff, background: t.bg0, color: t.text, minHeight: "100vh", fontSize: 13 }}>
      {settingsModal}{wdPopup}
      {qtyEditId && <div style={S.overlay} onClick={() => setQtyEditId(null)}><div onClick={e => e.stopPropagation()} style={{ background: t.bg2, border: `1px solid ${t.border}`, borderRadius: 12, padding: 20, width: 240, textAlign: "center" }}><div style={{ fontSize: 11, color: t.textMuted, marginBottom: 8 }}>SET QUANTITY</div><input autoFocus type="number" min="1" value={qtyEditVal} onChange={e => setQtyEditVal(e.target.value)} onKeyDown={e => { if (e.key === "Enter") { setQty(qtyEditId, qtyEditVal); setQtyEditId(null); } }} style={{ width: 80, padding: 10, background: t.bgInput, border: `2px solid ${t.accent}`, borderRadius: 8, color: t.textStrong, fontFamily: ff, fontSize: 24, textAlign: "center", outline: "none" }} /><div style={{ display: "flex", gap: 8, marginTop: 12, justifyContent: "center" }}><button style={S.btn(t.bg3, t.textMuted)} onClick={() => setQtyEditId(null)}>Cancel</button><button style={S.btn(t.accent, "#fff")} onClick={() => { setQty(qtyEditId, qtyEditVal); setQtyEditId(null); }}>Set</button></div></div></div>}
      <div style={{ background: t.bg1, borderBottom: `1px solid ${t.border}`, padding: "10px 16px", display: "flex", alignItems: "center", justifyContent: "space-between", position: "sticky", top: 0, zIndex: 100, gap: 12, flexWrap: "wrap", boxShadow: t.shadow }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}><button style={S.sm(t.bg3, t.textMuted)} onClick={() => setAppMode(null)}>←</button><div style={{ width: 30, height: 30, background: "linear-gradient(135deg,#dc2626,#991b1b)", borderRadius: 5, display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 900, fontSize: 12, color: "#fff" }}>GM</div><div><div style={{ fontSize: 14, fontWeight: 700, color: t.textStrong }}>Workstation</div><div style={{ fontSize: 10, color: t.textMuted }}>{settings.dealerName} · {settings.dealerCode}</div></div></div>
        <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}><span style={S.pill(t.text)}>{stats.total} scanned</span><span style={S.pill(t.text)}>{purchaseOrders.length} PO</span>{stats.wd > 0 && <span style={S.pill(t.purpleText)}>⚠{stats.wd}</span>}{stats.dipp > 0 && <span style={S.pill(t.blueText)}>D{stats.dipp}</span>}{purchaseOrders.length > 0 && <><span style={S.pill(t.greenText)}>✓{nM}</span>{nS > 0 && <span style={S.pill(t.redText)}>{nS}S</span>}{nO > 0 && <span style={S.pill(t.yellowText)}>{nO}O</span>}</>}</div>
          <button style={{ width: 30, height: 30, borderRadius: 6, border: `1px solid ${t.border}`, background: t.bg3, display: "flex", alignItems: "center", justifyContent: "center", cursor: "pointer", fontSize: 16, color: t.textMuted }} onClick={openSettings}>⚙</button>
        </div>
      </div>
      <div style={{ display: "flex", background: t.bg1, borderBottom: `1px solid ${t.border}`, position: "sticky", top: 52, zIndex: 99, overflowX: "auto" }}>
        {[["scan", "Scan", stats.total, t.textFaint], ["compare", "Compare", nS, t.red], ["wrongdealer", "Wrong Dealer", stats.wd, t.purple], ["dipp", "DIPP", stats.dipp, t.blue], ["form", "Form / Email", 0, null]].map(([k, l, n, c]) => <button key={k} style={{ padding: "9px 16px", cursor: "pointer", border: "none", background: wsTab === k ? t.bg2 : "transparent", color: wsTab === k ? t.textStrong : t.textMuted, borderBottom: wsTab === k ? `2px solid ${t.accent}` : "2px solid transparent", fontFamily: ff, fontSize: 11, fontWeight: wsTab === k ? 600 : 400, display: "flex", alignItems: "center", gap: 6, whiteSpace: "nowrap" }} onClick={() => setWsTab(k)}>{l} {n > 0 && <Bdg n={n} c={c} />}</button>)}
      </div>
      <div style={{ padding: "14px 16px", maxWidth: 1200, margin: "0 auto" }}>
        {wsTab === "scan" && (<>
          {pendingUS && <div style={{ padding: "8px 12px", background: t.yellowBg, borderBottom: `2px solid ${t.yellow}`, color: t.yellowText, fontSize: 12, fontWeight: 600, display: "flex", justifyContent: "space-between", marginBottom: 10 }}><span>⏳ US {pendingUS.type === "header" ? "part" : "header"} pending</span><button style={S.sm(`${t.yellow}20`, t.yellowText)} onClick={() => setPendingUS(null)}>Cancel</button></div>}
          <div style={S.card}><div style={{ padding: 12 }}><input ref={scanRef} autoFocus autoComplete="off" style={{ width: "100%", padding: "12px 14px", background: t.bgInput, border: `2px solid ${t.border}`, borderRadius: 6, color: t.textStrong, fontFamily: ff, fontSize: 15, outline: "none", boxSizing: "border-box" }} value={scanInput} onChange={e => setScanInput(e.target.value)} onKeyDown={handleScanKeyDown} onFocus={e => e.target.style.borderColor = t.accent} onBlur={e => e.target.style.borderColor = t.border} placeholder="▸ Scan barcode — Enter or Tab" />{lastFeedback && <div style={S.fb(lastFeedback.color)}>{lastFeedback.msg}</div>}</div></div>
          <div style={S.card}><div style={S.cH}><span style={S.cL}>Scanned · {stats.unique} unique · {stats.total} total</span>{scannedItems.length > 0 && <button style={S.sm(t.bg3, t.textMuted)} onClick={() => { if (confirm("Clear?")) setScannedItems([]) }}>Clear</button>}</div>
            {scannedItems.length === 0 ? <div style={S.empty}>No items.</div> : <div style={{ overflowX: "auto", maxHeight: 500, overflowY: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>SO</th><th style={S.th}>PDC</th><th style={S.th}>Dealer</th><th style={S.th}>Qty</th><th style={S.th}>Flags</th><th style={S.th}></th></tr></thead><tbody>
              {[...scannedItems].reverse().map(item => { const dn = item.wrongDealer ? lookupDealer(item.dealerCode) : null; return (
                <tr key={item.id} style={{ background: item.wrongDealer ? `${t.purple}08` : "transparent" }}>
                  <td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}>{item.pdc}</td>
                  <td style={S.td(item.wrongDealer ? t.purpleText : null)}>{item.dealerCode}{item.wrongDealer && " ⚠"}{dn && <div style={{ fontSize: 9, color: t.purpleText }}>{dn.name}</div>}</td>
                  <td style={S.td(t.textStrong)}><span style={{ display: "flex", alignItems: "center", gap: 4 }}><button style={S.sm(t.bg3, t.textMuted)} onClick={() => adjQty(item.id, -1)}>−</button><button style={{ background: "transparent", border: "none", color: t.textStrong, fontFamily: ff, fontSize: 13, fontWeight: 700, cursor: "pointer", padding: "2px 4px" }} onClick={() => { setQtyEditId(item.id); setQtyEditVal(String(item.quantity)); }}>{item.quantity}</button><button style={S.sm(t.bg3, t.textMuted)} onClick={() => adjQty(item.id, 1)}>+</button></span></td>
                  <td style={S.td()}>{item.wrongDealer && <span style={{ ...S.bg(`${t.purple}15`, t.purpleText), marginRight: 3 }}>WD</span>}{item.dipp && <span style={S.bg(`${t.blue}15`, t.blueText)}>DIPP</span>}</td>
                  <td style={S.td()}><span style={{ display: "flex", gap: 4 }}><button style={S.sm(item.dipp ? `${t.blue}20` : t.bg3, item.dipp ? t.blueText : t.textFaint)} onClick={() => toggleDipp(item.id)}>{item.dipp ? "★" : "D"}</button><button style={S.sm(t.bg3, t.textFaint)} onClick={() => delScan(item.id)}>✕</button></span></td>
                </tr>); })}</tbody></table></div>}
          </div>
        </>)}
        {wsTab === "compare" && (<>
          <div style={S.card}><div style={S.cH}><span style={S.cL}>POs ({purchaseOrders.length})</span><label style={{ ...S.btn(t.accent, "#fff"), cursor: "pointer" }}>+ XLSX<input ref={fileInputRef} type="file" accept=".xlsx,.xls,.csv" multiple onChange={handleFileUpload} style={{ display: "none" }} /></label></div>
            {purchaseOrders.length > 0 && <div style={{ padding: "8px 12px", display: "flex", gap: 6, flexWrap: "wrap", borderBottom: `1px solid ${t.border}` }}><button style={{ padding: "5px 10px", background: activePO === "__all__" ? t.accentBg : t.bg3, border: `1px solid ${activePO === "__all__" ? t.accent : t.border}`, borderRadius: 4, cursor: "pointer", fontSize: 11, color: activePO === "__all__" ? t.accentText : t.textMuted, fontFamily: ff }} onClick={() => setActivePO("__all__")}>All</button>{purchaseOrders.map(po => <button key={po.id} style={{ padding: "5px 10px", background: activePO === po.pbsPO ? t.accentBg : t.bg3, border: `1px solid ${activePO === po.pbsPO ? t.accent : t.border}`, borderRadius: 4, cursor: "pointer", fontSize: 11, color: activePO === po.pbsPO ? t.accentText : t.textMuted, fontFamily: ff, display: "flex", alignItems: "center", gap: 6 }} onClick={() => setActivePO(po.pbsPO)}><strong>{po.pbsPO}</strong>{po.gmControl && ` · ${po.gmControl}`}<span style={S.sm("transparent", t.textFaint)} onClick={e => { e.stopPropagation(); removePO(po.id); }}>✕</span></button>)}</div>}
            <details style={{ borderTop: `1px solid ${t.border}` }}><summary style={{ padding: "8px 12px", cursor: "pointer", fontSize: 11, color: t.textMuted }}>Paste CSV</summary><div style={{ padding: 12 }}><input style={S.inp} value={csvPO} onChange={e => setCsvPO(e.target.value)} placeholder="PO Name" /><textarea style={{ width: "100%", padding: 10, background: t.bgInput, border: `1px solid ${t.border}`, borderRadius: 4, color: t.text, fontFamily: ff, fontSize: 12, minHeight: 80, resize: "vertical", boxSizing: "border-box", marginTop: 6 }} value={csvText} onChange={e => setCsvText(e.target.value)} placeholder="Paste..." /><button style={{ ...S.btn(t.accent, "#fff"), marginTop: 6 }} onClick={handleCSVPaste}>Load</button></div></details>
          </div>
          {purchaseOrders.length > 0 && (<><div style={{ display: "flex", gap: 8, marginBottom: 10, alignItems: "center" }}><span style={{ fontSize: 10, color: t.textMuted, fontWeight: 600 }}>SHIPMENT:</span><select style={S.sel} value={selectedShipment} onChange={e => setSelectedShipment(e.target.value)}><option value="all">All</option>{getShipNums().map(sn => <option key={sn} value={sn}>{sn}</option>)}</select></div>
            <div style={S.card}><div style={S.cH}><span style={S.cL}><span style={{ color: t.greenText }}>{nM}✓</span> · <span style={{ color: t.redText }}>{nS} short</span> · <span style={{ color: t.yellowText }}>{nO} over</span></span></div>
              {comp.length === 0 ? <div style={S.empty}>No results.</div> : <div style={{ overflowX: "auto", maxHeight: 500, overflowY: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Status</th><th style={S.th}>Part #</th><th style={S.th}>SO</th><th style={S.th}>Exp</th><th style={S.th}>Scan</th><th style={S.th}>Diff</th><th style={S.th}>Notes</th></tr></thead><tbody>
                {comp.map((r, i) => { const c = sc(r.status); return <tr key={i} style={{ background: c.row }}><td style={S.td()}><span style={S.bg(c.bg, c.tx)}>{STATUS_CFG[r.status]?.label}</span></td><td style={S.td(t.textStrong)}><strong>{r.partNumber}</strong>{r.superseded && <div style={{ fontSize: 9, color: t.yellowText }}>↳{r.partOrdered}</div>}</td><td style={S.td()}>{r.shippingOrder}</td><td style={S.td()}>{r.expectedQty}</td><td style={S.td(r.scannedQty !== r.expectedQty ? c.tx : null)}>{r.scannedQty}</td><td style={S.td(r.qtyDiff > 0 ? t.yellowText : r.qtyDiff < 0 ? t.redText : t.greenText)}>{r.qtyDiff > 0 ? "+" : ""}{r.qtyDiff}</td><td style={S.td()}>{r.wrongDealer && <span style={{ ...S.bg(`${t.purple}15`, t.purpleText), marginRight: 3 }}>WD</span>}{r.superseded && <span style={{ ...S.bg(`${t.yellow}15`, t.yellowText), marginRight: 3 }}>SUP</span>}{r.dipp && <span style={S.bg(`${t.blue}15`, t.blueText)}>D</span>}</td></tr>; })}</tbody></table></div>}</div></>)}
        </>)}
        {wsTab === "wrongdealer" && <div style={S.card}><div style={S.cH}><span style={S.cL}>Wrong Dealer ({getWD().length})</span></div>{getWD().length === 0 ? <div style={S.empty}>None.</div> : <div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>Dealer</th><th style={S.th}>Belongs To</th><th style={S.th}>Contact</th><th style={S.th}>SO</th><th style={S.th}>Qty</th></tr></thead><tbody>{getWD().map(item => { const d = lookupDealer(item.dealerCode); return <tr key={item.id}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td(t.purpleText)}><strong>{item.dealerCode}</strong></td><td style={S.td()}>{d ? d.name : <em style={{ color: t.textFaint }}>Unknown</em>}</td><td style={S.td()}>{d?.email ? <span>{d.contact} <span style={{ color: t.blueText }}>{d.email}</span></span> : "—"}</td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}>{item.quantity}</td></tr>; })}</tbody></table></div>}</div>}
        {wsTab === "dipp" && <div style={S.card}><div style={S.cH}><span style={S.cL}>DIPP ({getDipp().length})</span></div>{getDipp().length === 0 ? <div style={S.empty}>None.</div> : <div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>PDC</th><th style={S.th}>SO</th><th style={S.th}>Description</th><th style={S.th}>Comments</th></tr></thead><tbody>{getDipp().map(item => <tr key={item.id}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td()}>{item.pdc}</td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}><input style={{ ...S.inp, width: "100%", minWidth: 120 }} value={dippDescriptions[item.id] || ""} onChange={e => setDippDescriptions(p => ({ ...p, [item.id]: e.target.value }))} placeholder="Part description..." /></td><td style={S.td()}><input style={{ ...S.inp, width: "100%", minWidth: 140 }} value={dippComments[item.id] || ""} onChange={e => setDippComments(p => ({ ...p, [item.id]: e.target.value }))} placeholder="Damage..." /><div style={{ display: "flex", gap: 3, flexWrap: "wrap", marginTop: 4 }}>{DIPP_PRESETS.map(p => <button key={p} style={S.sm(t.bg3, t.textMuted)} onClick={() => setDippComments(prev => ({ ...prev, [item.id]: (prev[item.id] || "") + (prev[item.id] ? ", " : "") + p }))}>{p}</button>)}</div></td></tr>)}</tbody></table></div>}</div>}
        {wsTab === "form" && (<>
          <div style={S.card}>
            <div style={S.cH}><span style={S.cL}>Woodstock Form</span></div>
            <div style={{ padding: 12, borderBottom: `1px solid ${t.border}` }}><div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}><div><label style={S.lbl}>Completed By</label><input style={S.inp} value={completedBy} onChange={e => setCompletedBy(e.target.value)} placeholder="Name" /></div><div><label style={S.lbl}>Date</label><input style={S.inp} type="date" value={formDate} onChange={e => setFormDate(e.target.value)} /></div><div><label style={S.lbl}>Phone</label><input style={{ ...S.inp, color: t.textFaint }} value={settings.phone} readOnly /></div>{purchaseOrders.length > 1 && <div><label style={S.lbl}>PO</label><select style={S.sel} value={activePO || ""} onChange={e => setActivePO(e.target.value)}><option value="__all__">All</option>{purchaseOrders.map(po => <option key={po.id} value={po.pbsPO}>{po.pbsPO}</option>)}</select></div>}</div></div>
            <div style={{ display: "flex", gap: 8, padding: 12, background: t.bg3, flexWrap: "wrap", alignItems: "center" }}>
              <button style={{ padding: "8px 16px", background: t.accent, color: "#fff", border: "none", borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={generatePDFHandler} disabled={pdfGenerating}>{pdfGenerating ? "⏳..." : "⬇ Generate PDF"}</button>
              {lastPdfBlob && <><button style={{ padding: "8px 16px", background: t.bg2, color: t.text, border: `1px solid ${t.border}`, borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={printPDF}>🖨 Print</button><button style={{ padding: "8px 16px", background: "#0078d4", color: "#fff", border: "none", borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={emailOutlook}>✉ Outlook</button></>}
              {lastPdfBlob && <span style={{ fontSize: 10, color: t.greenText, fontWeight: 600 }}>✓ {lastPdfName}</span>}
            </div>
            {lastPdfBlob && <div style={{ padding: 12, borderTop: `1px solid ${t.border}` }}><div style={{ background: t.bg0, border: `1px solid ${t.border}`, borderRadius: 6, padding: 12, fontSize: 12 }}><div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>To: </span>{settings.wdkEmail}</div>{getCCStr() && <div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>CC: </span><span style={{ color: t.blueText }}>{getCCStr()}</span></div>}<div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>Subject: </span><strong>{emailSubject}</strong></div><div><span style={{ color: t.textMuted }}>Attach: </span><span style={{ color: t.greenText }}>📎 {lastPdfName}</span></div></div></div>}
            <div style={{ padding: 12, display: "flex", gap: 20, flexWrap: "wrap", borderTop: `1px solid ${t.border}` }}>{[["Shorts", nS, t.red], ["WD", stats.wd, t.purple], ["DIPP", stats.dipp, t.blue], ["Over", nO, t.yellow], ["Match", nM, t.green]].map(([l, n, c]) => <div key={l} style={{ display: "flex", alignItems: "center", gap: 6 }}><span style={{ ...S.dot(c) }}></span><span style={{ fontSize: 12, color: t.textMuted }}>{l}:</span><strong style={{ color: c }}>{n}</strong></div>)}</div>
          </div>
        </>)}
      </div>
    </div>
  );
}
