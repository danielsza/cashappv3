import { useState, useRef, useEffect, useCallback } from "react";
import ExcelJS from "exceljs";
import { PDFDocument, StandardFonts, rgb } from "pdf-lib";
import { WOODSTOCK_TEMPLATE_B64 } from "./woodstockTemplate.js";

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
  users: ["Daniel"], defaultUser: "Daniel",
  highlightColor: "#c2f4fc", customPdfTemplate: "",
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
async function parseXLSXFile(file) {
  const buf = await file.arrayBuffer();
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.load(buf);
  const ws = wb.worksheets[0];
  if (!ws || ws.rowCount < 2) return [];
  const headers = [];
  ws.getRow(1).eachCell((cell, col) => { headers[col] = String(cell.value || "").trim(); });
  const rows = [];
  for (let r = 2; r <= ws.rowCount; r++) {
    const row = ws.getRow(r);
    const obj = {};
    let hasData = false;
    headers.forEach((h, col) => {
      if (!h) return;
      const v = row.getCell(col).value;
      obj[h] = v != null ? (typeof v === "object" && v.result !== undefined ? v.result : v) : "";
      if (v != null && v !== "") hasData = true;
    });
    if (hasData) rows.push(obj);
  }
  return rows;
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
const GM_LOGO_B64 = "data:image/jpeg;base64,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OAV/hdCGRueGUgjqCKK8u+Jnx58NeDNCtNY0/Tv+EisL27e285XuIbG+EQjae3t7uFkLybZUV3gkzB5gZmD+XHKUmxpHwlXS33xI8a6vqN9rPiPW28QalqRVrm+12CLVLlyGZsiW6WR1yXYnaRuzzmuaoqRnTyfEnxh5FvBYX9ppBtkmjE2jadbaZPLHKIw6TS2scckyfuUIWRmVTuKgF3LYWratquv6re67rup3eo6lqNxJd3l5dzNNPczyMWklkkYlndmJYsSSSSTVWigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAtaZq2q6LcveaNqd3YXElvPaPLbTNE7QTxPDNEWUglJIpJI3XoyOynIJFVaKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKANXw94s8VeEZrq48KeJdV0WW+t/sl1Jp97JbNPB5iSeU5jILJ5kUb7TkbkU9VBG5/wtfxR/bn/CQ/2V4N+1eb53l/8ACF6P9m3b9+Ps32Xyduf4dm3Hy428Vx1FAGr4h8WeKvF01rceK/Euq61LY2/2S1k1C9kuWgg8x5PKQyElU8yWR9owNzserEnKoooAKKKKACiiigAooooAK1fDPizxV4L1WPXfB3iXVdB1KLHl3mmXslrOmGVhiSMhhhlVuvVQewrKooA6fVviL4g1qe9uLzT/AAxG9+kiSi08L6Zaqockt5aw26rEeTgxhSvG3GBXP31/fandPfalez3dzJjfNPIZHbAAGWOScAAfQVBRQAUUUUAFFFFAEtpd3VhdQ31jcy21zbSLNDNC5R43U5VlYcgggEEcgioqKKACiiigDQ0PXtT8OajFqelyxCSKSKRori3jubecRypKqTwSq0U8e+NGMcisjbRuUit+2+K3i2ykaext/DVtP5ckaTw+FtLjlhLoV3xSLbhopF3ZSRCHRgroysqsOQooA0Ne8Q6/4p1N9a8T65qGr6jLHFC93f3L3EzJFGsUSl3JYhI0RFGcBVUDAAFFZ9FABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUVLaWl1f3UNjY20tzc3MiwwwwoXeR2OFVVHJJJAAHJJoAior6N+GX7K3iWWaS98d+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";

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
async function generateWoodstockPDF({ settings, shortItems, dippItems, dippComments, dippDescriptions = {}, wrongDealerItems, completedBy, formDate, poInfo, toteChoices = {}, wdContact = {}, wdRedirect = {} }) {
  // Load the original Woodstock form as template (pixel-perfect layout)
  const templateB64 = settings.customPdfTemplate || WOODSTOCK_TEMPLATE_B64;
  const templateBytes = Uint8Array.from(atob(templateB64), c => c.charCodeAt(0));
  const pdfDoc = await PDFDocument.load(templateBytes);
  const page = pdfDoc.getPages()[0];
  const font = await pdfDoc.embedFont(StandardFonts.Helvetica);
  const fontBold = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
  const pH = 792; // letter page height in pts

  // pdfplumber uses top-origin; pdf-lib uses bottom-origin
  const toY = (plumberTop) => pH - plumberTop;

  // Draw text centered in a table cell (pdfplumber coords)
  function drawInCell(text, x0, y0, x1, y1, opts = {}) {
    if (!text) return;
    const size = opts.size || 9;
    const f = opts.bold ? fontBold : font;
    const tw = f.widthOfTextAtSize(String(text), size);
    const th = size * 0.7;
    const cellW = x1 - x0;
    const cellH = y1 - y0;
    const x = opts.align === "left" ? x0 + 3 : x0 + (cellW - tw) / 2;
    const y = toY(y0 + cellH / 2 + th / 2);
    page.drawText(String(text), { x: Math.max(x, x0 + 2), y, size, font: f, color: rgb(0, 0, 0) });
  }

  // === SHORT INFO TABLE ===
  const sCols = [28.3, 95.6, 251.1, 373.1, 435.2, 501.2, 567.7];
  const sRows = [362.2, 386.9, 408.5, 431.0, 458.8];
  for (let r = 0; r < Math.min(shortItems.length, 4); r++) {
    const item = shortItems[r];
    drawInCell(String(r + 1), sCols[0], sRows[r], sCols[1], sRows[r + 1]);
    drawInCell(item.partNumber, sCols[1], sRows[r], sCols[2], sRows[r + 1]);
    drawInCell(item.shippingOrder, sCols[2], sRows[r], sCols[3], sRows[r + 1]);
    drawInCell(String(item.expectedQty), sCols[3], sRows[r], sCols[4], sRows[r + 1]);
    drawInCell(String(item.scannedQty), sCols[4], sRows[r], sCols[5], sRows[r + 1]);
    const tKey = `${item.partNumber}_${item.shippingOrder}`;
    const tVal = toteChoices[tKey] || "";
    drawInCell(tVal === "T" ? "Tote" : tVal === "P" ? "Pallet" : tVal === "?" ? "Unknown" : "", sCols[5], sRows[r], sCols[6], sRows[r + 1]);
  }

  // === DIPP TABLE ===
  const dCols = [36.2, 132.5, 184.2, 258.2, 373.1, 482.5, 577.7];
  const dRows = [536.9, 559.0, 581.0];
  for (let r = 0; r < Math.min(dippItems.length, 2); r++) {
    const item = dippItems[r];
    drawInCell(item.partNumber, dCols[0], dRows[r], dCols[1], dRows[r + 1], { size: 8 });
    drawInCell(item.pdc, dCols[1], dRows[r], dCols[2], dRows[r + 1], { size: 8 });
    drawInCell(item.shippingOrder, dCols[2], dRows[r], dCols[3], dRows[r + 1], { size: 8 });
    drawInCell(dippDescriptions[item.id] || "", dCols[3], dRows[r], dCols[4], dRows[r + 1], { size: 8, align: "left" });
    drawInCell(dippComments[item.id] || "", dCols[4], dRows[r], dCols[5], dRows[r + 1], { size: 8, align: "left" });
    drawInCell("Y", dCols[5], dRows[r], dCols[6], dRows[r + 1], { size: 8 });
  }

  // === WRONG DEALER TABLE ===
  const wCols = [36.2, 143.4, 251.1, 357.8, 465.1, 572.4];
  const wRows = [652.4, 677.0, 703.0];
  for (let r = 0; r < Math.min(wrongDealerItems.length, 2); r++) {
    const item = wrongDealerItems[r];
    drawInCell(item.partNumber, wCols[0], wRows[r], wCols[1], wRows[r + 1], { size: 8 });
    drawInCell(item.dealerCode, wCols[1], wRows[r], wCols[2], wRows[r + 1], { size: 8 });
    drawInCell(item.shippingOrder, wCols[2], wRows[r], wCols[3], wRows[r + 1], { size: 8 });
    drawInCell(wdContact[item.id] === "Y" ? "Yes" : wdContact[item.id] === "N" ? "No" : "", wCols[3], wRows[r], wCols[4], wRows[r + 1], { size: 8 });
    drawInCell(wdRedirect[item.id] === "Y" ? "Yes" : wdRedirect[item.id] === "N" ? "No" : "", wCols[4], wRows[r], wCols[5], wRows[r + 1], { size: 8 });
  }

  // === COMPLETED BY / DATE / PHONE ===
  // Underscores: completedBy x=126.5-248.7, date x=306-374.9, phone x=492.9-569.4
  // Baseline y top=288.1, bottom=300.1 (pdfplumber coords)
  if (completedBy) {
    page.drawText(completedBy, { x: 128, y: toY(298), size: 10, font: fontBold, color: rgb(0, 0, 0) });
  }
  if (formDate) {
    page.drawText(formDate, { x: 308, y: toY(298), size: 10, font: fontBold, color: rgb(0, 0, 0) });
  }
  if (settings.phone) {
    page.drawText(settings.phone, { x: 494, y: toY(298), size: 10, font: fontBold, color: rgb(0, 0, 0) });
  }

  return pdfDoc;
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

// ─── Pink Sheet Generator ───────────────────────────────────────
async function generatePinkSheet(purchaseOrders, activePO, scannedItems = []) {
  const wb = new ExcelJS.Workbook();
  const pinkFill = { type: "pattern", pattern: "solid", fgColor: { argb: "FFFF80FF" } };
  const boldFont = { bold: true, size: 11, name: "Arial" };
  const normalFont = { size: 10, name: "Arial" };
  const headerFont = { bold: true, size: 10, name: "Arial" };
  const titleFont = { bold: true, size: 14, name: "Arial", underline: true };

  const pos = activePO === "__all__" ? purchaseOrders : purchaseOrders.filter(p => p.pbsPO === activePO);
  if (!pos.length) return null;

  for (const po of pos) {
    const allRows = po.data;
    const shipNums = [...new Set(allRows.filter(r => r.status === "Shipped" && r.shipmentNo).map(r => r.shipmentNo))].sort();
    if (!shipNums.length) { shipNums.push("none"); }

    for (const shipNo of shipNums) {
      const sheetName = `${po.pbsPO}_${shipNo}`.substring(0, 31);
      const ws = wb.addWorksheet(sheetName);
      ws.pageSetup = { orientation: "landscape", paperSize: 1, fitToPage: true, fitToWidth: 1, fitToHeight: 0 };

      // PO# and Shipping Order#
      ws.getCell("A1").value = po.gmControl || po.pbsPO;
      ws.getCell("A1").font = titleFont;
      ws.getCell("A1").alignment = { horizontal: "right" };
      ws.getCell("B1").value = po.gmControl ? `(${po.pbsPO})` : "";
      ws.getCell("B1").font = { ...normalFont, italic: true };
      ws.getCell("A2").value = shipNo !== "none" ? Number(shipNo) || shipNo : "";
      ws.getCell("A2").font = titleFont;
      ws.getCell("A2").alignment = { horizontal: "right" };

      // Blank row 3, 4
      // Header row 5
      const headers = ["Current Status", "Line Item No.", "Part No. Ordered", "Part No. Processed", "Facility", "Qty Ordered", "Qty Proc.", "Shipment No.", "Notes"];
      const hRow = ws.getRow(5);
      headers.forEach((h, i) => {
        const cell = hRow.getCell(i + 1);
        cell.value = h;
        cell.font = headerFont;
      });

      // Separate non-shipped vs shipped for this shipping order
      const nonShipped = allRows.filter(r => r.status !== "Shipped").sort((a, b) => a.partOrdered.localeCompare(b.partOrdered));
      const shipped = allRows.filter(r => r.status === "Shipped" && r.shipmentNo === shipNo).sort((a, b) => a.partProcessed.localeCompare(b.partProcessed));

      // Skip if nothing
      let row = 6;

      // Write non-shipped rows (Written, Backordered, etc.)
      for (const r of nonShipped) {
        const dr = ws.getRow(row);
        dr.getCell(1).value = r.status;
        dr.getCell(2).value = "";
        dr.getCell(3).value = r.partOrdered;
        dr.getCell(4).value = r.partProcessed;
        dr.getCell(5).value = r.facility;
        dr.getCell(6).value = r.qtyOrdered;
        dr.getCell(7).value = r.qtyProc;
        dr.getCell(8).value = r.shipmentNo || "";
        dr.getCell(9).value = "";
        for (let c = 1; c <= 9; c++) dr.getCell(c).font = normalFont;
        // Supersession highlight
        if (r.partOrdered && r.partProcessed && r.partOrdered !== r.partProcessed) {
          dr.getCell(4).fill = pinkFill;
          dr.getCell(4).font = { ...normalFont, bold: true };
        }
        row++;
      }

      // Thick separator line
      if (nonShipped.length > 0 && shipped.length > 0) {
        const sepRow = row - 1;
        for (let c = 1; c <= 9; c++) {
          ws.getRow(sepRow).getCell(c).border = {
            ...ws.getRow(sepRow).getCell(c).border,
            bottom: { style: "thick" }
          };
        }
      } else if (nonShipped.length === 0 && shipped.length > 0) {
        // no separator needed
      }

      // Write shipped rows
      const greenFill = { type: "pattern", pattern: "solid", fgColor: { argb: "FF90EE90" } };
      const greenBorder = { top: { style: "medium", color: { argb: "FF228B22" } }, bottom: { style: "medium", color: { argb: "FF228B22" } }, left: { style: "medium", color: { argb: "FF228B22" } }, right: { style: "medium", color: { argb: "FF228B22" } } };
      const circleBorder = { top: { style: "medium" }, bottom: { style: "medium" }, left: { style: "medium" }, right: { style: "medium" } };
      for (const r of shipped) {
        const dr = ws.getRow(row);
        dr.getCell(1).value = r.status;
        dr.getCell(2).value = "";
        dr.getCell(3).value = r.partOrdered;
        dr.getCell(4).value = r.partProcessed;
        dr.getCell(5).value = r.facility;
        dr.getCell(6).value = r.qtyOrdered;
        dr.getCell(7).value = r.qtyProc;
        dr.getCell(8).value = r.shipmentNo || "";
        dr.getCell(9).value = "";
        for (let c = 1; c <= 9; c++) dr.getCell(c).font = normalFont;
        const notes = [];
        // Supersession highlight (Part Ordered ≠ Part Processed)
        if (r.partOrdered && r.partProcessed && r.partOrdered !== r.partProcessed) {
          dr.getCell(4).fill = pinkFill;
          dr.getCell(4).font = { ...normalFont, bold: true };
          notes.push(`SUP: ${r.partOrdered}`);
        }
        // Qty difference: Ordered ≠ Proc — circle Qty Proc + pink highlight + note
        if (r.qtyOrdered !== r.qtyProc) {
          dr.getCell(7).fill = pinkFill;
          dr.getCell(7).font = { ...normalFont, bold: true };
          dr.getCell(7).border = circleBorder;
          const diff = r.qtyProc - r.qtyOrdered;
          notes.push(`Ord:${r.qtyOrdered} Proc:${r.qtyProc} (${diff > 0 ? "+" : ""}${diff})`);
        }
        // Scanned verification: find matching scan by part + SO
        const partToMatch = r.partProcessed || r.partOrdered;
        const scan = scannedItems.find(s => s.partNumber === partToMatch && s.shippingOrder === r.shipmentNo);
        if (scan) {
          if (scan.quantity === r.qtyProc) {
            // Scanned and matches — green circle (only if no pink override)
            if (r.qtyOrdered === r.qtyProc) {
              dr.getCell(7).fill = greenFill;
              dr.getCell(7).border = greenBorder;
              dr.getCell(7).font = { ...normalFont, bold: true };
            }
            notes.push(`✓ Scanned: ${scan.quantity}`);
          } else {
            // Scanned but qty mismatch
            dr.getCell(7).fill = pinkFill;
            dr.getCell(7).font = { ...normalFont, bold: true };
            dr.getCell(7).border = circleBorder;
            notes.push(`Scanned: ${scan.quantity} (expected ${r.qtyProc})`);
          }
        }
        if (notes.length) dr.getCell(9).value = notes.join(" | ");
        row++;
      }

      // Auto-fit columns
      ws.columns = [
        { width: 14 }, { width: 12 }, { width: 16 }, { width: 16 },
        { width: 10 }, { width: 12 }, { width: 10 }, { width: 14 }, { width: 30 }
      ];
    }
  }

  const buffer = await wb.xlsx.writeBuffer();
  return new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
}

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
  const [newUserName, setNewUserName] = useState("");
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
  const [completedBy, setCompletedBy] = useState(() => { const s = loadSettings(); return s.defaultUser || (s.users && s.users[0]) || ""; });
  const [formDate, setFormDate] = useState(new Date().toISOString().split("T")[0]);
  const [toteChoices, setToteChoices] = useState({});  // key: partNumber+SO -> "T"/"P"/"?"
  const [wdContact, setWdContact] = useState({});       // key: id -> "Y"/"N"/""
  const [wdRedirect, setWdRedirect] = useState({});     // key: id -> "Y"/"N"/""
  const [pdfGenerating, setPdfGenerating] = useState(false);
  const [lastPdfBase64, setLastPdfBase64] = useState(null);
  const [lastPdfBlob, setLastPdfBlob] = useState(null);
  const [lastPdfName, setLastPdfName] = useState("");
  const [scanInput, setScanInput] = useState("");
  const [csvText, setCsvText] = useState(""); const [csvPO, setCsvPO] = useState("");
  const scanRef = useRef(null);
  const fileInputRef = useRef(null);
  const scanFileRef = useRef(null);

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
              const merged = [...prev];
              for (const s of d2.scans) {
                const ex = merged.find(i => i.partNumber === s.partNumber && i.shippingOrder === s.shippingOrder);
                if (ex) { ex.quantity = s.quantity; ex.scans = s.scans || ex.scans; ex.dipp = s.dipp || ex.dipp; ex.wrongDealer = s.wrongDealer || ex.wrongDealer; }
                else merged.push(s);
              }
              return merged;
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
  const importScanFile = async (e) => {
    const file = e.target.files?.[0]; if (!file) return;
    try {
      const text = await file.text();
      let items = [];
      if (file.name.endsWith(".json")) {
        const data = JSON.parse(text);
        items = Array.isArray(data) ? data : data.scans || data.items || [];
      } else {
        // CSV: PartNumber,ShippingOrder,Quantity,PDC,DealerCode
        const lines = text.split(/\r?\n/).filter(l => l.trim());
        const header = lines[0].toLowerCase();
        const hasHeader = header.includes("part") || header.includes("qty") || header.includes("ship");
        const start = hasHeader ? 1 : 0;
        for (let i = start; i < lines.length; i++) {
          const cols = lines[i].split(",").map(c => c.trim().replace(/^["']|["']$/g, ""));
          if (!cols[0]) continue;
          items.push({ partNumber: cols[0], shippingOrder: cols[1] || "", quantity: parseInt(cols[2], 10) || 1, pdc: cols[3] || "", dealerCode: cols[4] || settings.dealerCode });
        }
      }
      let added = 0;
      setScannedItems(prev => {
        const merged = [...prev];
        for (const item of items) {
          const pn = String(item.partNumber || "").replace(/\s/g, "");
          const so = String(item.shippingOrder || "").replace(/\s/g, "");
          if (!pn) continue;
          const ex = merged.find(i => i.partNumber === pn && i.shippingOrder === so);
          if (ex) { ex.quantity = item.quantity || ex.quantity; }
          else { merged.push({ partNumber: pn, shippingOrder: so, quantity: item.quantity || 1, pdc: item.pdc || "", dealerCode: item.dealerCode || settings.dealerCode, wrongDealer: item.wrongDealer || (item.dealerCode && item.dealerCode !== settings.dealerCode), raw: `import:${pn}`, scans: [`import:${pn}`], id: Date.now() + Math.random() + added, dipp: item.dipp || false }); }
          added++;
        }
        return merged;
      });
      showFB(`✓ Imported ${added} scan records from ${file.name}`, t.green);
    } catch (err) { showFB(`Import error: ${err.message}`, t.red); }
    if (scanFileRef.current) scanFileRef.current.value = "";
  };
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

  const generatePDFHandler = async () => { setPdfGenerating(true); try { const pdfDoc = await generateWoodstockPDF({ settings, shortItems: shortItems, dippItems: getDipp(), dippComments, dippDescriptions, wrongDealerItems: getWD(), completedBy, formDate, poInfo, toteChoices, wdContact, wdRedirect }); const pdfBytes = await pdfDoc.save(); const fn = `woodstock_form_${poInfo ? `${poInfo.pbsPO}_${poInfo.gmControl}` : "form"}_${formDate}.pdf`; const blob = new Blob([pdfBytes], { type: "application/pdf" }); setLastPdfBlob(blob); const b64 = btoa(pdfBytes.reduce((d, b) => d + String.fromCharCode(b), "")); setLastPdfBase64(b64); setLastPdfName(fn); const url = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = url; a.download = fn; a.click(); URL.revokeObjectURL(url); showFB("✓ PDF generated", t.green); } catch (err) { showFB(`PDF error: ${err.message}`, t.red); } setPdfGenerating(false); };
  const isElectron = !!(window.electronAPI?.isElectron);
  const printPDF = async () => {
    if (!lastPdfBlob) return;
    if (isElectron && lastPdfBase64) {
      const res = await window.electronAPI.printPdf({ base64: lastPdfBase64, filename: lastPdfName });
      if (res.success) { showFB("✓ Sent to print", t.green); return; }
    }
    const url = URL.createObjectURL(lastPdfBlob);
    const iframe = document.createElement("iframe");
    iframe.style.display = "none";
    iframe.src = url;
    document.body.appendChild(iframe);
    iframe.onload = () => { iframe.contentWindow.focus(); iframe.contentWindow.print(); setTimeout(() => { document.body.removeChild(iframe); URL.revokeObjectURL(url); }, 60000); };
  };
  const emailOutlook = async () => {
    if (!lastPdfBase64) return;
    const body = `Please find the attached Woodstock form for dealer ${settings.dealerCode} (${settings.dealerName}).\n\nDate: ${formDate}\nCompleted by: ${completedBy || "(not specified)"}\nPhone: ${settings.phone}${poInfo ? `\nPO: ${poInfo.pbsPO} / GM Control: ${poInfo.gmControl}` : ""}\n\nSummary:\n${shortItems.length ? `- ${shortItems.length} short\n` : ""}${getDipp().length ? `- ${getDipp().length} DIPP\n` : ""}${getWD().length ? `- ${getWD().length} wrong dealer\n` : ""}`;
    if (isElectron) {
      try {
        const tmpRes = await window.electronAPI.saveTempPdf({ base64: lastPdfBase64, filename: lastPdfName });
        if (!tmpRes.success) throw new Error(tmpRes.error);
        const res = await window.electronAPI.sendOutlookEmail({ to: settings.wdkEmail, cc: getCCStr(), subject: emailSubject, body, pdfPath: tmpRes.path, pdfFilename: lastPdfName });
        if (res.success) { showFB("✓ Outlook email opened with PDF attached", t.green); return; }
        throw new Error(res.error);
      } catch (err) { showFB(`Outlook COM failed, falling back to .eml: ${err.message}`, t.yellow); }
    }
    const eml = buildEML({ to: settings.wdkEmail, cc: getCCStr(), subject: emailSubject, bodyText: body, pdfBase64: lastPdfBase64, pdfFilename: lastPdfName });
    const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([eml], { type: "message/rfc822" })); a.download = `woodstock_${formDate}.eml`; document.body.appendChild(a); a.click(); document.body.removeChild(a);
    showFB(isElectron ? "✓ .eml downloaded (Outlook COM unavailable)" : "✓ .eml downloaded — open in Outlook", t.green);
  };
  const downloadPinkSheet = async () => {
    try {
      const hlColor = settings.highlightColor || "#c2f4fc";
      const pos = activePO === "__all__" ? purchaseOrders : purchaseOrders.filter(p => p.pbsPO === activePO);
      if (!pos.length) { showFB("No PO data", t.red); return; }
      let html = `<html><head><title>Pink Sheet</title><style>
@page{size:landscape;margin:0.5in}
body{font-family:Arial,sans-serif;font-size:10pt;margin:0;padding:0}
.sheet{page-break-after:always;padding:20px}.sheet:last-child{page-break-after:auto}
h1{font-size:14pt;text-decoration:underline;margin:0;font-weight:bold}
h2{font-size:14pt;text-decoration:underline;margin:0 0 14px;font-weight:bold}
table{border-collapse:collapse;width:100%}
th{font-weight:bold;text-align:left;padding:3px 6px;font-size:9pt;border-bottom:1px solid #000}
td{padding:3px 6px;font-size:9pt}
.sep td{border-bottom:3px solid #000}
.hl{background:${hlColor};font-weight:bold}
.notes{font-size:8pt;color:#555}
.circ{display:inline-block;min-width:18px;text-align:center;padding:1px 6px;font-weight:bold;border-radius:50%;border:2px solid #000}
.qty-diff{background:${hlColor}}
</style></head><body>`;
      for (const po of pos) {
        const allRows = po.data;
        const shipNums = [...new Set(allRows.filter(r => r.status === "Shipped" && r.shipmentNo).map(r => r.shipmentNo))].sort();
        if (!shipNums.length) shipNums.push("none");
        for (const shipNo of shipNums) {
          html += `<div class="sheet"><h1>${po.pbsPO}</h1><h2>${shipNo !== "none" ? shipNo : ""}</h2>`;
          html += `<table><thead><tr><th>Status</th><th>Line#</th><th>Part Ordered</th><th>Part Processed</th><th>Facility</th><th>Qty Ord</th><th>Qty Proc</th><th>Ship#</th><th>Notes</th></tr></thead><tbody>`;
          const nonShipped = allRows.filter(r => r.status !== "Shipped").sort((a, b) => a.partOrdered.localeCompare(b.partOrdered));
          const shipped = allRows.filter(r => r.status === "Shipped" && r.shipmentNo === shipNo).sort((a, b) => a.partProcessed.localeCompare(b.partProcessed));
          for (const r of nonShipped) {
            const isSup = r.partOrdered && r.partProcessed && r.partOrdered !== r.partProcessed;
            const isLast = r === nonShipped[nonShipped.length - 1] && shipped.length > 0;
            html += `<tr${isLast ? ' class="sep"' : ""}><td>${r.status}</td><td></td><td>${r.partOrdered}</td><td${isSup ? ' class="hl"' : ""}>${r.partProcessed}</td><td>${r.facility}</td><td>${r.qtyOrdered}</td><td>${r.qtyProc}</td><td>${r.shipmentNo || ""}</td><td></td></tr>`;
          }
          for (const r of shipped) {
            const isSup = r.partOrdered && r.partProcessed && r.partOrdered !== r.partProcessed;
            const qtyDiff = r.qtyOrdered !== r.qtyProc;
            const partToMatch = r.partProcessed || r.partOrdered;
            const scan = scannedItems.find(s => s.partNumber === partToMatch && s.shippingOrder === r.shipmentNo);
            const notes = [];
            let qtyHtml = String(r.qtyProc);
            let tdExtra = "";
            if (isSup) notes.push(`SUP: ${r.partOrdered}`);
            if (qtyDiff) notes.push(`Ord:${r.qtyOrdered} Proc:${r.qtyProc} (${r.qtyProc - r.qtyOrdered > 0 ? "+" : ""}${r.qtyProc - r.qtyOrdered})`);
            if (scan && scan.quantity === r.qtyProc) {
              // Scanned matches proc — circle it
              if (qtyDiff) {
                // Ordered ≠ Proc but scan matches proc: circle + yellow cell
                qtyHtml = `<span class="circ">${r.qtyProc}</span>`;
                tdExtra = ' class="qty-diff"';
              } else {
                // Perfect match: ordered = proc = scanned: plain black circle
                qtyHtml = `<span class="circ">${r.qtyProc}</span>`;
              }
              notes.push(`✓ Scanned: ${scan.quantity}`);
            } else if (scan) {
              // Scanned but doesn't match — no circle
              notes.push(`Scanned: ${scan.quantity} (exp ${r.qtyProc})`);
            }
            html += `<tr><td>${r.status}</td><td></td><td>${r.partOrdered}</td><td${isSup ? ' class="hl"' : ""}>${r.partProcessed}</td><td>${r.facility}</td><td>${r.qtyOrdered}</td><td${tdExtra}>${qtyHtml}</td><td>${r.shipmentNo || ""}</td><td class="notes">${notes.join(" | ")}</td></tr>`;
          }
          html += `</tbody></table></div>`;
        }
      }
      html += `</body></html>`;
      const iframe = document.createElement("iframe");
      iframe.style.display = "none";
      document.body.appendChild(iframe);
      iframe.contentDocument.open();
      iframe.contentDocument.write(html);
      iframe.contentDocument.close();
      iframe.onload = () => { iframe.contentWindow.focus(); iframe.contentWindow.print(); };
      setTimeout(() => { iframe.contentWindow.focus(); iframe.contentWindow.print(); }, 500);
      setTimeout(() => document.body.removeChild(iframe), 60000);
      showFB("✓ Pink sheet sent to printer", t.green);
    } catch (err) { showFB(`Pink sheet error: ${err.message}`, t.red); }
  };

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
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Pink Sheet Highlight Color</label><div style={{ display: "flex", gap: 8, alignItems: "center", marginTop: 4 }}><input type="color" value={settingsDraft.highlightColor || "#c2f4fc"} onChange={e => setSettingsDraft(p => ({ ...p, highlightColor: e.target.value }))} style={{ width: 40, height: 32, border: `1px solid ${t.border}`, borderRadius: 4, cursor: "pointer", padding: 0 }} /><input style={{ ...S.mI, flex: 1, marginBottom: 0, fontFamily: "monospace" }} value={settingsDraft.highlightColor || "#c2f4fc"} onChange={e => setSettingsDraft(p => ({ ...p, highlightColor: e.target.value }))} /><div style={{ width: 60, height: 32, borderRadius: 4, border: `1px solid ${t.border}`, background: settingsDraft.highlightColor || "#c2f4fc" }} /></div></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Woodstock PDF Template</label><div style={{ display: "flex", gap: 8, alignItems: "center", marginTop: 4 }}><label style={{ ...S.btn(t.bg3, t.text), cursor: "pointer", border: `1px solid ${t.border}` }}>📄 Upload PDF<input type="file" accept=".pdf" style={{ display: "none" }} onChange={e => { const file = e.target.files?.[0]; if (!file) return; const reader = new FileReader(); reader.onload = () => { const b64 = reader.result.split(",")[1]; setSettingsDraft(p => ({ ...p, customPdfTemplate: b64 })); }; reader.readAsDataURL(file); e.target.value = ""; }} /></label>{settingsDraft.customPdfTemplate ? <><span style={{ fontSize: 11, color: t.greenText }}>✓ Custom template loaded</span><button style={S.sm(t.bg3, t.redText)} onClick={() => setSettingsDraft(p => ({ ...p, customPdfTemplate: "" }))}>Reset to default</button></> : <span style={{ fontSize: 11, color: t.textMuted }}>Using built-in John Bear template</span>}</div></div>
            <div style={{ marginBottom: 14 }}><label style={S.lbl}>Users</label>
              <div style={{ display: "flex", gap: 6, marginTop: 4, marginBottom: 8 }}><input style={{ ...S.mI, flex: 1, marginBottom: 0 }} placeholder="Add user name" value={newUserName} onChange={e => setNewUserName(e.target.value)} onKeyDown={e => { if (e.key === "Enter") { const n = newUserName.trim(); if (n && !(settingsDraft.users || []).includes(n)) { setSettingsDraft(p => ({ ...p, users: [...(p.users || []), n] })); setNewUserName(""); } } }} /><button style={S.btn(t.accent, "#fff")} onClick={() => { const n = newUserName.trim(); if (n && !(settingsDraft.users || []).includes(n)) { setSettingsDraft(p => ({ ...p, users: [...(p.users || []), n] })); setNewUserName(""); } }}>Add</button></div>
              {(settingsDraft.users || []).map(u => <div key={u} style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 4 }}>
                <span style={{ flex: 1, fontSize: 13, color: t.text }}>{u}{u === settingsDraft.defaultUser && <span style={{ ...S.bg(`${t.green}20`, t.greenText), marginLeft: 6, fontSize: 10 }}>DEFAULT</span>}</span>
                <button style={S.sm(t.bg3, t.accent)} onClick={() => setSettingsDraft(p => ({ ...p, defaultUser: u }))}>★</button>
                <button style={S.sm(t.bg3, t.textFaint)} onClick={() => setSettingsDraft(p => ({ ...p, users: (p.users || []).filter(x => x !== u), defaultUser: p.defaultUser === u ? ((p.users || []).filter(x => x !== u)[0] || "") : p.defaultUser }))}>✕</button>
              </div>)}
            </div>
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
          <div style={S.card}><div style={S.cH}><span style={S.cL}>Scanned · {stats.unique} unique · {stats.total} total</span><div style={{ display: "flex", gap: 4 }}><label style={{ ...S.sm(t.bg3, t.textMuted), cursor: "pointer" }}>📥 Import<input ref={scanFileRef} type="file" accept=".csv,.json,.txt" onChange={importScanFile} style={{ display: "none" }} /></label>{scannedItems.length > 0 && <><button style={S.sm(t.bg3, t.textMuted)} onClick={() => { const data = scannedItems.map(i => `${i.partNumber},${i.shippingOrder},${i.quantity},${i.pdc},${i.dealerCode}`); const csv = "PartNumber,ShippingOrder,Quantity,PDC,DealerCode\n" + data.join("\n"); const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([csv], { type: "text/csv" })); a.download = `scans_${formDate}.csv`; a.click(); showFB("✓ Scans exported", t.green); }}>📤 Export</button><button style={S.sm(t.bg3, t.textMuted)} onClick={() => { if (confirm("Clear?")) setScannedItems([]) }}>Clear</button></>}</div></div>
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
          {purchaseOrders.length > 0 && (<><div style={{ display: "flex", gap: 8, marginBottom: 10, alignItems: "center" }}><span style={{ fontSize: 10, color: t.textMuted, fontWeight: 600 }}>SHIPMENT:</span><select style={S.sel} value={selectedShipment} onChange={e => setSelectedShipment(e.target.value)}><option value="all">All</option>{getShipNums().map(sn => <option key={sn} value={sn}>{sn}</option>)}</select><button style={{ padding: "6px 14px", background: "#e91e90", color: "#fff", border: "none", borderRadius: 6, fontFamily: ff, fontSize: 11, fontWeight: 600, cursor: "pointer", marginLeft: "auto" }} onClick={downloadPinkSheet}>🖨 Pink Sheet</button></div>
            <div style={S.card}><div style={S.cH}><span style={S.cL}><span style={{ color: t.greenText }}>{nM}✓</span> · <span style={{ color: t.redText }}>{nS} short</span> · <span style={{ color: t.yellowText }}>{nO} over</span></span></div>
              {comp.length === 0 ? <div style={S.empty}>No results.</div> : <div style={{ overflowX: "auto", maxHeight: 500, overflowY: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Status</th><th style={S.th}>Part #</th><th style={S.th}>SO</th><th style={S.th}>Exp</th><th style={S.th}>Scan</th><th style={S.th}>Diff</th><th style={S.th}>Notes</th></tr></thead><tbody>
                {comp.map((r, i) => { const c = sc(r.status); return <tr key={i} style={{ background: c.row }}><td style={S.td()}><span style={S.bg(c.bg, c.tx)}>{STATUS_CFG[r.status]?.label}</span></td><td style={S.td(t.textStrong)}><strong>{r.partNumber}</strong>{r.superseded && <div style={{ fontSize: 9, color: t.yellowText }}>↳{r.partOrdered}</div>}</td><td style={S.td()}>{r.shippingOrder}</td><td style={S.td()}>{r.expectedQty}</td><td style={S.td(r.scannedQty !== r.expectedQty ? c.tx : null)}>{r.scannedQty}</td><td style={S.td(r.qtyDiff > 0 ? t.yellowText : r.qtyDiff < 0 ? t.redText : t.greenText)}>{r.qtyDiff > 0 ? "+" : ""}{r.qtyDiff}</td><td style={S.td()}>{r.wrongDealer && <span style={{ ...S.bg(`${t.purple}15`, t.purpleText), marginRight: 3 }}>WD</span>}{r.superseded && <span style={{ ...S.bg(`${t.yellow}15`, t.yellowText), marginRight: 3 }}>SUP</span>}{r.dipp && <span style={S.bg(`${t.blue}15`, t.blueText)}>D</span>}</td></tr>; })}</tbody></table></div>}</div></>)}
        </>)}
        {wsTab === "wrongdealer" && <div style={S.card}><div style={S.cH}><span style={S.cL}>Wrong Dealer ({getWD().length})</span></div>{getWD().length === 0 ? <div style={S.empty}>None.</div> : <div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>Dealer</th><th style={S.th}>Belongs To</th><th style={S.th}>Contact</th><th style={S.th}>SO</th><th style={S.th}>Qty</th></tr></thead><tbody>{getWD().map(item => { const d = lookupDealer(item.dealerCode); return <tr key={item.id}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td(t.purpleText)}><strong>{item.dealerCode}</strong></td><td style={S.td()}>{d ? d.name : <em style={{ color: t.textFaint }}>Unknown</em>}</td><td style={S.td()}>{d?.email ? <span>{d.contact} <span style={{ color: t.blueText }}>{d.email}</span></span> : "—"}</td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}>{item.quantity}</td></tr>; })}</tbody></table></div>}</div>}
        {wsTab === "dipp" && <div style={S.card}><div style={S.cH}><span style={S.cL}>DIPP ({getDipp().length})</span></div>{getDipp().length === 0 ? <div style={S.empty}>None.</div> : <div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>PDC</th><th style={S.th}>SO</th><th style={S.th}>Description</th><th style={S.th}>Comments</th></tr></thead><tbody>{getDipp().map(item => <tr key={item.id}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td()}>{item.pdc}</td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}><input style={{ ...S.inp, width: "100%", minWidth: 120 }} value={dippDescriptions[item.id] || ""} onChange={e => setDippDescriptions(p => ({ ...p, [item.id]: e.target.value }))} placeholder="Part description..." /></td><td style={S.td()}><input style={{ ...S.inp, width: "100%", minWidth: 140 }} value={dippComments[item.id] || ""} onChange={e => setDippComments(p => ({ ...p, [item.id]: e.target.value }))} placeholder="Damage..." /><div style={{ display: "flex", gap: 3, flexWrap: "wrap", marginTop: 4 }}>{DIPP_PRESETS.map(p => <button key={p} style={S.sm(t.bg3, t.textMuted)} onClick={() => setDippComments(prev => ({ ...prev, [item.id]: (prev[item.id] || "") + (prev[item.id] ? ", " : "") + p }))}>{p}</button>)}</div></td></tr>)}</tbody></table></div>}</div>}
        {wsTab === "form" && (<>
          <div style={S.card}>
            <div style={S.cH}><span style={S.cL}>Woodstock Form</span></div>
            <div style={{ padding: 12, borderBottom: `1px solid ${t.border}` }}><div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}><div><label style={S.lbl}>Completed By</label><select style={S.sel} value={completedBy} onChange={e => setCompletedBy(e.target.value)}><option value="">— select —</option>{(settings.users || []).map(u => <option key={u} value={u}>{u}</option>)}</select></div><div><label style={S.lbl}>Date</label><input style={S.inp} type="date" value={formDate} onChange={e => setFormDate(e.target.value)} /></div><div><label style={S.lbl}>Phone</label><input style={{ ...S.inp, color: t.textFaint }} value={settings.phone} readOnly /></div>{purchaseOrders.length > 1 && <div><label style={S.lbl}>PO</label><select style={S.sel} value={activePO || ""} onChange={e => setActivePO(e.target.value)}><option value="__all__">All</option>{purchaseOrders.map(po => <option key={po.id} value={po.pbsPO}>{po.pbsPO}</option>)}</select></div>}</div></div>
            {shortItems.length > 0 && <div style={{ padding: 12, borderBottom: `1px solid ${t.border}` }}><label style={{ ...S.lbl, marginBottom: 6, display: "block" }}>Tote or Pallet? (Short Items)</label><div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>SO #</th><th style={S.th}>Tote / Pallet</th></tr></thead><tbody>{shortItems.map(item => { const tKey = `${item.partNumber}_${item.shippingOrder}`; return <tr key={tKey}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td()}>{item.shippingOrder}</td><td style={S.td()}><div style={{ display: "flex", gap: 4 }}>{[["T", "Tote"], ["P", "Pallet"], ["?", "Unknown"]].map(([v, lbl]) => <button key={v} onClick={() => setToteChoices(p => ({ ...p, [tKey]: p[tKey] === v ? "" : v }))} style={{ padding: "4px 10px", borderRadius: 4, border: `1px solid ${toteChoices[tKey] === v ? t.accent : t.border}`, background: toteChoices[tKey] === v ? `${t.accent}18` : t.bg2, color: toteChoices[tKey] === v ? t.accent : t.textMuted, fontFamily: ff, fontSize: 11, fontWeight: toteChoices[tKey] === v ? 700 : 400, cursor: "pointer" }}>{lbl}</button>)}</div></td></tr>; })}</tbody></table></div></div>}
            {getWD().length > 0 && <div style={{ padding: 12, borderBottom: `1px solid ${t.border}` }}><label style={{ ...S.lbl, marginBottom: 6, display: "block" }}>Wrong Dealer Options</label><div style={{ overflowX: "auto" }}><table style={S.tbl}><thead><tr><th style={S.th}>Part #</th><th style={S.th}>Dealer</th><th style={S.th}>Contacted?</th><th style={S.th}>Redirect?</th></tr></thead><tbody>{getWD().map(item => <tr key={item.id}><td style={S.td(t.textStrong)}><strong>{item.partNumber}</strong></td><td style={S.td(t.purpleText)}><strong>{item.dealerCode}</strong></td><td style={S.td()}><div style={{ display: "flex", gap: 4 }}>{[["Y", "Yes"], ["N", "No"]].map(([v, lbl]) => <button key={v} onClick={() => setWdContact(p => ({ ...p, [item.id]: p[item.id] === v ? "" : v }))} style={{ padding: "4px 10px", borderRadius: 4, border: `1px solid ${wdContact[item.id] === v ? t.accent : t.border}`, background: wdContact[item.id] === v ? `${t.accent}18` : t.bg2, color: wdContact[item.id] === v ? t.accent : t.textMuted, fontFamily: ff, fontSize: 11, fontWeight: wdContact[item.id] === v ? 700 : 400, cursor: "pointer" }}>{lbl}</button>)}</div></td><td style={S.td()}><div style={{ display: "flex", gap: 4 }}>{[["Y", "Yes"], ["N", "No"]].map(([v, lbl]) => <button key={v} onClick={() => setWdRedirect(p => ({ ...p, [item.id]: p[item.id] === v ? "" : v }))} style={{ padding: "4px 10px", borderRadius: 4, border: `1px solid ${wdRedirect[item.id] === v ? t.accent : t.border}`, background: wdRedirect[item.id] === v ? `${t.accent}18` : t.bg2, color: wdRedirect[item.id] === v ? t.accent : t.textMuted, fontFamily: ff, fontSize: 11, fontWeight: wdRedirect[item.id] === v ? 700 : 400, cursor: "pointer" }}>{lbl}</button>)}</div></td></tr>)}</tbody></table></div></div>}
            <div style={{ display: "flex", gap: 8, padding: 12, background: t.bg3, flexWrap: "wrap", alignItems: "center" }}>
              <button style={{ padding: "8px 16px", background: t.accent, color: "#fff", border: "none", borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={generatePDFHandler} disabled={pdfGenerating}>{pdfGenerating ? "⏳..." : "⬇ Generate PDF"}</button>
              {lastPdfBlob && <><button style={{ padding: "8px 16px", background: t.bg2, color: t.text, border: `1px solid ${t.border}`, borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={printPDF}>🖨 Print</button><button style={{ padding: "8px 16px", background: "#0078d4", color: "#fff", border: "none", borderRadius: 6, fontFamily: ff, fontSize: 12, fontWeight: 600, cursor: "pointer" }} onClick={emailOutlook}>✉ Outlook</button></>}
              {lastPdfBlob && <span style={{ fontSize: 10, color: t.greenText, fontWeight: 600 }}>✓ {lastPdfName}</span>}
            </div>
            {lastPdfBlob && <div style={{ padding: 12, borderTop: `1px solid ${t.border}` }}><div style={{ background: t.bg0, border: `1px solid ${t.border}`, borderRadius: 6, padding: 12, fontSize: 12 }}><div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>To: </span>{settings.wdkEmail}</div>{getCCStr() && <div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>CC: </span><span style={{ color: t.blueText }}>{getCCStr()}</span></div>}<div style={{ marginBottom: 4 }}><span style={{ color: t.textMuted }}>Subject: </span><strong>{emailSubject}</strong></div><div><span style={{ color: t.textMuted }}>Attach: </span><span style={{ color: t.greenText }}>📎 {lastPdfName}</span></div></div></div>}
            <div style={{ padding: 12, display: "flex", gap: 20, flexWrap: "wrap", borderTop: `1px solid ${t.border}` }}>{[["Shorts", shortItems.length, t.red], ["WD", stats.wd, t.purple], ["DIPP", stats.dipp, t.blue], ["Over", nO, t.yellow], ["Match", nM, t.green]].map(([l, n, c]) => <div key={l} style={{ display: "flex", alignItems: "center", gap: 6 }}><span style={{ ...S.dot(c) }}></span><span style={{ fontSize: 12, color: t.textMuted }}>{l}:</span><strong style={{ color: c }}>{n}</strong></div>)}</div>
          </div>
        </>)}
      </div>
    </div>
  );
}
