using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class EODCountForm : Form
    {
        private Dictionary<string, NumericUpDown> _denominationControls = new();
        private Dictionary<string, Label> _totalLabels = new();
        private Label _expectedLabel = null!;
        private Label _actualLabel = null!;
        private Label _varianceLabel = null!;
        private Panel _variancePanel = null!;
        private Button _addAdjustmentButton = null!;
        private Button _printSummaryButton = null!;
        private Button _closeDayButton = null!;
        private ListBox _adjustmentsList = null!;

        private readonly Dictionary<string, decimal> _denominations = new()
        {
            { "Nickels", 0.05m },
            { "Dimes", 0.10m },
            { "Quarters", 0.25m },
            { "Loonies", 1.00m },
            { "Toonies", 2.00m },
            { "$5", 5m },
            { "$10", 10m },
            { "$20", 20m },
            { "$50", 50m },
            { "$100", 100m }
        };

        public decimal ExpectedTotal { get; set; }
        public decimal SafeDropTotal { get; set; }
        public decimal AdjustedExpected => ExpectedTotal - SafeDropTotal;
        public decimal ActualTotal { get; private set; }
        public decimal Variance => ActualTotal - AdjustedExpected;
        public Dictionary<string, int> DenominationCounts { get; private set; } = new();
        public List<CashAdjustment> Adjustments { get; private set; } = new();
        public List<SafeDropInfo> SafeDrops { get; set; } = new();

        public EODCountForm(decimal expectedTotal, decimal safeDropTotal, List<SafeDropInfo> safeDrops)
        {
            ExpectedTotal = expectedTotal;
            SafeDropTotal = safeDropTotal;
            SafeDrops = safeDrops;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "End of Day - Cash Count";
            this.Size = new Size(700, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "📊 End of Day - Cash Count",
                Location = new Point(20, y),
                Size = new Size(640, 35),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panel.Controls.Add(titleLabel);
            y += 45;

            // Expected total section
            var expectedPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(640, SafeDropTotal > 0 ? 120 : 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 248, 255)
            };

            var expectedLabelText = new Label
            {
                Text = "BOD + Transactions:",
                Location = new Point(10, 10),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleRight
            };
            expectedPanel.Controls.Add(expectedLabelText);

            var originalExpectedLabel = new Label
            {
                Text = $"${ExpectedTotal:F2}",
                Location = new Point(220, 10),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            expectedPanel.Controls.Add(originalExpectedLabel);

            if (SafeDropTotal > 0)
            {
                var safeDropLabelText = new Label
                {
                    Text = "Safe Drops:",
                    Location = new Point(10, 40),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.FromArgb(192, 0, 0)
                };
                expectedPanel.Controls.Add(safeDropLabelText);

                var safeDropAmountLabel = new Label
                {
                    Text = $"-${SafeDropTotal:F2}",
                    Location = new Point(220, 40),
                    Size = new Size(150, 25),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(192, 0, 0)
                };
                expectedPanel.Controls.Add(safeDropAmountLabel);

                var separator = new Label
                {
                    Location = new Point(210, 70),
                    Size = new Size(160, 1),
                    BorderStyle = BorderStyle.Fixed3D
                };
                expectedPanel.Controls.Add(separator);

                var adjustedLabelText = new Label
                {
                    Text = "Expected in Drawer:",
                    Location = new Point(10, 80),
                    Size = new Size(200, 30),
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight
                };
                expectedPanel.Controls.Add(adjustedLabelText);

                _expectedLabel = new Label
                {
                    Text = $"${AdjustedExpected:F2}",
                    Location = new Point(220, 80),
                    Size = new Size(200, 30),
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 120, 215)
                };
                expectedPanel.Controls.Add(_expectedLabel);

                // Safe drop details link
                var detailsLink = new LinkLabel
                {
                    Text = $"View {SafeDrops.Count} safe drop(s) →",
                    Location = new Point(390, 85),
                    Size = new Size(200, 20),
                    Font = new Font("Segoe UI", 9)
                };
                detailsLink.LinkClicked += ShowSafeDropDetails;
                expectedPanel.Controls.Add(detailsLink);
            }
            else
            {
                _expectedLabel = new Label
                {
                    Text = $"${ExpectedTotal:F2}",
                    Location = new Point(220, 10),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                expectedPanel.Controls.Add(_expectedLabel);
            }

            panel.Controls.Add(expectedPanel);
            y += SafeDropTotal > 0 ? 130 : 70;

            var instructionLabel = new Label
            {
                Text = "Count your actual cash and enter the quantity for each denomination.",
                Location = new Point(20, y),
                Size = new Size(640, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(instructionLabel);
            y += 40;

            // Coins Section
            var coinsLabel = new Label
            {
                Text = "COINS",
                Location = new Point(20, y),
                Size = new Size(640, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            panel.Controls.Add(coinsLabel);
            y += 35;

            y = CreateDenominationRow(panel, y, "Nickels", 0.05m);
            y = CreateDenominationRow(panel, y, "Dimes", 0.10m);
            y = CreateDenominationRow(panel, y, "Quarters", 0.25m);
            y = CreateDenominationRow(panel, y, "Loonies", 1.00m);
            y = CreateDenominationRow(panel, y, "Toonies", 2.00m);
            y += 20;

            // Bills Section
            var billsLabel = new Label
            {
                Text = "BILLS",
                Location = new Point(20, y),
                Size = new Size(640, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            panel.Controls.Add(billsLabel);
            y += 35;

            y = CreateDenominationRow(panel, y, "$5", 5m);
            y = CreateDenominationRow(panel, y, "$10", 10m);
            y = CreateDenominationRow(panel, y, "$20", 20m);
            y = CreateDenominationRow(panel, y, "$50", 50m);
            y = CreateDenominationRow(panel, y, "$100", 100m);
            y += 30;

            // Actual Total
            var separator1 = new Label
            {
                Location = new Point(20, y),
                Size = new Size(640, 2),
                BorderStyle = BorderStyle.Fixed3D
            };
            panel.Controls.Add(separator1);
            y += 15;

            var actualLabelText = new Label
            {
                Text = "ACTUAL COUNT:",
                Location = new Point(20, y),
                Size = new Size(350, 35),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(actualLabelText);

            _actualLabel = new Label
            {
                Text = "$0.00",
                Location = new Point(380, y),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panel.Controls.Add(_actualLabel);
            y += 50;

            // Variance Panel
            _variancePanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(640, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240),
                Visible = false
            };

            var varianceTitleLabel = new Label
            {
                Text = "VARIANCE:",
                Location = new Point(20, 20),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            _variancePanel.Controls.Add(varianceTitleLabel);

            _varianceLabel = new Label
            {
                Text = "$0.00",
                Location = new Point(280, 15),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _variancePanel.Controls.Add(_varianceLabel);

            _addAdjustmentButton = new Button
            {
                Text = "📝 Add Adjustment",
                Location = new Point(280, 60),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Visible = false
            };
            _addAdjustmentButton.Click += AddAdjustmentButton_Click;
            _variancePanel.Controls.Add(_addAdjustmentButton);

            panel.Controls.Add(_variancePanel);
            y += 110;

            // Adjustments list
            var adjustmentsLabel = new Label
            {
                Text = "ADJUSTMENTS:",
                Location = new Point(20, y),
                Size = new Size(640, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Visible = false
            };
            panel.Controls.Add(adjustmentsLabel);
            y += 30;

            _adjustmentsList = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(640, 100),
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Red,
                Visible = false
            };
            panel.Controls.Add(_adjustmentsList);
            y += 110;

            // Buttons
            _printSummaryButton = new Button
            {
                Text = "🖨️ Print Summary",
                Location = new Point(120, y),
                Size = new Size(150, 45),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            _printSummaryButton.Click += PrintSummaryButton_Click;
            panel.Controls.Add(_printSummaryButton);

            _closeDayButton = new Button
            {
                Text = "✅ Close Day",
                Location = new Point(280, y),
                Size = new Size(140, 45),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            _closeDayButton.Click += CloseDayButton_Click;
            panel.Controls.Add(_closeDayButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(430, y),
                Size = new Size(100, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            panel.Controls.Add(cancelButton);

            this.Controls.Add(panel);
        }

        private int CreateDenominationRow(Panel panel, int y, string name, decimal value)
        {
            var nameLabel = new Label
            {
                Text = $"{name} (${value:F2})",
                Location = new Point(40, y + 3),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(nameLabel);

            var countInput = new NumericUpDown
            {
                Location = new Point(200, y),
                Width = 80,
                Maximum = 9999,
                Font = new Font("Segoe UI", 10),
                TextAlign = HorizontalAlignment.Center
            };
            countInput.ValueChanged += (s, e) => RecalculateTotal();
            _denominationControls[name] = countInput;
            panel.Controls.Add(countInput);

            var timesLabel = new Label
            {
                Text = "×",
                Location = new Point(290, y + 3),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(timesLabel);

            var equalsLabel = new Label
            {
                Text = "=",
                Location = new Point(320, y + 3),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(equalsLabel);

            var totalLabel = new Label
            {
                Text = "$0.00",
                Location = new Point(350, y + 3),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            _totalLabels[name] = totalLabel;
            panel.Controls.Add(totalLabel);

            return y + 35;
        }

        private void RecalculateTotal()
        {
            decimal total = 0;
            bool hasValues = false;

            foreach (var kvp in _denominationControls)
            {
                var name = kvp.Key;
                var control = kvp.Value;
                var count = (int)control.Value;

                if (count > 0) hasValues = true;

                var value = _denominations[name];
                var lineTotal = count * value;
                total += lineTotal;

                _totalLabels[name].Text = $"${lineTotal:F2}";
            }

            ActualTotal = total;
            _actualLabel.Text = $"${ActualTotal:F2}";

            // Calculate and display variance
            if (hasValues)
            {
                var variance = Variance;
                _variancePanel.Visible = true;
                _varianceLabel.Text = $"${Math.Abs(variance):F2}";

                if (Math.Abs(variance) < 0.01m)
                {
                    // Balanced!
                    _varianceLabel.Text = "✓ BALANCED";
                    _varianceLabel.ForeColor = Color.Green;
                    _variancePanel.BackColor = Color.FromArgb(220, 255, 220);
                    _addAdjustmentButton.Visible = false;
                }
                else if (variance > 0)
                {
                    // Overage
                    _varianceLabel.Text = $"+${variance:F2} OVER";
                    _varianceLabel.ForeColor = Color.FromArgb(255, 140, 0);
                    _variancePanel.BackColor = Color.FromArgb(255, 245, 220);
                    _addAdjustmentButton.Visible = true;
                }
                else
                {
                    // Shortage
                    _varianceLabel.Text = $"-${Math.Abs(variance):F2} SHORT";
                    _varianceLabel.ForeColor = Color.Red;
                    _variancePanel.BackColor = Color.FromArgb(255, 220, 220);
                    _addAdjustmentButton.Visible = true;
                }

                _printSummaryButton.Enabled = true;
                _closeDayButton.Enabled = true;
            }
        }

        private void AddAdjustmentButton_Click(object? sender, EventArgs e)
        {
            // Require authentication first
            using var authDialog = new AuthenticationDialog("Adjustment - Authentication Required");
            if (authDialog.ShowDialog(this) != DialogResult.OK)
                return;
            
            using var dialog = new AdjustmentDialog(Variance);
            dialog.SetCurrentUser(authDialog.Username);
            
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Adjustments.Add(dialog.Adjustment);
                
                // Show adjustments list
                _adjustmentsList.Visible = true;
                _adjustmentsList.Items.Clear();
                
                foreach (var adj in Adjustments)
                {
                    _adjustmentsList.Items.Add($"** {adj.Reason}: ${adj.Amount:F2} - {adj.EnteredBy} **");
                }
            }
        }

        private void PrintSummaryButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Build EOD summary report
                var summary = new System.Text.StringBuilder();
                summary.AppendLine("========================================");
                summary.AppendLine("     END OF DAY CASH COUNT SUMMARY");
                summary.AppendLine("========================================");
                summary.AppendLine($"Date: {DateTime.Now:yyyy-MM-DD HH:mm:ss}");
                summary.AppendLine();
                
                // Denomination counts
                summary.AppendLine("DENOMINATION BREAKDOWN:");
                summary.AppendLine("----------------------------------------");
                foreach (var kvp in _denominationControls.OrderBy(x => _denominations[x.Key]))
                {
                    var count = (int)kvp.Value.Value;
                    var value = _denominations[kvp.Key];
                    var total = count * value;
                    if (count > 0)
                    {
                        summary.AppendLine($"{kvp.Key,-12} x {count,4} = ${total,8:F2}");
                    }
                }
                summary.AppendLine();
                
                // Totals
                summary.AppendLine("TOTALS:");
                summary.AppendLine("----------------------------------------");
                summary.AppendLine($"Expected Total:       ${ExpectedTotal,10:F2}");
                summary.AppendLine($"Safe Drop Total:      ${SafeDropTotal,10:F2}");
                summary.AppendLine($"Adjusted Expected:    ${AdjustedExpected,10:F2}");
                summary.AppendLine($"Actual Count:         ${ActualTotal,10:F2}");
                summary.AppendLine($"Variance:             ${Variance,10:F2}");
                summary.AppendLine();
                
                // Safe drops
                if (SafeDrops != null && SafeDrops.Any())
                {
                    summary.AppendLine("SAFE DROPS:");
                    summary.AppendLine("----------------------------------------");
                    foreach (var drop in SafeDrops)
                    {
                        summary.AppendLine($"  {drop.Timestamp:HH:mm} - ${drop.Amount:F2} (Invoice: {drop.Invoice}, User: {drop.Username})");
                    }
                    summary.AppendLine();
                }
                
                // Adjustments
                if (Adjustments.Any())
                {
                    summary.AppendLine("ADJUSTMENTS:");
                    summary.AppendLine("----------------------------------------");
                    foreach (var adj in Adjustments)
                    {
                        summary.AppendLine($"  ${adj.Amount:F2} - {adj.Reason} (By: {adj.EnteredBy})");
                    }
                    summary.AppendLine();
                }
                
                summary.AppendLine("========================================");
                
                // Print using default printer
                var printDoc = new System.Drawing.Printing.PrintDocument();
                string textToPrint = summary.ToString();
                
                printDoc.PrintPage += (s, ev) =>
                {
                    if (ev.Graphics != null)
                    {
                        var font = new System.Drawing.Font("Courier New", 10);
                        ev.Graphics.DrawString(textToPrint, font, System.Drawing.Brushes.Black, 
                            ev.MarginBounds, System.Drawing.StringFormat.GenericDefault);
                    }
                };
                
                printDoc.Print();
                
                MessageBox.Show("EOD summary sent to printer.", "Print Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing summary: {ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseDayButton_Click(object? sender, EventArgs e)
        {
            DenominationCounts.Clear();

            foreach (var kvp in _denominationControls)
            {
                var count = (int)kvp.Value.Value;
                if (count > 0)
                {
                    var denomValue = _denominations[kvp.Key].ToString("F2");
                    DenominationCounts[denomValue] = count;
                }
            }

            var varianceText = Math.Abs(Variance) < 0.01m ? "BALANCED ✓" :
                              Variance > 0 ? $"${Variance:F2} OVER" : $"${Math.Abs(Variance):F2} SHORT";

            var adjustmentText = Adjustments.Count > 0 
                ? $"\n\n{Adjustments.Count} adjustment(s) recorded"
                : "";

            var result = MessageBox.Show(
                $"Close day with actual count of ${ActualTotal:F2}?\n\n" +
                $"Expected: ${ExpectedTotal:F2}\n" +
                $"Actual: ${ActualTotal:F2}\n" +
                $"Variance: {varianceText}{adjustmentText}",
                "Confirm Close Day",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        private void ShowSafeDropDetails(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            var details = new StringBuilder();
            details.AppendLine("Safe Drops for Today:");
            details.AppendLine();
            
            foreach (var drop in SafeDrops)
            {
                details.AppendLine($"Time: {drop.Timestamp:HH:mm:ss}");
                details.AppendLine($"Amount: ${drop.Amount:F2}");
                details.AppendLine($"User: {drop.Username}");
                if (!string.IsNullOrWhiteSpace(drop.Invoice))
                {
                    details.AppendLine($"Invoice: {drop.Invoice}");
                }
                details.AppendLine();
            }

            details.AppendLine($"Total Safe Drops: ${SafeDropTotal:F2}");

            MessageBox.Show(
                details.ToString(),
                "Safe Drop Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    public class SafeDropInfo
    {
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
        public string Username { get; set; } = "";
        public string Invoice { get; set; } = "";
    }

    public partial class CashAdjustment
    {
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string EnteredBy { get; set; } = "";
    }
}
