using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

namespace CashDrawer.Shared.Utils
{
    /// <summary>
    /// Prints a list of pre-formatted text lines across as many pages as needed.
    ///
    /// The older EOD print drew one big string into MarginBounds in a single
    /// DrawString call, which silently clipped anything past the bottom of page 1 —
    /// fine for a totals-only summary, wrong as soon as a transaction list is added.
    /// This walks the lines, tracks how many fit per page, and sets HasMorePages.
    /// </summary>
    public static class TextReportPrinter
    {
        /// <param name="lines">Pre-formatted lines, one per printed row.</param>
        /// <param name="repeatHeader">
        /// Lines re-drawn at the top of every page after the first (e.g. column
        /// headers), so page 3 of a transaction list is still readable on its own.
        /// </param>
        /// <param name="documentName">Shown in the printer queue.</param>
        public static void Print(
            IEnumerable<string> lines,
            IEnumerable<string>? repeatHeader = null,
            string documentName = "CashDrawer Report",
            float fontSize = 10f)
        {
            using var doc = CreateDocument(lines, repeatHeader, documentName, fontSize);
            doc.Print();
        }

        /// <summary>
        /// Builds the paginated document without printing it. Split out from
        /// <see cref="Print"/> so pagination can be exercised headlessly against a
        /// PreviewPrintController (no physical printer, no paper).
        /// </summary>
        public static PrintDocument CreateDocument(
            IEnumerable<string> lines,
            IEnumerable<string>? repeatHeader = null,
            string documentName = "CashDrawer Report",
            float fontSize = 10f)
        {
            var allLines = (lines ?? Enumerable.Empty<string>()).ToList();
            var header = (repeatHeader ?? Enumerable.Empty<string>()).ToList();

            var doc = new PrintDocument();
            doc.DocumentName = documentName;

            var font = new Font("Courier New", fontSize);
            int next = 0;      // index of the next line to draw
            int pageNumber = 0;

            doc.PrintPage += (s, ev) =>
            {
                if (ev.Graphics == null) return;

                pageNumber++;
                float lineHeight = font.GetHeight(ev.Graphics);
                float y = ev.MarginBounds.Top;
                float bottom = ev.MarginBounds.Bottom;

                // Repeat the column header on continuation pages only — page 1
                // already carries it inline as part of the report body.
                if (pageNumber > 1 && header.Count > 0)
                {
                    foreach (var h in header)
                    {
                        ev.Graphics.DrawString(h, font, Brushes.Black, ev.MarginBounds.Left, y);
                        y += lineHeight;
                    }
                }

                while (next < allLines.Count && y + lineHeight <= bottom)
                {
                    ev.Graphics.DrawString(allLines[next], font, Brushes.Black, ev.MarginBounds.Left, y);
                    y += lineHeight;
                    next++;
                }

                ev.HasMorePages = next < allLines.Count;
            };

            // Reset per-run so the same document can be printed (or previewed then
            // printed) more than once without resuming mid-report.
            doc.BeginPrint += (s, ev) =>
            {
                next = 0;
                pageNumber = 0;
            };

            doc.Disposed += (s, ev) => font.Dispose();

            return doc;
        }
    }
}
