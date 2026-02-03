using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private TabPage _emailTab = null!;
        private CheckBox _emailEnabledCheck = null!;
        private TextBox _smtpServerText = null!;
        private NumericUpDown _smtpPortNumber = null!;
        private CheckBox _useSslCheck = null!;
        private TextBox _smtpUsernameText = null!;
        private TextBox _smtpPasswordText = null!;
        private TextBox _fromEmailText = null!;
        private TextBox _adminEmailsText = null!;
        private Button _saveEmailButton = null!;
        private Label _emailStatusLabel = null!;

        private void CreateEmailConfigTab()
        {
            _emailTab = new TabPage("Email Configuration");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };

            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "📧 Email Notification Settings",
                Location = new Point(20, y),
                Size = new Size(600, 35),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panel.Controls.Add(titleLabel);
            y += 50;

            // Info
            var infoLabel = new Label
            {
                Text = "Configure SMTP settings for error notifications. Changes require server restart to take effect.",
                Location = new Point(20, y),
                Size = new Size(700, 40),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(infoLabel);
            y += 50;

            // Enable Email
            _emailEnabledCheck = new CheckBox
            {
                Text = "✓ Enable Email Notifications",
                Location = new Point(20, y),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 16)
            };
            _emailEnabledCheck.CheckedChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_emailEnabledCheck);
            y += 40;

            // SMTP Server
            panel.Controls.Add(CreateLabel("SMTP Server:", 20, y));
            _smtpServerText = new TextBox { Location = new Point(200, y - 3), Width = 300 };
            _smtpServerText.TextChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_smtpServerText);
            y += 35;

            // SMTP Port
            panel.Controls.Add(CreateLabel("SMTP Port:", 20, y));
            _smtpPortNumber = new NumericUpDown 
            { 
                Location = new Point(200, y - 3), 
                Width = 100,
                Minimum = 1,
                Maximum = 65535,
                Value = 587
            };
            _smtpPortNumber.ValueChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_smtpPortNumber);

            _useSslCheck = new CheckBox
            {
                Text = "Use SSL/TLS",
                Location = new Point(320, y),
                Size = new Size(120, 25)
            };
            _useSslCheck.CheckedChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_useSslCheck);
            y += 35;

            // Username
            panel.Controls.Add(CreateLabel("SMTP Username:", 20, y));
            _smtpUsernameText = new TextBox { Location = new Point(200, y - 3), Width = 300 };
            _smtpUsernameText.TextChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_smtpUsernameText);
            y += 35;

            // Password
            panel.Controls.Add(CreateLabel("SMTP Password:", 20, y));
            _smtpPasswordText = new TextBox 
            { 
                Location = new Point(200, y - 3), 
                Width = 300,
                UseSystemPasswordChar = true
            };
            _smtpPasswordText.TextChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_smtpPasswordText);

            var showPasswordCheck = new CheckBox
            {
                Text = "Show",
                Location = new Point(510, y),
                Size = new Size(70, 25)
            };
            showPasswordCheck.CheckedChanged += (s, e) => 
                _smtpPasswordText.UseSystemPasswordChar = !showPasswordCheck.Checked;
            panel.Controls.Add(showPasswordCheck);
            y += 35;

            // From Email
            panel.Controls.Add(CreateLabel("From Email:", 20, y));
            _fromEmailText = new TextBox { Location = new Point(200, y - 3), Width = 300 };
            _fromEmailText.TextChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_fromEmailText);
            y += 35;

            // Admin Emails
            panel.Controls.Add(CreateLabel("Admin Emails:", 20, y));
            _adminEmailsText = new TextBox 
            { 
                Location = new Point(200, y - 3), 
                Width = 300,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _adminEmailsText.TextChanged += (s, e) => MarkAsModified();
            panel.Controls.Add(_adminEmailsText);

            var emailHintLabel = new Label
            {
                Text = "One email per line",
                Location = new Point(510, y),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(emailHintLabel);
            y += 95;

            // Save button
            _saveEmailButton = new Button
            {
                Text = "💾 Save Email Settings",
                Location = new Point(200, y),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            _saveEmailButton.Click += SaveEmailButton_Click;
            panel.Controls.Add(_saveEmailButton);
            y += 50;

            // Status label
            _emailStatusLabel = new Label
            {
                Text = "",
                Location = new Point(200, y),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9)
            };
            panel.Controls.Add(_emailStatusLabel);

            _emailTab.Controls.Add(panel);
            _tabControl.TabPages.Add(_emailTab);
        }

        private void LoadEmailConfigurationFromServer()
        {
            if (_selectedServer?.Config == null) return;

            try
            {
                // Load from selected server's config
                // Note: In a real implementation, you'd load this from a separate NotificationConfig
                // For now, we'll use placeholder values that should be manually configured
                _emailEnabledCheck.Checked = false; // Default
                _smtpServerText.Text = "smtp.office365.com";
                _smtpPortNumber.Value = 587;
                _useSslCheck.Checked = true;
                _smtpUsernameText.Text = "";
                _smtpPasswordText.Text = "";
                _fromEmailText.Text = "cashdrawer@company.com";
                _adminEmailsText.Text = "dszajkowski@johnbear.com";

                _saveEmailButton.Enabled = false;
            }
            catch (Exception ex)
            {
                _emailStatusLabel.Text = $"❌ Error loading email config: {ex.Message}";
                _emailStatusLabel.ForeColor = Color.Red;
            }
        }

        private void SaveEmailButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Validation
                if (_emailEnabledCheck.Checked)
                {
                    if (string.IsNullOrWhiteSpace(_smtpServerText.Text))
                    {
                        MessageBox.Show("SMTP server is required when email is enabled.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_fromEmailText.Text))
                    {
                        MessageBox.Show("From email is required when email is enabled.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_adminEmailsText.Text))
                    {
                        MessageBox.Show("At least one admin email is required.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                _emailStatusLabel.Text = "💾 Email settings will be saved with server configuration...";
                _emailStatusLabel.ForeColor = Color.Blue;
                _saveEmailButton.Enabled = false;

                // Show instruction message
                MessageBox.Show(
                    "Email settings have been validated.\n\n" +
                    "To apply these settings:\n" +
                    "1. Click 'Save All Changes' at the bottom\n" +
                    "2. Restart the server for changes to take effect\n\n" +
                    "Note: Email settings are stored in the server's appsettings.json file.\n" +
                    "You can also edit them directly on the server:\n" +
                    "C:\\Program Files\\Daniel Szajkowski\\Cash Drawer System\\Server\\appsettings.json",
                    "Email Configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _emailStatusLabel.Text = "✅ Email settings ready - click 'Save All Changes' to apply";
                _emailStatusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _emailStatusLabel.Text = "❌ Error saving email settings";
                _emailStatusLabel.ForeColor = Color.Red;

                MessageBox.Show($"Failed to save email settings:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MarkAsModified()
        {
            if (_selectedServer != null)
            {
                _saveButton.Enabled = true;
                _saveEmailButton.Enabled = true;
            }
        }
    }
}
