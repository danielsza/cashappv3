using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class BODFloatForm : Form
    {
        // Denomination controls
        private Dictionary<string, NumericUpDown> _denominationControls = new();
        private Dictionary<string, Label> _totalLabels = new();
        private Label _grandTotalLabel = null!;
        private Label _totalAmountLabel = null!;
        private Button _submitButton = null!;
        private Button _clearButton = null!;

        // Denomination values
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

        public Dictionary<string, int> DenominationCounts { get; private set; } = new();
        public decimal TotalFloat { get; private set; }

        public BODFloatForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Beginning of Day - Starting Float";
            this.Size = new Size(600, 750);
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
                Text = "💰 Beginning of Day - Starting Float",
                Location = new Point(20, y),
                Size = new Size(540, 35),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panel.Controls.Add(titleLabel);
            y += 45;

            var instructionLabel = new Label
            {
                Text = "Count your starting cash and enter the quantity for each denomination.",
                Location = new Point(20, y),
                Size = new Size(540, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(instructionLabel);
            y += 40;

            // Coins Section
            var coinsLabel = new Label
            {
                Text = "COINS",
                Location = new Point(20, y),
                Size = new Size(540, 25),
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
                Size = new Size(540, 25),
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

            // Total Section
            var separator = new Label
            {
                Location = new Point(20, y),
                Size = new Size(540, 2),
                BorderStyle = BorderStyle.Fixed3D
            };
            panel.Controls.Add(separator);
            y += 15;

            _grandTotalLabel = new Label
            {
                Text = "TOTAL STARTING FLOAT:",
                Location = new Point(20, y),
                Size = new Size(350, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(_grandTotalLabel);

            _totalAmountLabel = new Label
            {
                Text = "$0.00",
                Location = new Point(380, y),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(_totalAmountLabel);
            y += 60;

            // Buttons
            _clearButton = new Button
            {
                Text = "🗑️ Clear All",
                Location = new Point(180, y),
                Size = new Size(120, 45),
                BackColor = Color.FromArgb(192, 192, 192),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _clearButton.Click += ClearButton_Click;
            panel.Controls.Add(_clearButton);

            _submitButton = new Button
            {
                Text = "✅ Submit Float",
                Location = new Point(310, y),
                Size = new Size(150, 45),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            _submitButton.Click += SubmitButton_Click;
            panel.Controls.Add(_submitButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(470, y),
                Size = new Size(90, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            panel.Controls.Add(cancelButton);

            this.Controls.Add(panel);
        }

        private int CreateDenominationRow(Panel panel, int y, string name, decimal value)
        {
            // Name label
            var nameLabel = new Label
            {
                Text = $"{name} (${value:F2})",
                Location = new Point(40, y + 3),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(nameLabel);

            // Count input
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

            // "x" label
            var timesLabel = new Label
            {
                Text = "×",
                Location = new Point(290, y + 3),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(timesLabel);

            // "=" label
            var equalsLabel = new Label
            {
                Text = "=",
                Location = new Point(320, y + 3),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(equalsLabel);

            // Total label
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

                if (count > 0)
                {
                    hasValues = true;
                }

                var value = _denominations[name];
                var lineTotal = count * value;
                total += lineTotal;

                _totalLabels[name].Text = $"${lineTotal:F2}";
            }

            _totalAmountLabel.Text = $"${total:F2}";
            _submitButton.Enabled = hasValues;
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            foreach (var control in _denominationControls.Values)
            {
                control.Value = 0;
            }
        }

        private void SubmitButton_Click(object? sender, EventArgs e)
        {
            DenominationCounts.Clear();
            TotalFloat = 0;

            foreach (var kvp in _denominationControls)
            {
                var name = kvp.Key;
                var count = (int)kvp.Value.Value;
                
                if (count > 0)
                {
                    // Store as denomination value as string key for compatibility
                    var denomValue = _denominations[name].ToString("F2");
                    DenominationCounts[denomValue] = count;
                    TotalFloat += count * _denominations[name];
                }
            }

            if (TotalFloat == 0)
            {
                MessageBox.Show(
                    "Please enter at least one denomination count.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Submit starting float of ${TotalFloat:F2}?\n\n" +
                $"This will mark the beginning of your day.",
                "Confirm Starting Float",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
