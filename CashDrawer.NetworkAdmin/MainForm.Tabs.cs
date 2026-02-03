using System;
using System.Drawing;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private void CreateServerConfigTab()
        {
            var tab = new TabPage("Server Configuration");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            
            int y = 20;

            panel.Controls.Add(CreateLabel("Server ID:", 20, y));
            _serverIdText = new TextBox { Location = new Point(200, y - 3), Width = 300 };
            panel.Controls.Add(_serverIdText);
            y += 40;

            panel.Controls.Add(CreateLabel("TCP Port:", 20, y));
            _portNumber = new NumericUpDown { Location = new Point(200, y - 3), Width = 150, Minimum = 1000, Maximum = 65535, Value = 5000 };
            panel.Controls.Add(_portNumber);
            y += 40;

            panel.Controls.Add(CreateLabel("COM Port:", 20, y));
            _comPortCombo = new ComboBox { Location = new Point(200, y - 3), Width = 150, DropDownStyle = ComboBoxStyle.DropDown };
            for (int i = 1; i <= 20; i++) _comPortCombo.Items.Add($"COM{i}");
            panel.Controls.Add(_comPortCombo);
            y += 40;

            panel.Controls.Add(CreateLabel("Relay Type:", 20, y));
            _relayTypeCombo = new ComboBox { Location = new Point(200, y - 3), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _relayTypeCombo.Items.AddRange(new object[] { "DTR", "DTR_INVERTED", "RTS", "RTS_INVERTED", "BYTES_ESC", "BYTES_DLE", "RELAY_COMMANDS" });
            panel.Controls.Add(_relayTypeCombo);
            y += 40;

            panel.Controls.Add(CreateLabel("Relay Duration (sec):", 20, y));
            _relayDurationNumber = new NumericUpDown { Location = new Point(200, y - 3), Width = 150, Minimum = 0.1M, Maximum = 5.0M, DecimalPlaces = 1, Increment = 0.1M, Value = 0.7M };
            panel.Controls.Add(_relayDurationNumber);
            y += 40;

            _testModeCheck = new CheckBox { Text = "🧪 Test Mode (Disable Relay)", Location = new Point(20, y), Size = new Size(500, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkOrange };
            panel.Controls.Add(_testModeCheck);
            y += 40;

            panel.Controls.Add(CreateLabel("Network Log Path:", 20, y));
            _logPathText = new TextBox { Location = new Point(200, y - 3), Width = 500 };
            panel.Controls.Add(_logPathText);
            y += 40;

            panel.Controls.Add(CreateLabel("Local Log Path:", 20, y));
            _localLogPathText = new TextBox { Location = new Point(200, y - 3), Width = 500 };
            panel.Controls.Add(_localLogPathText);
            y += 60;

            _testRelayButton = new Button { Text = "🔌 Test Relay", Location = new Point(20, y), Size = new Size(180, 45), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            _testRelayButton.Click += async (s, e) => await TestRelayButton_Click();
            panel.Controls.Add(_testRelayButton);

            _startServerButton = new Button { Text = "▶️ Start Server", Location = new Point(210, y), Size = new Size(180, 45), BackColor = Color.FromArgb(16, 124, 16), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            _startServerButton.Click += async (s, e) => await ServerControlButton_Click("start");
            panel.Controls.Add(_startServerButton);

            _restartServerButton = new Button { Text = "🔄 Restart Server", Location = new Point(400, y), Size = new Size(180, 45), BackColor = Color.FromArgb(255, 165, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            _restartServerButton.Click += async (s, e) => await ServerControlButton_Click("restart");
            panel.Controls.Add(_restartServerButton);

            _stopServerButton = new Button { Text = "⏹️ Stop Server", Location = new Point(590, y), Size = new Size(180, 45), BackColor = Color.FromArgb(192, 0, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            _stopServerButton.Click += async (s, e) => await ServerControlButton_Click("stop");
            panel.Controls.Add(_stopServerButton);
            y += 60;
            
            var testNotificationButton = new Button { Text = "🔔 Test Notification", Location = new Point(20, y), Size = new Size(180, 40), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            testNotificationButton.Click += async (s, e) => await TestNotificationButton_Click();
            panel.Controls.Add(testNotificationButton);
            y += 50;

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private void CreateUsersTab()
        {
            var tab = new TabPage("User Management");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            panel.Controls.Add(new Label { Text = "Users:", Location = new Point(20, 20), Size = new Size(100, 25), Font = new Font("Segoe UI", 11, FontStyle.Bold) });

            _usersList = new ListBox { Location = new Point(20, 50), Size = new Size(400, 500), Font = new Font("Consolas", 10) };
            _usersList.SelectedIndexChanged += UsersList_SelectedIndexChanged;
            panel.Controls.Add(_usersList);

            int buttonX = 440, buttonY = 50;

            _addUserButton = new Button { Text = "➕ Add User", Location = new Point(buttonX, buttonY), Size = new Size(150, 40), BackColor = Color.FromArgb(16, 124, 16), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            _addUserButton.Click += async (s, e) => await AddUserButton_Click();
            panel.Controls.Add(_addUserButton);
            buttonY += 50;

            _editUserButton = new Button { Text = "✏️ Edit User", Location = new Point(buttonX, buttonY), Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Enabled = false };
            _editUserButton.Click += async (s, e) => await EditUserButton_Click();
            panel.Controls.Add(_editUserButton);
            buttonY += 50;

            _deleteUserButton = new Button { Text = "❌ Delete User", Location = new Point(buttonX, buttonY), Size = new Size(150, 40), BackColor = Color.FromArgb(192, 0, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Enabled = false };
            _deleteUserButton.Click += async (s, e) => await DeleteUserButton_Click();
            panel.Controls.Add(_deleteUserButton);
            buttonY += 50;

            _unlockUserButton = new Button { Text = "🔓 Unlock User", Location = new Point(buttonX, buttonY), Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Enabled = false };
            _unlockUserButton.Click += async (s, e) => await UnlockUserButton_Click();
            panel.Controls.Add(_unlockUserButton);
            buttonY += 50;

            _resetPasswordButton = new Button { Text = "🔑 Reset Password", Location = new Point(buttonX, buttonY), Size = new Size(150, 40), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Enabled = false };
            _resetPasswordButton.Click += async (s, e) => await ResetPasswordButton_Click();
            panel.Controls.Add(_resetPasswordButton);

            _userDetailsLabel = new Label { Location = new Point(buttonX, buttonY + 60), Size = new Size(400, 300), Font = new Font("Consolas", 9), ForeColor = Color.Gray };
            panel.Controls.Add(_userDetailsLabel);

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private void CreateAboutTab()
        {
            var tab = new TabPage("About");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            var aboutText = new Label
            {
                Text = @"Cash Drawer - Network Administration Tool
Version 3.0

Features:
• Auto-discover servers on network
• Remote configuration management
• User management across all servers
• Test relay remotely
• No file access needed

© 2026 Daniel Szajkowski - Cash Drawer Management System",
                Location = new Point(20, 20),
                Size = new Size(700, 500),
                Font = new Font("Segoe UI", 11)
            };
            panel.Controls.Add(aboutText);

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }
    }
}
