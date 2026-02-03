using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class AdjustmentDialog : Form
    {
        private TextBox _reasonText = null!;
        private NumericUpDown _amountNumber = null!;
        private TextBox _notesText = null!;
        private Label _enteredByLabel = null!;
        private readonly decimal _currentVariance;

        public CashAdjustment Adjustment { get; private set; } = new();

        public AdjustmentDialog(decimal currentVariance)
        {
            _currentVariance = currentVariance;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Cash Adjustment";
            this.Size = new Size(500, 520); // Increased from 450 to 520
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "📝 Cash Adjustment - Explain Discrepancy",
                Location = new Point(20, y),
                Size = new Size(450, 30),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 0)
            };
            this.Controls.Add(titleLabel);
            y += 40;

            // Variance info
            var varianceInfoLabel = new Label
            {
                Text = $"Current Variance: {(_currentVariance > 0 ? "+" : "")}{_currentVariance:F2}",
                Location = new Point(20, y),
                Size = new Size(450, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _currentVariance > 0 ? Color.FromArgb(255, 140, 0) : Color.Red
            };
            this.Controls.Add(varianceInfoLabel);
            y += 35;

            var instructionLabel = new Label
            {
                Text = "Adjustments will be displayed in BOLD on the EOD summary.\n" +
                       "Please provide a clear explanation for the variance.",
                Location = new Point(20, y),
                Size = new Size(450, 40),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            this.Controls.Add(instructionLabel);
            y += 50;

            // Reason
            var reasonLabel = new Label
            {
                Text = "Reason:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(reasonLabel);

            _reasonText = new TextBox
            {
                Location = new Point(130, y),
                Width = 330,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "e.g., Found money under register"
            };
            this.Controls.Add(_reasonText);
            y += 40;

            // Amount
            var amountLabel = new Label
            {
                Text = "Amount:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(amountLabel);

            _amountNumber = new NumericUpDown
            {
                Location = new Point(130, y - 3),
                Width = 150,
                DecimalPlaces = 2,
                Minimum = -10000,
                Maximum = 10000,
                Value = _currentVariance,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(_amountNumber);

            var amountHintLabel = new Label
            {
                Text = "(+ for found money, - for missing)",
                Location = new Point(290, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            this.Controls.Add(amountHintLabel);
            y += 50;

            // Notes
            var notesLabel = new Label
            {
                Text = "Additional Notes (Optional):",
                Location = new Point(20, y),
                Size = new Size(450, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(notesLabel);
            y += 30;

            _notesText = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(450, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Any additional details about this adjustment..."
            };
            this.Controls.Add(_notesText);
            y += 90;

            // Entered by
            var enteredByLabelText = new Label
            {
                Text = "Entered By:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(enteredByLabelText);

            _enteredByLabel = new Label
            {
                Text = "[Current User]", // Will be set from context
                Location = new Point(130, y),
                Size = new Size(330, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(_enteredByLabel);
            y += 40;

            // Buttons
            var saveButton = new Button
            {
                Text = "✅ Add Adjustment",
                Location = new Point(230, y),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(390, y),
                Size = new Size(80, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(cancelButton);
        }

        public void SetCurrentUser(string username)
        {
            _enteredByLabel.Text = username;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_reasonText.Text))
            {
                MessageBox.Show(
                    "Please provide a reason for this adjustment.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_amountNumber.Value == 0)
            {
                MessageBox.Show(
                    "Adjustment amount cannot be zero.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Adjustment = new CashAdjustment
            {
                Reason = _reasonText.Text.Trim(),
                Amount = _amountNumber.Value,
                Timestamp = DateTime.Now,
                EnteredBy = _enteredByLabel.Text,
                Notes = _notesText.Text.Trim()
            };

            var confirmMessage = Adjustment.Amount > 0
                ? $"Add adjustment: Found ${Adjustment.Amount:F2}\n\nReason: {Adjustment.Reason}"
                : $"Add adjustment: Missing ${Math.Abs(Adjustment.Amount):F2}\n\nReason: {Adjustment.Reason}";

            var result = MessageBox.Show(
                confirmMessage + "\n\nThis will be displayed in BOLD on the EOD summary.",
                "Confirm Adjustment",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
            }
        }
    }

    public partial class CashAdjustment
    {
        public string Notes { get; set; } = "";
    }
}
