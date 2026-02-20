// Cash Drawer Management System
// Copyright (c) 2026 Daniel Szajkowski. All rights reserved.
// Contact: dszajkowski@johnbear.com | 905-575-9400 ext. 236

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CashDrawer.AdminTool
{
    public partial class MainForm
    {
        private void CreateLogsTab()
        {
            var tab = new TabPage("📋 Transaction Logs");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50
            };

            var refreshButton = new Button
            {
                Text = "🔄 Refresh Logs",
                Location = new Point(0, 10),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            refreshButton.Click += (s, e) => LoadLogs(logTextBox);
            topPanel.Controls.Add(refreshButton);

            panel.Controls.Add(logTextBox);
            panel.Controls.Add(topPanel);
            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);

            LoadLogs(logTextBox);
        }

        private void CreateAboutTab()
        {
            var tab = new TabPage("ℹ About");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40)
            };

            var titleLabel = new Label
            {
                Text = "Cash Drawer Server Administration Tool",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(40, 40),
                AutoSize = true
            };

            var versionLabel = new Label
            {
                Text = "Version 3.0 - C# Edition",
                Font = new Font("Segoe UI", 12),
                Location = new Point(40, 80),
                AutoSize = true
            };

            var descLabel = new Label
            {
                Text = "Manage server configuration, users, and view transaction logs.\n\n" +
                       "Features:\n" +
                       "• Configure server settings (ports, COM port, relay type)\n" +
                       "• Add, edit, and delete users\n" +
                       "• Change passwords and unlock accounts\n" +
                       "• View transaction history\n" +
                       "• First-run setup wizard\n\n" +
                       "All changes are saved to:\n" +
                       "• appsettings.json (server configuration)\n" +
                       "• users.json (user accounts)",
                Font = new Font("Segoe UI", 10),
                Location = new Point(40, 130),
                Size = new Size(800, 300)
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(versionLabel);
            panel.Controls.Add(descLabel);
            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private void LoadData()
        {
            try
            {
                _userManager = new UserManager();       // Uses ProgramData automatically
                _configManager = new ConfigManager();   // Uses ProgramData automatically

                LoadServerConfig();
                LoadUsers();

                // Check for first run
                if (_userManager.GetUsers().Count == 0)
                {
                    ShowFirstRunSetup();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading data: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadServerConfig()
        {
            var config = _configManager.LoadConfig();
            _serverIdText.Text = config.ServerID;
            _portNumber.Value = config.Port;
            _comPortCombo.SelectedItem = config.COMPort;
            _relayTypeCombo.SelectedItem = config.RelayPin.ToString();
            _relayDurationNumber.Value = (decimal)config.RelayDuration;
            _logPathText.Text = config.LogPath;
            _localLogPathText.Text = config.LocalLogPath;
            _testModeCheck.Checked = config.TestMode;
        }

        private void LoadUsers()
        {
            _usersList.Items.Clear();
            var users = _userManager.GetUsers();

            foreach (var kvp in users)
            {
                var user = kvp.Value;
                var status = user.IsLocked ? "🔒" : "✓";
                var level = user.Level == Shared.Models.UserLevel.Admin ? "[ADMIN]" : "[USER]";
                var display = $"{status} {user.Username,-15} {level,-10} {user.Name}";
                _usersList.Items.Add(display);
            }

            if (_usersList.Items.Count > 0)
            {
                _usersList.SelectedIndex = 0;
            }
        }

        private void LoadLogs(TextBox textBox)
        {
            try
            {
                var logPath = Path.Combine(_serverPath, "Logs");
                var todayLog = Path.Combine(logPath, $"CashDrawer_{DateTime.Now:yyyy-MM-dd}.log");

                if (File.Exists(todayLog))
                {
                    var lines = File.ReadAllLines(todayLog);
                    var recent = lines.Length > 100 ? lines.Skip(lines.Length - 100) : lines;
                    textBox.Text = string.Join(Environment.NewLine, recent);
                    textBox.SelectionStart = textBox.Text.Length;
                    textBox.ScrollToCaret();
                }
                else
                {
                    textBox.Text = "No transactions logged today.";
                }
            }
            catch (Exception ex)
            {
                textBox.Text = $"Error loading logs: {ex.Message}";
            }
        }

        private void ShowFirstRunSetup()
        {
            var result = MessageBox.Show(
                "No users found!\n\n" +
                "This appears to be the first time configuring the server.\n" +
                "You must create an administrator account.\n\n" +
                "Create admin account now?",
                "First Time Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                using var dialog = new UserEditorDialog(null);
                dialog.Text = "Create Administrator Account";
                dialog.ForceAdmin = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _userManager.AddUser(
                        dialog.Username,
                        dialog.Password,
                        dialog.UserName,
                        Shared.Models.UserLevel.Admin);

                    _userManager.SaveUsers();
                    LoadUsers();

                    MessageBox.Show(
                        $"Administrator account '{dialog.Username}' created!\n\n" +
                        "You can now add additional users and configure the server.",
                        "Setup Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    ShowFirstRunSetup(); // Try again
                }
            }
        }

        // Event Handlers
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save server config
                var config = _configManager.LoadConfig();
                config.ServerID = _serverIdText.Text;
                config.Port = (int)_portNumber.Value;
                config.COMPort = _comPortCombo.SelectedItem?.ToString() ?? "COM10";
                config.RelayPin = Enum.Parse<Shared.Models.RelayType>(_relayTypeCombo.SelectedItem?.ToString() ?? "DTR");
                config.RelayDuration = (double)_relayDurationNumber.Value;
                config.LogPath = _logPathText.Text;
                config.LocalLogPath = _localLogPathText.Text;
                config.TestMode = _testModeCheck.Checked;
                _configManager.SaveConfig(config);

                // Save users
                _userManager.SaveUsers();

                var testModeWarning = config.TestMode 
                    ? "\n\n⚠️ TEST MODE IS ENABLED - Relay will NOT be triggered!" 
                    : "";

                MessageBox.Show(
                    $"All changes saved successfully!{testModeWarning}\n\n" +
                    "Note: Server must be restarted for some changes to take effect.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AddUser_Click(object? sender, EventArgs e)
        {
            using var dialog = new UserEditorDialog(null);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _userManager.AddUser(
                    dialog.Username,
                    dialog.Password,
                    dialog.UserName,
                    dialog.UserLevel);
                LoadUsers();
            }
        }

        private void EditUser_Click(object? sender, EventArgs e)
        {
            if (_usersList.SelectedIndex < 0) return;

            var username = GetSelectedUsername();
            if (username == null) return;

            var user = _userManager.GetUser(username);
            if (user == null) return;

            using var dialog = new UserEditorDialog(user);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _userManager.UpdateUser(
                    username,
                    dialog.UserName,
                    dialog.UserLevel);
                LoadUsers();
            }
        }

        private void ChangePassword_Click(object? sender, EventArgs e)
        {
            var username = GetSelectedUsername();
            if (username == null) return;

            using var dialog = new PasswordChangeDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _userManager.ChangePassword(username, dialog.NewPassword);
                MessageBox.Show("Password changed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteUser_Click(object? sender, EventArgs e)
        {
            var username = GetSelectedUsername();
            if (username == null) return;

            var user = _userManager.GetUser(username);
            if (user?.Level == Shared.Models.UserLevel.Admin)
            {
                var adminCount = _userManager.GetUsers().Values
                    .Count(u => u.Level == Shared.Models.UserLevel.Admin);

                if (adminCount <= 1)
                {
                    MessageBox.Show(
                        "Cannot delete the last administrator account!",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Delete user '{username}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _userManager.DeleteUser(username);
                LoadUsers();
            }
        }

        private void UnlockUser_Click(object? sender, EventArgs e)
        {
            var username = GetSelectedUsername();
            if (username == null) return;

            _userManager.UnlockUser(username);
            LoadUsers();
            MessageBox.Show($"Account '{username}' unlocked!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UsersList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var username = GetSelectedUsername();
            if (username == null)
            {
                _editUserButton.Enabled = false;
                _deleteUserButton.Enabled = false;
                _changePasswordButton.Enabled = false;
                _unlockUserButton.Enabled = false;
                _userDetailsLabel.Text = "Select a user to view details";
                return;
            }

            var user = _userManager.GetUser(username);
            if (user == null) return;

            _editUserButton.Enabled = true;
            _deleteUserButton.Enabled = true;
            _changePasswordButton.Enabled = true;
            _unlockUserButton.Enabled = user.IsLocked;

            _userDetailsLabel.Text =
                $"Username: {user.Username}\n" +
                $"Name: {user.Name}\n" +
                $"Level: {user.Level}\n" +
                $"Created: {user.Created:yyyy-MM-dd HH:mm}\n" +
                $"Failed Attempts: {user.FailedAttempts}\n" +
                $"Locked: {(user.IsLocked ? "Yes" : "No")}\n" +
                (user.LockedUntil.HasValue ? $"Locked Until: {user.LockedUntil:yyyy-MM-dd HH:mm}\n" : "");
        }

        // Helper Methods
        private string? GetSelectedUsername()
        {
            if (_usersList.SelectedIndex < 0) return null;

            var item = _usersList.SelectedItem?.ToString();
            if (item == null) return null;

            // Parse username from display format: "✓ username [LEVEL] Name"
            var parts = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            return parts[1]; // Username is second element
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
        }

        private Button CreateActionButton(string text, int y, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(420, 45),  // Wide buttons for 450px panel
                BackColor = backColor,
                ForeColor = backColor.GetBrightness() > 0.5 ? Color.Black : Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
        }

        private void BrowseFolder(TextBox textBox)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = textBox.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.SelectedPath;
            }
        }

        private void TestRelay_Click(object? sender, EventArgs e)
        {
            // Get current settings from UI
            var comPort = _comPortCombo.SelectedItem?.ToString();
            var relayType = _relayTypeCombo.SelectedItem?.ToString();
            var duration = (double)_relayDurationNumber.Value;

            if (string.IsNullOrEmpty(comPort))
            {
                MessageBox.Show("Please select a COM port first", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(relayType))
            {
                MessageBox.Show("Please select a relay type first", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find the result label
            var resultLabel = this.Controls.Find("testResultLabel", true).FirstOrDefault() as Label;

            var result = MessageBox.Show(
                $"Test Relay Settings:\n\n" +
                $"COM Port: {comPort}\n" +
                $"Relay Type: {relayType}\n" +
                $"Duration: {duration} seconds\n\n" +
                $"This will attempt to open the cash drawer.\n" +
                $"Make sure the drawer is connected!\n\n" +
                $"Continue with test?",
                "Test Relay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                // Parse relay type
                var relayTypeEnum = Enum.Parse<Shared.Models.RelayType>(relayType);

                // Test the relay
                using var tester = new RelayTester();
                var success = tester.TestRelay(comPort, relayTypeEnum, duration);

                if (success)
                {
                    if (resultLabel != null)
                    {
                        resultLabel.Text = "✓ Test successful! Drawer should have opened.";
                        resultLabel.ForeColor = Color.Green;
                    }

                    MessageBox.Show(
                        "Relay test successful!\n\n" +
                        "If the drawer opened, your settings are correct.\n" +
                        "If it didn't open, try:\n" +
                        "• Different COM port\n" +
                        "• Different relay type\n" +
                        "• Check physical connections",
                        "Test Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    if (resultLabel != null)
                    {
                        resultLabel.Text = "✗ Test failed - see error message";
                        resultLabel.ForeColor = Color.Red;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (resultLabel != null)
                {
                    resultLabel.Text = "✗ Access denied - run as administrator";
                    resultLabel.ForeColor = Color.Red;
                }

                MessageBox.Show(
                    "Access Denied!\n\n" +
                    "COM ports require administrator rights.\n\n" +
                    "Please:\n" +
                    "1. Close this application\n" +
                    "2. Right-click CashDrawer.AdminTool.exe\n" +
                    "3. Choose 'Run as administrator'\n" +
                    "4. Try the test again",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                if (resultLabel != null)
                {
                    resultLabel.Text = $"✗ Error: {ex.Message}";
                    resultLabel.ForeColor = Color.Red;
                }

                MessageBox.Show(
                    $"Relay test failed:\n\n{ex.Message}\n\n" +
                    $"Common issues:\n" +
                    $"• Wrong COM port selected\n" +
                    $"• COM port in use by another program\n" +
                    $"• Drawer not connected\n" +
                    $"• Need administrator rights",
                    "Test Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
