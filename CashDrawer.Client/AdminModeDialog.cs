using System;
using System.Drawing;
using System.Windows.Forms;
using CashDrawer.Client.Models;

namespace CashDrawer.Client
{
    public class AdminModeDialog : Form
    {
        private TextBox _usernameText = null!;
        private TextBox _passwordText = null!;
        private CheckBox _showErrorPopupsCheck = null!;
        private CheckBox _criticalOnlyCheck = null!;
        private readonly ClientSettings _settings;

        public bool AdminModeEnabled { get; private set; }

        public AdminModeDialog(ClientSettings settings)
        {
            _settings = settings;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Enable Admin Mode";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            var titleLabel = new Label
            {
                Text = "⚠️ Admin Mode - Receive Error Notifications",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.DarkOrange
            };
            this.Controls.Add(titleLabel);
            y += 40;

            var infoLabel = new Label
            {
                Text = "Admin mode allows you to receive real-time error notifications\n" +
                       "from the server. You must authenticate with an admin account.",
                Location = new Point(20, y),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(infoLabel);
            y += 55;

            var usernameLabel = new Label
            {
                Text = "Admin Username:",
                Location = new Point(20, y),
                Size = new Size(130, 25)
            };
            this.Controls.Add(usernameLabel);

            _usernameText = new TextBox
            {
                Location = new Point(160, y),
                Width = 250
            };
            this.Controls.Add(_usernameText);
            y += 40;

            var passwordLabel = new Label
            {
                Text = "Password:",
                Location = new Point(20, y),
                Size = new Size(130, 25)
            };
            this.Controls.Add(passwordLabel);

            _passwordText = new TextBox
            {
                Location = new Point(160, y),
                Width = 250,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_passwordText);
            y += 50;

            _showErrorPopupsCheck = new CheckBox
            {
                Text = "Show error popup notifications",
                Location = new Point(20, y),
                Size = new Size(400, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(_showErrorPopupsCheck);
            y += 35;

            _criticalOnlyCheck = new CheckBox
            {
                Text = "Show critical errors only (recommended)",
                Location = new Point(40, y),
                Size = new Size(380, 25),
                Checked = true
            };
            this.Controls.Add(_criticalOnlyCheck);
            y += 50;

            var enableButton = new Button
            {
                Text = "Enable Admin Mode",
                Location = new Point(180, y),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            enableButton.Click += EnableButton_Click;
            this.Controls.Add(enableButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(340, y),
                Size = new Size(70, 35)
            };
            cancelButton.Click += (s, e) => this.Close();
            this.Controls.Add(cancelButton);
        }

        private async void EnableButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_usernameText.Text) || 
                string.IsNullOrWhiteSpace(_passwordText.Text))
            {
                MessageBox.Show(
                    "Please enter admin username and password",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // TODO: Authenticate with server
            // For now, just enable
            _settings.AdminModeEnabled = true;
            _settings.AdminUsername = _usernameText.Text;
            _settings.ShowErrorPopups = _showErrorPopupsCheck.Checked;
            _settings.ShowCriticalAlertsOnly = _criticalOnlyCheck.Checked;
            _settings.Save();

            AdminModeEnabled = true;

            MessageBox.Show(
                "✅ Admin mode enabled!\n\n" +
                "You will now receive error notifications from the server.\n" +
                "This setting persists until you disable it.",
                "Admin Mode Enabled",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.Close();
        }
    }
}
