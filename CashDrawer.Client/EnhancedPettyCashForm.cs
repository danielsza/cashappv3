using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class EnhancedPettyCashForm : Form
    {
        private ComboBox _recipientCombo = null!;
        private Button _addRecipientButton = null!;
        private TextBox _invoiceText = null!;
        private NumericUpDown _amountNumber = null!;
        private TextBox _purposeText = null!;
        private Button _submitButton = null!;
        private Label _statusLabel = null!;

        private List<string> _recipients = new();
        
        public string RecipientName => _recipientCombo.Text;
        public string Invoice => _invoiceText.Text;
        public decimal Amount => _amountNumber.Value;
        public string Purpose => _purposeText.Text;
        public bool ShouldPrint { get; private set; } = true;

        public EnhancedPettyCashForm(List<string> recipients)
        {
            _recipients = recipients ?? new List<string>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Petty Cash";
            this.Size = new Size(550, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "💵 Petty Cash Disbursement",
                Location = new Point(20, y),
                Size = new Size(500, 35),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            this.Controls.Add(titleLabel);
            y += 45;

            var instructionLabel = new Label
            {
                Text = "Complete the form below. Receipt will print automatically upon submission.",
                Location = new Point(20, y),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            this.Controls.Add(instructionLabel);
            y += 40;

            // Recipient
            var recipientLabel = new Label
            {
                Text = "Recipient:",
                Location = new Point(20, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(recipientLabel);

            _recipientCombo = new ComboBox
            {
                Location = new Point(150, y - 3),
                Width = 280,
                Font = new Font("Segoe UI", 10),
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            
            foreach (var recipient in _recipients)
            {
                _recipientCombo.Items.Add(recipient);
            }
            
            this.Controls.Add(_recipientCombo);

            _addRecipientButton = new Button
            {
                Text = "+ New",
                Location = new Point(440, y - 3),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _addRecipientButton.Click += AddRecipientButton_Click;
            this.Controls.Add(_addRecipientButton);
            y += 45;

            // Invoice
            var invoiceLabel = new Label
            {
                Text = "Invoice #:",
                Location = new Point(20, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(invoiceLabel);

            _invoiceText = new TextBox
            {
                Location = new Point(150, y),
                Width = 280,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "PC-001"
            };
            this.Controls.Add(_invoiceText);
            y += 45;

            // Amount
            var amountLabel = new Label
            {
                Text = "Amount:",
                Location = new Point(20, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(amountLabel);

            var dollarSignLabel = new Label
            {
                Text = "$",
                Location = new Point(130, y),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            this.Controls.Add(dollarSignLabel);

            _amountNumber = new NumericUpDown
            {
                Location = new Point(150, y - 3),
                Width = 150,
                DecimalPlaces = 2,
                Maximum = 10000,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(_amountNumber);
            y += 50;

            // Purpose
            var purposeLabel = new Label
            {
                Text = "Purpose:",
                Location = new Point(20, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(purposeLabel);
            y += 30;

            _purposeText = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(490, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Enter the purpose of this petty cash disbursement..."
            };
            this.Controls.Add(_purposeText);
            y += 110;

            // Auto-print info
            var printInfoPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(490, 50),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 248, 255)
            };

            var printIcon = new Label
            {
                Text = "🖨️",
                Location = new Point(10, 12),
                Size = new Size(30, 25),
                Font = new Font("Segoe UI", 14)
            };
            printInfoPanel.Controls.Add(printIcon);

            var printInfoLabel = new Label
            {
                Text = "Petty cash slip will be printed automatically (no popup)",
                Location = new Point(45, 15),
                Size = new Size(430, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            printInfoPanel.Controls.Add(printInfoLabel);

            this.Controls.Add(printInfoPanel);
            y += 60;

            // Status label
            _statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, y),
                Size = new Size(490, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Green,
                Visible = false
            };
            this.Controls.Add(_statusLabel);
            y += 30;

            // Buttons
            _submitButton = new Button
            {
                Text = "🖨️ Print & Submit",
                Location = new Point(230, y),
                Size = new Size(180, 45),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _submitButton.Click += SubmitButton_Click;
            this.Controls.Add(_submitButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(420, y),
                Size = new Size(90, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(cancelButton);
        }

        private void AddRecipientButton_Click(object? sender, EventArgs e)
        {
            using var inputDialog = new InputDialog(
                "Add New Recipient",
                "Recipient Name:",
                "");

            if (inputDialog.ShowDialog(this) == DialogResult.OK)
            {
                var newRecipient = inputDialog.Value.Trim();
                if (!string.IsNullOrWhiteSpace(newRecipient))
                {
                    if (!_recipients.Contains(newRecipient))
                    {
                        _recipients.Add(newRecipient);
                        _recipientCombo.Items.Add(newRecipient);
                    }
                    _recipientCombo.Text = newRecipient;
                }
            }
        }

        private void SubmitButton_Click(object? sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_recipientCombo.Text))
            {
                MessageBox.Show(
                    "Please select or enter a recipient name.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _recipientCombo.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_invoiceText.Text))
            {
                MessageBox.Show(
                    "Please enter an invoice number.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _invoiceText.Focus();
                return;
            }

            if (_amountNumber.Value <= 0)
            {
                MessageBox.Show(
                    "Please enter an amount greater than zero.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _amountNumber.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_purposeText.Text))
            {
                MessageBox.Show(
                    "Please enter the purpose of this disbursement.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _purposeText.Focus();
                return;
            }

            // Confirm
            var result = MessageBox.Show(
                $"Submit petty cash disbursement?\n\n" +
                $"Recipient: {_recipientCombo.Text}\n" +
                $"Amount: ${_amountNumber.Value:F2}\n" +
                $"Invoice: {_invoiceText.Text}\n\n" +
                $"A printed slip will be generated automatically.",
                "Confirm Petty Cash",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _statusLabel.Text = "✅ Submitting and printing...";
                _statusLabel.ForeColor = Color.Green;
                _statusLabel.Visible = true;
                _submitButton.Enabled = false;

                this.DialogResult = DialogResult.OK;
            }
        }
    }

    public class InputDialog : Form
    {
        private TextBox _inputText = null!;
        public string Value => _inputText.Text;

        public InputDialog(string title, string label, string defaultValue)
        {
            this.Text = title;
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var labelControl = new Label
            {
                Text = label,
                Location = new Point(20, 20),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(labelControl);

            _inputText = new TextBox
            {
                Location = new Point(20, 50),
                Width = 340,
                Text = defaultValue,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(_inputText);

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(190, 90),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 90),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
