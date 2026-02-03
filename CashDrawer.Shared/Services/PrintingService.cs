using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashDrawer.Shared.Services
{
    public class PrintingService
    {
        private readonly ILogger<PrintingService>? _logger;
        private readonly PrintingConfig _config;
        private string? _printContent;
        private Image? _logoImage;

        public PrintingService(ILogger<PrintingService>? logger, IOptions<PrintingConfig>? config)
        {
            _logger = logger;
            _config = config?.Value ?? new PrintingConfig();
        }

        public PrintingService() : this(null, null) { }
        
        /// <summary>
        /// Create PrintingService with a specific logo path
        /// </summary>
        public PrintingService(string logoPath) : this(null, null)
        {
            _config = new PrintingConfig { LogoPath = logoPath };
        }

        public bool PrintTransaction(TransactionPrintData data)
        {
            try
            {
                // Load logo if configured
                Image? logo = null;
                if (!string.IsNullOrEmpty(_config.LogoPath) && File.Exists(_config.LogoPath))
                {
                    try
                    {
                        logo = Image.FromFile(_config.LogoPath);
                        _logoImage = logo;
                    }
                    catch
                    {
                        // Ignore logo errors, continue without it
                    }
                }
                
                var content = new StringBuilder();
                
                // Add space for logo if present
                if (_logoImage != null)
                {
                    content.AppendLine();
                    content.AppendLine();
                    content.AppendLine();
                }
                
                // Special header for SAFE DROP
                if (data.TransactionType == "SAFE DROP")
                {
                    content.AppendLine("═══════════════════════════════════");
                    content.AppendLine("═══════════════════════════════════");
                    content.AppendLine();
                    content.AppendLine("         *** SAFE DROP ***");
                    content.AppendLine();
                    content.AppendLine("═══════════════════════════════════");
                    content.AppendLine("═══════════════════════════════════");
                }
                else
                {
                    content.AppendLine("═══════════════════════════════════");
                    content.AppendLine("    CASH DRAWER TRANSACTION");
                    content.AppendLine("═══════════════════════════════════");
                }
                
                content.AppendLine();
                content.AppendLine($"Date/Time: {data.Timestamp:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine($"Cashier: {data.Username}");
                content.AppendLine($"Transaction ID: {data.TransactionId}");
                content.AppendLine();
                
                // Don't repeat "Type: SAFE DROP" since we already have prominent header
                if (data.TransactionType != "SAFE DROP")
                {
                    content.AppendLine($"Type: {data.TransactionType}");
                }
                
                if (!string.IsNullOrWhiteSpace(data.Invoice))
                {
                    content.AppendLine($"Invoice: {data.Invoice}");
                }
                
                if (data.Amount.HasValue)
                {
                    content.AppendLine($"Amount: ${data.Amount:F2}");
                }
                
                if (!string.IsNullOrWhiteSpace(data.Reason))
                {
                    content.AppendLine($"Reason: {data.Reason}");
                }

                if (!string.IsNullOrWhiteSpace(data.RecipientName))
                {
                    content.AppendLine($"For: {data.RecipientName}");
                }
                
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");
                
                if (!string.IsNullOrWhiteSpace(data.Notes))
                {
                    content.AppendLine();
                    content.AppendLine("Notes:");
                    content.AppendLine(data.Notes);
                }

                var result = PrintSilently(content.ToString());
                
                // Cleanup logo
                _logoImage = null;
                logo?.Dispose();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print transaction");
                _logoImage = null;
                return false;
            }
        }

        public bool PrintSafeDropReceipt(SafeDropPrintData data)
        {
            try
            {
                var content = new StringBuilder();
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine("    CASH DRAWER TRANSACTION");
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine($"Date/Time: {data.Timestamp:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine($"Cashier: {data.Username}");
                content.AppendLine($"Transaction ID: {data.TransactionId}");
                content.AppendLine();
                content.AppendLine($"Type: {data.TransactionType}");
                
                if (!string.IsNullOrWhiteSpace(data.Invoice))
                {
                    content.AppendLine($"Invoice: {data.Invoice}");
                }
                
                if (data.Amount.HasValue)
                {
                    content.AppendLine($"Amount: ${data.Amount:F2}");
                }
                
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine();
                content.AppendLine("   ███████╗ █████╗ ███████╗███████╗");
                content.AppendLine("   ██╔════╝██╔══██╗██╔════╝██╔════╝");
                content.AppendLine("   ███████╗███████║█████╗  █████╗  ");
                content.AppendLine("   ╚════██║██╔══██║██╔══╝  ██╔══╝  ");
                content.AppendLine("   ███████║██║  ██║██║     ███████╗");
                content.AppendLine("   ╚══════╝╚═╝  ╚═╝╚═╝     ╚══════╝");
                content.AppendLine();
                content.AppendLine("   ██████╗ ██████╗  ██████╗ ██████╗ ");
                content.AppendLine("   ██╔══██╗██╔══██╗██╔═══██╗██╔══██╗");
                content.AppendLine("   ██║  ██║██████╔╝██║   ██║██████╔╝");
                content.AppendLine("   ██║  ██║██╔══██╗██║   ██║██╔═══╝ ");
                content.AppendLine("   ██████╔╝██║  ██║╚██████╔╝██║     ");
                content.AppendLine("   ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚═╝     ");
                content.AppendLine();
                content.AppendLine();
                content.AppendLine($"        Amount: ${data.Amount:F2}");
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");

                return PrintSilently(content.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print safe drop receipt");
                return false;
            }
        }

        public bool PrintPettyCashSlip(PettyCashData data, Image? logo = null)
        {
            try
            {
                _logoImage = logo;
                
                var content = new StringBuilder();
                
                // Logo will be printed separately in PrintPage event
                if (logo != null)
                {
                    content.AppendLine(); // Space for logo
                    content.AppendLine();
                    content.AppendLine();
                }
                
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine("       PETTY CASH SLIP");
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine($"Date: {data.Date:yyyy-MM-dd}");
                content.AppendLine($"Time: {data.Date:HH:mm:ss}");
                content.AppendLine();
                content.AppendLine($"Recipient: {data.RecipientName}");
                content.AppendLine($"Amount: ${data.Amount:F2}");
                content.AppendLine($"Invoice #: {data.Invoice}");
                content.AppendLine();
                content.AppendLine("Purpose:");
                content.AppendLine(data.Purpose);
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine($"Issued by: {data.IssuedBy}");
                content.AppendLine();
                content.AppendLine("Signature: _____________________");
                content.AppendLine();
                content.AppendLine("Date: _____________________");
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");

                var result = PrintSilently(content.ToString());
                _logoImage = null;
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print petty cash slip");
                _logoImage = null;
                return false;
            }
        }

        public bool PrintEndOfDaySummary(EndOfDayData data)
        {
            try
            {
                var content = new StringBuilder();
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine("     END OF DAY SUMMARY");
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine($"Date: {data.Date:yyyy-MM-dd}");
                content.AppendLine($"Cashier: {data.CashierName}");
                content.AppendLine();
                content.AppendLine("─── STARTING FLOAT ───");
                content.AppendLine($"Opening: ${data.StartingFloat:F2}");
                PrintDenominations(content, data.StartingDenominations);
                content.AppendLine();
                content.AppendLine("─── TRANSACTIONS ───");
                content.AppendLine($"Total Transactions: {data.TransactionCount}");
                content.AppendLine($"Total Sales: ${data.TotalSales:F2}");
                content.AppendLine($"Total Petty Cash: ${data.TotalPettyCash:F2}");
                content.AppendLine($"Net Change: ${data.NetChange:F2}");
                content.AppendLine();
                content.AppendLine("─── ENDING COUNT ───");
                content.AppendLine($"Expected: ${data.ExpectedTotal:F2}");
                content.AppendLine($"Actual Count: ${data.ActualCount:F2}");
                PrintDenominations(content, data.EndingDenominations);
                content.AppendLine();
                content.AppendLine("─── VARIANCE ───");
                
                var variance = data.ActualCount - data.ExpectedTotal;
                if (Math.Abs(variance) < 0.01m)
                {
                    content.AppendLine("✓ BALANCED");
                }
                else if (variance > 0)
                {
                    content.AppendLine($"OVERAGE: ${variance:F2}");
                }
                else
                {
                    content.AppendLine($"SHORTAGE: ${Math.Abs(variance):F2}");
                }

                if (data.Adjustments != null && data.Adjustments.Count > 0)
                {
                    content.AppendLine();
                    content.AppendLine("─── ADJUSTMENTS ───");
                    foreach (var adj in data.Adjustments)
                    {
                        content.AppendLine($"** {adj.Reason}: ${adj.Amount:F2} **");
                    }
                }

                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");
                content.AppendLine();
                content.AppendLine("Verified by: _____________________");
                content.AppendLine();
                content.AppendLine("Manager: _____________________");
                content.AppendLine();
                content.AppendLine("═══════════════════════════════════");

                return PrintSilently(content.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print end of day summary");
                return false;
            }
        }

        private void PrintDenominations(StringBuilder content, Dictionary<string, int>? denoms)
        {
            if (denoms == null || denoms.Count == 0) return;

            var coins = new[] { "0.05", "0.10", "0.25", "1.00", "2.00" };
            var bills = new[] { "5", "10", "20", "50", "100" };

            if (denoms.Any(d => coins.Contains(d.Key)))
            {
                content.AppendLine("  Coins:");
                foreach (var coin in coins)
                {
                    if (denoms.TryGetValue(coin, out var count) && count > 0)
                    {
                        var value = decimal.Parse(coin);
                        content.AppendLine($"    ${value:F2} x {count,3} = ${value * count:F2}");
                    }
                }
            }

            if (denoms.Any(d => bills.Contains(d.Key)))
            {
                content.AppendLine("  Bills:");
                foreach (var bill in bills)
                {
                    if (denoms.TryGetValue(bill, out var count) && count > 0)
                    {
                        var value = decimal.Parse(bill);
                        content.AppendLine($"    ${value,3:F0} x {count,3} = ${value * count:F2}");
                    }
                }
            }
        }

        private bool PrintSilently(string content)
        {
            try
            {
                _printContent = content;

                var printDoc = new PrintDocument();
                
                // Use default printer or configured printer
                if (!string.IsNullOrWhiteSpace(_config.PrinterName))
                {
                    printDoc.PrinterSettings.PrinterName = _config.PrinterName;
                }

                printDoc.PrintPage += PrintPage;
                printDoc.Print(); // Silent print, no dialog

                _logger?.LogInformation("Print job sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print silently");
                return false;
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                var graphics = e.Graphics!;
                var font = new Font("Courier New", 10);
                var brush = Brushes.Black;
                float y = 50;

                // Print logo if provided
                if (_logoImage != null)
                {
                    var logoWidth = 150;
                    var logoHeight = (int)(_logoImage.Height * (logoWidth / (float)_logoImage.Width));
                    var logoX = (e.PageBounds.Width - logoWidth) / 2;
                    graphics.DrawImage(_logoImage, logoX, (int)y, logoWidth, logoHeight);
                    y += logoHeight + 20;
                }

                // Print content
                if (_printContent != null)
                {
                    var lines = _printContent.Split('\n');
                    foreach (var line in lines)
                    {
                        graphics.DrawString(line, font, brush, 50, y);
                        y += font.GetHeight(graphics);
                    }
                }

                e.HasMorePages = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in PrintPage event");
            }
        }
    }

    public class PrintingConfig
    {
        public string PrinterName { get; set; } = ""; // Empty = use default
        public bool AutoPrint { get; set; } = true;
        public string LogoPath { get; set; } = ""; // Path to logo image (PNG, JPG) for receipts
    }

    public class TransactionPrintData
    {
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public string? Invoice { get; set; }
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
        public string? RecipientName { get; set; }
        public string? Notes { get; set; }
    }

    public class SafeDropPrintData
    {
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public string? Invoice { get; set; }
        public decimal? Amount { get; set; }
    }

    public class PettyCashData
    {
        public DateTime Date { get; set; }
        public string RecipientName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Invoice { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string IssuedBy { get; set; } = "";
    }

    public class EndOfDayData
    {
        public DateTime Date { get; set; }
        public string CashierName { get; set; } = "";
        public decimal StartingFloat { get; set; }
        public Dictionary<string, int>? StartingDenominations { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPettyCash { get; set; }
        public decimal NetChange { get; set; }
        public decimal ExpectedTotal { get; set; }
        public decimal ActualCount { get; set; }
        public Dictionary<string, int>? EndingDenominations { get; set; }
        public List<Adjustment>? Adjustments { get; set; }
    }

    public class Adjustment
    {
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string EnteredBy { get; set; } = "";
    }
}
