// Cash Drawer Management System
// Copyright (c) 2026 Daniel Szajkowski. All rights reserved.
// Contact: dszajkowski@johnbear.com | 905-575-9400 ext. 236

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using CashDrawer.Shared.Models;

namespace CashDrawer.AdminTool
{
    public partial class MainForm : Form
    {
        private UserManager _userManager = null!;
        private ConfigManager _configManager = null!;
        private string _serverPath = "";

        private TabControl _tabControl = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Label _pathLabel = null!;

        // Server Config Tab
        private TextBox _serverIdText = null!;
        private NumericUpDown _portNumber = null!;
        private ComboBox _comPortCombo = null!;
        private ComboBox _relayTypeCombo = null!;
        private NumericUpDown _relayDurationNumber = null!;
        private TextBox _logPathText = null!;
        private TextBox _localLogPathText = null!;
        private CheckBox _testModeCheck = null!;

        // Users Tab
        private ListBox _usersList = null!;
        private Button _addUserButton = null!;
        private Button _editUserButton = null!;
        private Button _deleteUserButton = null!;
        private Button _changePasswordButton = null!;
        private Button _unlockUserButton = null!;
        private Label _userDetailsLabel = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeServerPath();
        }

        private void InitializeServerPath()
        {
            // Use ProgramData path (same location server uses)
            _serverPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CashDrawer");
            
            // Ensure directory exists
            if (!Directory.Exists(_serverPath))
                Directory.CreateDirectory(_serverPath);
            
            _pathLabel.Text = $"Config: {Path.Combine(_serverPath, "appsettings.json")}";
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Cash Drawer - Server Administration Tool";
            this.Size = new Size(1000, 750);  // Wider window
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(1000, 750);

            // Top panel with path (60px tall to ensure visibility)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(15, 5, 15, 5)
            };

            var titleLabel = new Label
            {
                Text = "🔧 Server Administration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 8)
            };

