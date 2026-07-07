using System;
using System.Collections.Generic;
using System.Linq;

namespace CashDrawer.Shared.Utils
{
    /// <summary>One parsed transaction/BOD/EOD row used for day-summary math.</summary>
    public class DaySummaryLine
    {
        public DateTime Timestamp { get; set; }
        public string DocumentType { get; set; } = "";
        public string ServerID { get; set; } = "";
        public decimal Total { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
    }

    public class ServerTotal
    {
        public decimal In { get; set; }
        public decimal Out { get; set; }
        public int Count { get; set; }
    }

    public class DaySummaryResult
    {
        public string BusinessDate { get; set; } = "";
        public DateTime SessionStart { get; set; }
        public bool FoundBod { get; set; }
        public decimal BodFloat { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal TotalTransactions { get; set; }
        public int TransactionCount { get; set; }
        public Dictionary<string, ServerTotal> ServerBreakdown { get; set; } = new();

        /// <summary>Number of BOD entries found in the current session. >1 means the
        /// cashier did Beginning-of-Day more than once (a re-do/correction).</summary>
        public int BodCount { get; set; }

        public decimal ExpectedTotal => BodFloat + TotalTransactions;
    }

    /// <summary>
    /// Computes the End-of-Day summary for a single business day.
    ///
    /// This shop removes the drawer cash every night whether or not an EOD was run,
    /// so each calendar day stands on its own - nothing carries over. The business
    /// day is the calendar date of the most recent BOD; the summary covers only that
    /// date's transactions, so a forgotten EOD on an earlier day never bleeds into a
    /// later day's total.
    ///
    ///  - Fixes "short the BOD balance": the float comes from the BOD entry for that
    ///    day (the most recent one that day if it was redone), not a lookup keyed to
    ///    today's date - so an EOD run after the BOD's day still finds the float.
    ///  - Duplicate/corrective BOD on the SAME day: treated as a correction. The
    ///    earliest BOD that day anchors the start (no sales dropped) and the latest
    ///    BOD that day supplies the float (the corrected amount).
    ///  - Re-running EOD produces the same totals (EOD entries don't change scope).
    /// </summary>
    public static class DaySummaryCalculator
    {
        /// <param name="lines">All candidate lines (typically yesterday + today).</param>
        /// <param name="configuredBodFloatForDate">
        /// Returns the stored BOD float for a "yyyy-MM-dd" business date, or null.
        /// </param>
        /// <param name="fallbackDate">
        /// Business date to assume when no BOD marker exists in the lines.
        /// </param>
        public static DaySummaryResult Compute(
            IEnumerable<DaySummaryLine> lines,
            Func<string, decimal?> configuredBodFloatForDate,
            DateTime fallbackDate)
        {
            var ordered = lines
                .Where(l => l != null)
                .OrderBy(l => l.Timestamp)
                .ToList();

            var result = new DaySummaryResult();

            var latestBod = ordered
                .Where(l => IsType(l.DocumentType, "BOD"))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            DateTime businessDate;

            if (latestBod != null)
            {
                // The business day is the calendar date of the most recent BOD.
                businessDate = latestBod.Timestamp.Date;

                // All BODs done ON that date (duplicates/corrections).
                var sameDayBods = ordered
                    .Where(l => IsType(l.DocumentType, "BOD") && l.Timestamp.Date == businessDate)
                    .OrderBy(l => l.Timestamp)
                    .ToList();

                result.FoundBod = true;
                result.BodCount = sameDayBods.Count;
                result.SessionStart = sameDayBods.First().Timestamp;     // earliest BOD that day
                result.BusinessDate = businessDate.ToString("yyyy-MM-dd");

                // Float: the last BOD entered that day (a correction wins); fall back
                // to the configured float for the business date if the line has none.
                var latestSameDayBod = sameDayBods.Last();
                result.BodFloat = latestSameDayBod.Total != 0m
                    ? latestSameDayBod.Total
                    : (configuredBodFloatForDate(result.BusinessDate) ?? 0m);
            }
            else
            {
                // No BOD marker at all - fall back to the whole fallback calendar day.
                businessDate = fallbackDate.Date;
                result.FoundBod = false;
                result.BodCount = 0;
                result.SessionStart = businessDate;
                result.BusinessDate = businessDate.ToString("yyyy-MM-dd");
                result.BodFloat = configuredBodFloatForDate(result.BusinessDate) ?? 0m;
            }

            foreach (var l in ordered)
            {
                // Per-day: only the business date's activity (cash is reset nightly).
                if (l.Timestamp.Date != businessDate) continue;
                if (l.Timestamp < result.SessionStart) continue;
                if (IsType(l.DocumentType, "BOD") || IsType(l.DocumentType, "EOD")) continue;

                result.TotalTransactions += l.Total;
                result.TotalIn += l.AmountIn;
                result.TotalOut += l.AmountOut;
                result.TransactionCount++;

                var key = string.IsNullOrWhiteSpace(l.ServerID) ? "Unknown" : l.ServerID.Trim();
                if (!result.ServerBreakdown.TryGetValue(key, out var st))
                {
                    st = new ServerTotal();
                    result.ServerBreakdown[key] = st;
                }
                st.In += l.AmountIn;
                st.Out += l.AmountOut;
                st.Count++;
            }

            return result;
        }

        private static bool IsType(string? docType, string expected) =>
            string.Equals(docType?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
