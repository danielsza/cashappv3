using System;
using System.Globalization;

namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// Transaction record
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Unique transaction ID for sync deduplication (ServerID-Timestamp-Random)
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ServerID { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public decimal Total { get; set; } = 0;
        public decimal AmountIn { get; set; } = 0;
        public decimal AmountOut { get; set; } = 0;
        
        /// <summary>
        /// Generate a unique transaction ID
        /// </summary>
        public void GenerateId()
        {
            if (string.IsNullOrEmpty(TransactionId))
            {
                TransactionId = $"{ServerID}-{Timestamp:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            }
        }
        
        /// <summary>
        /// Strip the field delimiter and any line breaks from a free-text field so
        /// user input (e.g. an invoice number or petty-cash reason containing "|")
        /// can never shift the pipe-delimited columns and corrupt the log line.
        /// </summary>
        private static string Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace('|', '/')
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        public override string ToString()
        {
            // Money is always written with InvariantCulture so a machine whose
            // locale uses a comma decimal separator can't write "50,00" and then
            // fail to parse it back as invariant "50.00".
            return $"{Sanitize(TransactionId)} | {Timestamp:yyyy-MM-dd HH:mm:ss} | {Sanitize(ServerID)} | {Sanitize(Username)} | " +
                   $"{Sanitize(Reason)} | {Sanitize(DocumentType)} | {Sanitize(DocumentNumber)} | " +
                   $"Total: {Total.ToString("F2", CultureInfo.InvariantCulture)} | " +
                   $"IN: {AmountIn.ToString("F2", CultureInfo.InvariantCulture)} | " +
                   $"OUT: {AmountOut.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        
        /// <summary>
        /// Parse a transaction from log line
        /// </summary>
        public static Transaction? FromLogLine(string line)
        {
            try
            {
                var parts = line.Split('|');
                if (parts.Length < 10) return null;
                
                var txn = new Transaction
                {
                    TransactionId = parts[0].Trim(),
                    ServerID = parts[2].Trim(),
                    Username = parts[3].Trim(),
                    Reason = parts[4].Trim(),
                    DocumentType = parts[5].Trim(),
                    DocumentNumber = parts[6].Trim()
                };
                
                // Parse timestamp
                if (DateTime.TryParse(parts[1].Trim(), out var ts))
                    txn.Timestamp = ts;
                
                // Parse Total: X.XX  (InvariantCulture to match how it was written)
                var totalPart = parts[7].Trim();
                if (totalPart.StartsWith("Total:"))
                {
                    var val = totalPart.Substring(6).Trim();
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                        txn.Total = total;
                }

                // Parse IN: X.XX
                var inPart = parts[8].Trim();
                if (inPart.StartsWith("IN:"))
                {
                    var val = inPart.Substring(3).Trim();
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var amtIn))
                        txn.AmountIn = amtIn;
                }

                // Parse OUT: X.XX
                var outPart = parts[9].Trim();
                if (outPart.StartsWith("OUT:"))
                {
                    var val = outPart.Substring(4).Trim();
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var amtOut))
                        txn.AmountOut = amtOut;
                }

                return txn;
            }
            catch
            {
                return null;
            }
        }
    }
}