            _pathLabel = new Label
            {
                Text = "No server selected",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 35)
            };

            topPanel.Controls.Add(titleLabel);
            topPanel.Controls.Add(_pathLabel);

            // Bottom panel with buttons (70px tall)
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            _saveButton = new Button
            {
                Text = "💾 Save All Changes",
                Location = new Point(750, 18),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _saveButton.Click += SaveButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(910, 18),
                Size = new Size(70, 38),
                FlatStyle = FlatStyle.Flat
            };
            _cancelButton.Click += (s, e) => this.Close();

            var openFolderButton = new Button
            {
                Text = "📁 Open Config Folder",
                Location = new Point(15, 18),
                Size = new Size(180, 38),
                FlatStyle = FlatStyle.Flat
            };
            openFolderButton.Click += (s, e) => 
            {
                System.Diagnostics.Process.Start("explorer.exe", _serverPath);
            };

            bottomPanel.Controls.Add(_saveButton);
            bottomPanel.Controls.Add(_cancelButton);
            bottomPanel.Controls.Add(openFolderButton);

            // Tab control - MUST be visible with proper size
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Padding = new Point(10, 10),
                Appearance = TabAppearance.Normal,
                SizeMode = TabSizeMode.Normal
            };

            // Create all tabs
            CreateServerConfigTab();
            CreateUsersTab();
            CreateLogsTab();
            CreateAboutTab();

            // Add controls to form in CORRECT ORDER
            // Order matters for docking!
            this.Controls.Add(_tabControl);  // Add tab control FIRST (will fill)
            this.Controls.Add(bottomPanel);  // Then bottom (will dock to bottom)
            this.Controls.Add(topPanel);     // Then top (will dock to top)
        }

        private void CreateServerConfigTab()
        {
            var tab = new TabPage("⚙ Server Configuration");
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 30, 20, 20), // Extra top padding
                AutoScroll = true
            };

            int y = 15; // Start lower to account for padding

            // Server ID
            panel.Controls.Add(CreateLabel("Server ID:", 20, y));
            _serverIdText = new TextBox { Location = new Point(200, y - 3), Width = 300 };
            panel.Controls.Add(_serverIdText);
            y += 50;

            // Port
            panel.Controls.Add(CreateLabel("TCP Port:", 20, y));
            _portNumber = new NumericUpDown
            {
                Location = new Point(200, y - 3),
                Width = 150,
                Minimum = 1000,
                Maximum = 65535,
                Value = 5000
            };
            panel.Controls.Add(_portNumber);
            y += 45;

            // COM Port
            panel.Controls.Add(CreateLabel("COM Port:", 20, y));
            _comPortCombo = new ComboBox
            {
                Location = new Point(200, y - 3),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 20; i++)
                _comPortCombo.Items.Add($"COM{i}");
            panel.Controls.Add(_comPortCombo);
            y += 45;

            // Relay Type
            panel.Controls.Add(CreateLabel("Relay Type:", 20, y));
            _relayTypeCombo = new ComboBox
            {
                Location = new Point(200, y - 3),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (RelayType type in Enum.GetValues(typeof(RelayType)))
                _relayTypeCombo.Items.Add(type.ToString());
            panel.Controls.Add(_relayTypeCombo);
            y += 45;

            // Relay Duration
            panel.Controls.Add(CreateLabel("Relay Duration (seconds):", 20, y));
            _relayDurationNumber = new NumericUpDown
            {
                Location = new Point(200, y - 3),
                Width = 150,
                Minimum = 0.1M,
                Maximum = 5.0M,
                DecimalPlaces = 1,
                Increment = 0.1M,
                Value = 0.5M
            };
            panel.Controls.Add(_relayDurationNumber);
            y += 45;

            // Network Log Path
            panel.Controls.Add(CreateLabel("Network Log Path:", 20, y));
            _logPathText = new TextBox
            {
                Location = new Point(200, y - 3),
                Width = 500
            };
            var browseButton1 = new Button
            {
                Text = "Browse...",
                Location = new Point(710, y - 5),
                Size = new Size(100, 28)
            };
            browseButton1.Click += (s, e) => BrowseFolder(_logPathText);
            panel.Controls.Add(_logPathText);
            panel.Controls.Add(browseButton1);
            y += 45;

            // Local Log Path
            panel.Controls.Add(CreateLabel("Local Log Path:", 20, y));
            _localLogPathText = new TextBox
            {
                Location = new Point(200, y - 3),
                Width = 500
            };
            var browseButton2 = new Button
            {
                Text = "Browse...",
                Location = new Point(710, y - 5),
                Size = new Size(100, 28)
            };
            browseButton2.Click += (s, e) => BrowseFolder(_localLogPathText);
            panel.Controls.Add(_localLogPathText);
            panel.Controls.Add(browseButton2);
            y += 60;

            // Test Mode Checkbox
            var testModeCheck = new CheckBox
            {
                Text = "🧪 Test Mode (Disable Relay - For Testing)",
                Location = new Point(20, y),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkOrange
            };
            testModeCheck.CheckedChanged += (s, e) =>
            {
                if (testModeCheck.Checked)
                {
                    MessageBox.Show(
                        "⚠️ TEST MODE ENABLED\n\n" +
                        "The relay will NOT be triggered.\n" +
                        "All drawer open requests will be simulated.\n\n" +
                        "This allows you to test:\n" +
                        "• User authentication\n" +
                        "• Logging\n" +
                        "• Network communication\n" +
                        "• Client features\n\n" +
                        "Without actually opening the drawer.\n\n" +
                        "Remember to DISABLE test mode for production!",
                        "Test Mode Enabled",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };
            panel.Controls.Add(testModeCheck);
            _testModeCheck = testModeCheck;
            y += 40;

            // Test Relay Button
            var testRelayButton = new Button
            {
                Text = "🔌 Test Relay / Open Drawer",
                Location = new Point(20, y),
                Size = new Size(250, 45),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            testRelayButton.Click += TestRelay_Click;
            panel.Controls.Add(testRelayButton);

            var testResultLabel = new Label
            {
                Name = "testResultLabel",
                Location = new Point(280, y + 12),
                Size = new Size(530, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Text = "Click to test if drawer opens with current settings"
            };
            panel.Controls.Add(testResultLabel);
            y += 60;

            // Info panel
            var infoPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(820, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(255, 250, 205)
            };

            var infoLabel = new Label
            {
                Text = "ℹ Configuration Notes:\n\n" +
                       "• Changes to ports require server restart\n" +
                       "• COM port must match your hardware\n" +
                       "• DTR is the recommended relay type (confirmed working)\n" +
                       "• Network log path should be accessible by all servers",
                Location = new Point(15, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(infoLabel);
            panel.Controls.Add(infoPanel);

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private void CreateUsersTab()
        {
            var tab = new TabPage("👥 User Management");

            // Use simple panel layout instead of SplitContainer
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Left panel - user list (fixed width)
            var leftPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(500, 600),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var listLabel = new Label
            {
                Text = "Users:",
                Location = new Point(10, 10),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _usersList = new ListBox
            {
                Location = new Point(10, 45),
                Size = new Size(480, 545),
                Font = new Font("Consolas", 10),
                ItemHeight = 20
            };
            _usersList.SelectedIndexChanged += UsersList_SelectedIndexChanged;
            _usersList.DoubleClick += (s, e) => EditUser_Click(s, e);

            leftPanel.Controls.Add(listLabel);
            leftPanel.Controls.Add(_usersList);

            // Right panel - actions (fixed width)
            var rightPanel = new Panel
            {
                Location = new Point(520, 10),
                Size = new Size(450, 600),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(10)
            };

            int y = 15;

            _addUserButton = CreateActionButton("➕ Add User", y, Color.FromArgb(16, 124, 16));
            _addUserButton.Click += AddUser_Click;
            rightPanel.Controls.Add(_addUserButton);
            y += 55;

            _editUserButton = CreateActionButton("✏ Edit User", y, Color.FromArgb(0, 120, 215));
            _editUserButton.Click += EditUser_Click;
            _editUserButton.Enabled = false;
            rightPanel.Controls.Add(_editUserButton);
            y += 55;

            _changePasswordButton = CreateActionButton("🔑 Change Password", y, Color.FromArgb(255, 140, 0));
            _changePasswordButton.Click += ChangePassword_Click;
            _changePasswordButton.Enabled = false;
            rightPanel.Controls.Add(_changePasswordButton);
            y += 55;

            _unlockUserButton = CreateActionButton("🔓 Unlock Account", y, Color.FromArgb(255, 185, 0));
            _unlockUserButton.Click += UnlockUser_Click;
            _unlockUserButton.Enabled = false;
            rightPanel.Controls.Add(_unlockUserButton);
            y += 55;

            _deleteUserButton = CreateActionButton("🗑 Delete User", y, Color.FromArgb(196, 43, 28));
            _deleteUserButton.Click += DeleteUser_Click;
            _deleteUserButton.Enabled = false;
            rightPanel.Controls.Add(_deleteUserButton);
            y += 70;

            // User details panel
            _userDetailsLabel = new Label
            {
                Location = new Point(10, y),
                Size = new Size(420, 180),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Padding = new Padding(10),
                Text = "Select a user to view details",
                Font = new Font("Segoe UI", 9)
            };
            rightPanel.Controls.Add(_userDetailsLabel);

            containerPanel.Controls.Add(leftPanel);
            containerPanel.Controls.Add(rightPanel);
            tab.Controls.Add(containerPanel);
            _tabControl.TabPages.Add(tab);
        }

        // Continued in next part...
    }
}
