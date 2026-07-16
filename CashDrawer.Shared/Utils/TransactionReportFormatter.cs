using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CashDrawer.Shared.Models;

namespace CashDrawer.Shared.Utils
{
    /// <summary>
    /// Renders transactions as a fixed-width, one-line-per-transaction table for
    /// printing (EOD summary and the NetworkAdmin log viewer both use this, so a
    /// printed row looks the same wherever it came from).
    ///
    /// Widths assume a monospaced font; the whole table is under 70 columns so it
    /// fits portrait letter at Courier New 10 without wrapping.
    /// </summary>
    public static class TransactionReportFormatter
    {
        // Column widths. Amount is right-aligned; the rest are left-aligned.
        private const int TimeWidth = 5;   // HH:mm
        private const int DateWidth = 10;  // yyyy-MM-dd
        private const int TypeWidth = 11;
        private const int DocWidth = 12;
        private const int UserWidth = 10;
        private const int AmountWidth = 11; // includes the "$"

        /// <summary>Column header, followed by a rule. Repeat at the top of each page.</summary>
        public static IEnumerable<string> Header(bool includeDate = false)
        {
            var sb = new StringBuilder();
            if (includeDate) sb.Append("DATE".PadRight(DateWidth)).Append(' ');
            sb.Append("TIME".PadRight(TimeWidth)).Append(' ');
            sb.Append("TYPE".PadRight(TypeWidth)).Append(' ');
            sb.Append("DOC #".PadRight(DocWidth)).Append(' ');
            sb.Append("USER".PadRight(UserWidth)).Append(' ');
            sb.Append("AMOUNT".PadLeft(AmountWidth));

            var header = sb.ToString();
            yield return header;
            yield return new string('-', header.Length);
        }

        /// <summary>One transaction as a single fixed-width row.</summary>
        public static string Row(Transaction txn, bool includeDate = false)
        {
            if (txn == null) throw new ArgumentNullException(nameof(txn));

            var sb = new StringBuilder();
            if (includeDate)
                sb.Append(txn.Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture).PadRight(DateWidth)).Append(' ');
            sb.Append(txn.Timestamp.ToString("HH:mm", CultureInfo.InvariantCulture).PadRight(TimeWidth)).Append(' ');
            sb.Append(Fit(txn.DocumentType, TypeWidth)).Append(' ');
            sb.Append(Fit(txn.DocumentNumber, DocWidth)).Append(' ');
            sb.Append(Fit(txn.Username, UserWidth)).Append(' ');

            // Money uses InvariantCulture to match how it was written to the log —
            // a comma-decimal locale must not render "50,00" on the printout.
            var amount = "$" + txn.Total.ToString("F2", CultureInfo.InvariantCulture);
            sb.Append(amount.PadLeft(AmountWidth));

            return sb.ToString();
        }

        /// <summary>
        /// Header + a row per transaction, oldest first, plus a count/total footer.
        /// Returns an empty section (just a "none" line) when there are no rows, so
        /// the printout never leaves the reader wondering if data was dropped.
        /// </summary>
        public static IEnumerable<string> Table(IEnumerable<Transaction> transactions, bool includeDate = false)
        {
            var rows = (transactions ?? Enumerable.Empty<Transaction>())
                .Where(t => t != null)
                .OrderBy(t => t.Timestamp)
                .ToList();

            if (rows.Count == 0)
            {
                yield return "(no transactions)";
                yield break;
            }

            foreach (var line in Header(includeDate))
                yield return line;

            foreach (var txn in rows)
                yield return Row(txn, includeDate);

            var total = rows.Sum(t => t.Total);
            var width = Header(includeDate).First().Length;
            yield return new string('-', width);
            yield return ($"{rows.Count} transaction(s)").PadRight(width - AmountWidth)
                         + ("$" + total.ToString("F2", CultureInfo.InvariantCulture)).PadLeft(AmountWidth);
        }

        /// <summary>Pad to width, truncating anything longer so columns never shift.</summary>
        private static string Fit(string? value, int width)
        {
            var v = (value ?? string.Empty).Trim();
            if (v.Length > width) v = v.Substring(0, width);
            return v.PadRight(width);
        }
    }
}
