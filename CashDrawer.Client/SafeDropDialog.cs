using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class SafeDropDialog : Form
    {
        private Label _amountLabel = null!;
        private Label _infoLabel = null!;
        private CheckBox _confirmCheck = null!;
        private Button _dropButton = null!;
        private Button _skipButton = null!;

        public bool DropConfirmed { get; private set; }
        public decimal Amount { get; private set; }

        public SafeDropDialog(decimal amount)
        {
            Amount = amount;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Safe Drop Required";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int y = 20;

            // Icon and title
            var iconLabel = new Label
            {
                Text = "🔒",
                Location = new Point(20, y),
                Size = new Size(60, 60),
                Font = new Font("Segoe UI", 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(iconLabel);

            var titleLabel = new Label
            {
                Text = "Safe Drop Required",
                Location = new Point(90, y + 10),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 0, 0)
            };
            this.Controls.Add(titleLabel);
            y += 80;

            // Amount
            var amountTitleLabel = new Label
            {
                Text = "Transaction Amount:",
                Location = new Point(20, y),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(amountTitleLabel);
            y += 35;

            _amountLabel = new Label
            {
                Text = $"${Amount:F2}",
                Location = new Point(20, y),
                Size = new Size(480, 55),
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(_amountLabel);
            y += 70;

            // Info panel - larger and more readable
            var infoPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(480, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(255, 245, 220)
            };

            var warningIcon = new Label
            {
                Text = "⚠️",
                Location = new Point(15, 20),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 24)
            };
            infoPanel.Controls.Add(warningIcon);

            _infoLabel = new Label
            {
                Text = "Transactions over $200 require a safe drop.\n\nPlease drop this amount into the safe now.",
                Location = new Point(60, 15),
                Size = new Size(400, 70),
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleLeft
            };
            infoPanel.Controls.Add(_infoLabel);
            this.Controls.Add(infoPanel);
            y += 115;

            // Confirmation checkbox
            _confirmCheck = new CheckBox
            {
                Text = "I have dropped the cash into the safe",
                Location = new Point(30, y),
                Size = new Size(460, 35),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            _confirmCheck.CheckedChanged += (s, e) => UpdateButtonStates();
            this.Controls.Add(_confirmCheck);
            y += 55;

            // Buttons
            _dropButton = new Button
            {
                Text = "🔒 Confirm Safe Drop",
                Location = new Point(80, y),
                Size = new Size(220, 55),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Enabled = false
            };
            _dropButton.Click += DropButton_Click;
            this.Controls.Add(_dropButton);

            _skipButton = new Button
            {
                Text = "Skip",
                Location = new Point(320, y),
                Size = new Size(120, 55),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Gray
            };
            _skipButton.Click += SkipButton_Click;
            this.Controls.Add(_skipButton);
        }

        private void UpdateButtonStates()
        {
            _dropButton.Enabled = _confirmCheck.Checked;
        }

        private void DropButton_Click(object? sender, EventArgs e)
        {
            DropConfirmed = true;
            this.DialogResult = DialogResult.OK;
        }

        private void SkipButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ Are you sure you want to skip the safe drop?\n\n" +
                "This violates cash handling policy and will be logged.",
                "Skip Safe Drop?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                DropConfirmed = false;
                this.DialogResult = DialogResult.OK;
            }
        }
    }

    public class SafeDropData
    {
        public string TransactionId { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
        public string Username { get; set; } = "";
        public string Invoice { get; set; } = "";
        public bool Confirmed { get; set; }
        public string ServerName { get; set; } = "";
    }
}
