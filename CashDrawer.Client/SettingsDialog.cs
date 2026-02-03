using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using CashDrawer.Shared.Models;

namespace CashDrawer.Client
{
    public class SettingsDialog : Form
    {
        private ListBox _discoveredServersList = null!;
        private TextBox _serverHostText = null!;
        private NumericUpDown _serverPortNumber = null!;
        private TextBox _backupHostText = null!;
        private NumericUpDown _backupPortNumber = null!;
        private Button _okButton = null!;
        private Button _testButton = null!;
        private Button _discoverButton = null!;
        private Button _useSelectedButton = null!;
        private Label _statusLabel = null!;
        private CheckBox _enableBackupCheck = null!;
        private TextBox _logoPathText = null!;
        private Button _browseLogoButton = null!;

        public string ServerHost => _serverHostText.Text.Trim();
        public int ServerPort => (int)_serverPortNumber.Value;
        public string BackupHost => _backupHostText.Text.Trim();
        public int BackupPort => (int)_backupPortNumber.Value;
        public bool BackupEnabled => _enableBackupCheck.Checked;
        public string LogoPath => _logoPathText.Text.Trim();

        private readonly string _configFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CashDrawer",
            "client_settings.json"
        );

        public SettingsDialog()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Client Settings";
            this.Size = new Size(900, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(900, 750);

            // Create main scrollable panel - FILL the space between top and bottom
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            int y = 20;
            int leftMargin = 20;

            // ===== DISCOVERED SERVERS SECTION =====
            var discoverLabel = new Label
            {
                Text = "Discovered Servers:",
                Location = new Point(leftMargin, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            mainPanel.Controls.Add(discoverLabel);
            y += 30;

            _discoveredServersList = new ListBox
            {
                Location = new Point(leftMargin, y),
                Size = new Size(840, 120),
                Font = new Font("Consolas", 9)
            };
            _discoveredServersList.SelectedIndexChanged += DiscoveredServersList_SelectedIndexChanged;
            mainPanel.Controls.Add(_discoveredServersList);
            y += 130;

            _discoverButton = new Button
            {
                Text = "🔍 Discover Servers",
                Location = new Point(leftMargin, y),
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            _discoverButton.Click += DiscoverButton_Click;
            mainPanel.Controls.Add(_discoverButton);

            _useSelectedButton = new Button
            {
                Text = "Use Selected",
                Location = new Point(leftMargin + 200, y),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Enabled = false
            };
            _useSelectedButton.Click += UseSelectedButton_Click;
            mainPanel.Controls.Add(_useSelectedButton);

            var autoConfigButton = new Button
            {
                Text = "Auto-Config",
                Location = new Point(leftMargin + 350, y),
                Size = new Size(120, 38),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            autoConfigButton.Click += AutoConfigButton_Click;
            mainPanel.Controls.Add(autoConfigButton);
            y += 60;

            // ===== MANUAL CONFIGURATION SECTION =====
            var manualLabel = new Label
            {
                Text = "Manual Configuration:",
                Location = new Point(leftMargin, y),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            mainPanel.Controls.Add(manualLabel);
            y += 35;

            // Primary Server
            var primaryLabel = new Label
            {
                Text = "Primary Server:",
                Location = new Point(leftMargin, y),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            mainPanel.Controls.Add(primaryLabel);
            y += 30;

            // Server Address
            var hostLabel = new Label
            {
                Text = "Address:",
                Location = new Point(leftMargin + 20, y),
                Size = new Size(80, 20)
            };
            mainPanel.Controls.Add(hostLabel);

            _serverHostText = new TextBox
            {
                Location = new Point(leftMargin + 120, y - 3),
                Size = new Size(350, 25),
                Text = "localhost"
            };
            mainPanel.Controls.Add(_serverHostText);
            y += 40;

            // Server Port
            var portLabel = new Label
            {
                Text = "Port:",
                Location = new Point(leftMargin + 20, y),
                Size = new Size(80, 20)
            };
            mainPanel.Controls.Add(portLabel);

            _serverPortNumber = new NumericUpDown
            {
                Location = new Point(leftMargin + 120, y - 3),
                Size = new Size(120, 25),
                Minimum = 1000,
                Maximum = 65535,
                Value = 5000
            };
            mainPanel.Controls.Add(_serverPortNumber);
            y += 50;

            // Backup Server Checkbox
            _enableBackupCheck = new CheckBox
            {
                Text = "Enable Backup Server (Failover)",
                Location = new Point(leftMargin, y),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _enableBackupCheck.CheckedChanged += EnableBackupCheck_CheckedChanged;
            mainPanel.Controls.Add(_enableBackupCheck);
            y += 35;

            // Backup Address
            var backupHostLabel = new Label
            {
                Text = "Address:",
                Location = new Point(leftMargin + 20, y),
                Size = new Size(80, 20)
            };
            mainPanel.Controls.Add(backupHostLabel);

            _backupHostText = new TextBox
            {
                Location = new Point(leftMargin + 120, y - 3),
                Size = new Size(350, 25),
                Enabled = false
            };
            mainPanel.Controls.Add(_backupHostText);
            y += 40;

            // Backup Port
            var backupPortLabel = new Label
            {
                Text = "Port:",
                Location = new Point(leftMargin + 20, y),
                Size = new Size(80, 20)
            };
            mainPanel.Controls.Add(backupPortLabel);

            _backupPortNumber = new NumericUpDown
            {
                Location = new Point(leftMargin + 120, y - 3),
                Size = new Size(120, 25),
                Minimum = 1000,
                Maximum = 65535,
                Value = 5000,
                Enabled = false
            };
            mainPanel.Controls.Add(_backupPortNumber);
            y += 55;

            // ===== LOGO SECTION =====
            var logoSectionLabel = new Label
            {
                Text = "📄 Receipt Logo",
                Location = new Point(leftMargin, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            mainPanel.Controls.Add(logoSectionLabel);
            y += 30;

            var logoPathLabel = new Label
            {
                Text = "Logo Path:",
                Location = new Point(leftMargin + 20, y),
                Size = new Size(100, 20)
            };
            mainPanel.Controls.Add(logoPathLabel);

            _logoPathText = new TextBox
            {
                Location = new Point(leftMargin + 120, y - 3),
                Size = new Size(550, 25),
                PlaceholderText = "Path to logo image (PNG, JPG) - leave empty for no logo"
            };
            mainPanel.Controls.Add(_logoPathText);

            _browseLogoButton = new Button
            {
                Text = "Browse...",
                Location = new Point(leftMargin + 680, y - 5),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat
            };
            _browseLogoButton.Click += BrowseLogoButton_Click;
            mainPanel.Controls.Add(_browseLogoButton);
            y += 40;

            var logoHintLabel = new Label
            {
                Text = "💡 Logo will appear at the top of transaction receipts. Recommended size: 150-200px wide.",
                Location = new Point(leftMargin + 120, y),
                Size = new Size(550, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            mainPanel.Controls.Add(logoHintLabel);
            y += 35;

            // Test Connection Button
            _testButton = new Button
            {
                Text = "🔍 Test Primary Connection",
                Location = new Point(leftMargin + 120, y),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _testButton.Click += TestButton_Click;
            mainPanel.Controls.Add(_testButton);
            y += 50;

            // Status Label
            _statusLabel = new Label
            {
                Location = new Point(leftMargin, y),
                Size = new Size(840, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10),
                Text = "",
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            mainPanel.Controls.Add(_statusLabel);

            // Bottom buttons panel - ADD FIRST
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            _okButton = new Button
            {
                Text = "Save",
                Location = new Point(720, 15),
                Size = new Size(80, 40),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(810, 15),
                Size = new Size(75, 40),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };

            buttonPanel.Controls.Add(_okButton);
            buttonPanel.Controls.Add(cancelButton);
            
            // Add in correct order: bottom panel first, then main panel fills rest
            this.Controls.Add(buttonPanel);
            this.Controls.Add(mainPanel);

            this.AcceptButton = _okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = File.ReadAllText(_configFile);
                    var settings = JsonSerializer.Deserialize<ConnectionSettings>(json);
                    if (settings != null)
                    {
                        _serverHostText.Text = settings.ServerHost;
                        _serverPortNumber.Value = settings.ServerPort;
                        
                        if (!string.IsNullOrEmpty(settings.BackupHost))
                        {
                            _enableBackupCheck.Checked = true;
                            _backupHostText.Text = settings.BackupHost;
                            _backupPortNumber.Value = settings.BackupPort;
                        }
                        
                        _logoPathText.Text = settings.LogoPath ?? "";
                    }
                }
            }
            catch
            {
                // Use defaults
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var settings = new ConnectionSettings
                {
                    ServerHost = ServerHost,
                    ServerPort = ServerPort,
                    BackupHost = BackupEnabled ? BackupHost : "",
                    BackupPort = BackupEnabled ? BackupPort : 5000,
                    BackupEnabled = BackupEnabled,
                    LogoPath = LogoPath
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_configFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_configFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnableBackupCheck_CheckedChanged(object? sender, EventArgs e)
        {
            _backupHostText.Enabled = _enableBackupCheck.Checked;
            _backupPortNumber.Enabled = _enableBackupCheck.Checked;
        }

        private void BrowseLogoButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select Logo Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|PNG Files|*.png|JPEG Files|*.jpg;*.jpeg|All Files|*.*",
                FilterIndex = 1
            };

            if (!string.IsNullOrEmpty(_logoPathText.Text) && File.Exists(_logoPathText.Text))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(_logoPathText.Text);
                dialog.FileName = Path.GetFileName(_logoPathText.Text);
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _logoPathText.Text = dialog.FileName;
            }
        }

        private async void DiscoverButton_Click(object? sender, EventArgs e)
        {
            _discoveredServersList.Items.Clear();
            _discoveredServersList.Items.Add("Discovering...");
            _discoverButton.Enabled = false;
            _statusLabel.Text = "Searching for servers...";
            _statusLabel.ForeColor = Color.Orange;

            try
            {
                using var client = new NetworkClient();
                var servers = await client.DiscoverServersAsync(timeout: 5);

                _discoveredServersList.Items.Clear();

                if (servers.Count > 0)
                {
                    foreach (var server in servers)
                    {
                        _discoveredServersList.Items.Add($"{server.ServerID} - {server.Host}:{server.Port}");
                    }
                    _discoveredServersList.Tag = servers; // Store servers list
                    _statusLabel.Text = $"Found {servers.Count} server(s)";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    _discoveredServersList.Items.Add("No servers found");
                    _statusLabel.Text = "No servers found. Try manual connection.";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                _discoveredServersList.Items.Clear();
                _discoveredServersList.Items.Add($"Discovery error: {ex.Message}");
                _statusLabel.Text = $"Discovery failed: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
                
                MessageBox.Show(
                    $"Discovery failed: {ex.Message}\n\nUse manual configuration instead.\n\nEnter server IP address in 'Primary Server' field below.",
                    "Discovery Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _discoverButton.Enabled = true;
            }
        }

        private void DiscoveredServersList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _useSelectedButton.Enabled = _discoveredServersList.SelectedIndex >= 0 
                && _discoveredServersList.Tag is List<ServerInfo>;
        }

        private void UseSelectedButton_Click(object? sender, EventArgs e)
        {
            if (_discoveredServersList.Tag is List<ServerInfo> servers
                && _discoveredServersList.SelectedIndex >= 0)
            {
                var selected = servers[_discoveredServersList.SelectedIndex];
                
                // Ask user what to do with this server
                var result = MessageBox.Show(
                    $"Configure {selected.ServerID} ({selected.Host}:{selected.Port}) as:\n\n" +
                    "YES = Primary Server\n" +
                    "NO = Backup Server\n" +
                    "CANCEL = Don't use",
                    "Configure Server",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // Set as primary
                    _serverHostText.Text = selected.Host;
                    _serverPortNumber.Value = selected.Port;
                    
                    // Auto-populate backup with another server if available
                    if (servers.Count > 1)
                    {
                        var backup = servers.FirstOrDefault(s => s.Host != selected.Host);
                        if (backup != null)
                        {
                            _backupHostText.Text = backup.Host;
                            _backupPortNumber.Value = backup.Port;
                            _enableBackupCheck.Checked = true;
                            
                            _statusLabel.Text = $"Primary: {selected.Host}, Backup: {backup.Host}";
                            _statusLabel.ForeColor = Color.Green;
                            return;
                        }
                    }
                    
                    _statusLabel.Text = $"Primary set to: {selected.Host}:{selected.Port}";
                    _statusLabel.ForeColor = Color.Blue;
                }
                else if (result == DialogResult.No)
                {
                    // Set as backup
                    _backupHostText.Text = selected.Host;
                    _backupPortNumber.Value = selected.Port;
                    _enableBackupCheck.Checked = true;
                    
                    _statusLabel.Text = $"Backup set to: {selected.Host}:{selected.Port}";
                    _statusLabel.ForeColor = Color.Blue;
                }
            }
        }

        private void AutoConfigButton_Click(object? sender, EventArgs e)
        {
            if (_discoveredServersList.Tag is List<ServerInfo> servers && servers.Count > 0)
            {
                // Get unique servers by host
                var uniqueServers = servers
                    .GroupBy(s => s.Host)
                    .Select(g => g.First())
                    .Take(2)
                    .ToList();

                if (uniqueServers.Count >= 1)
                {
                    // Set primary
                    _serverHostText.Text = uniqueServers[0].Host;
                    _serverPortNumber.Value = uniqueServers[0].Port;

                    if (uniqueServers.Count >= 2)
                    {
                        // Set backup
                        _backupHostText.Text = uniqueServers[1].Host;
                        _backupPortNumber.Value = uniqueServers[1].Port;
                        _enableBackupCheck.Checked = true;

                        _statusLabel.Text = $"✅ Configured: Primary={uniqueServers[0].Host}, Backup={uniqueServers[1].Host}";
                        _statusLabel.ForeColor = Color.Green;

                        MessageBox.Show(
                            $"Auto-configured successfully!\n\n" +
                            $"Primary Server: {uniqueServers[0].Host}:{uniqueServers[0].Port}\n" +
                            $"Backup Server: {uniqueServers[1].Host}:{uniqueServers[1].Port}\n\n" +
                            $"Click 'Save' to apply these settings.",
                            "Auto-Configuration Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        _statusLabel.Text = $"✅ Configured: Primary={uniqueServers[0].Host} (no backup available)";
                        _statusLabel.ForeColor = Color.Blue;

                        MessageBox.Show(
                            $"Auto-configured with primary server only:\n\n" +
                            $"Primary Server: {uniqueServers[0].Host}:{uniqueServers[0].Port}\n\n" +
                            $"Only one unique server found. Click 'Save' to apply.",
                            "Auto-Configuration Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show(
                    "No servers available for auto-configuration.\n\nClick 'Discover Servers' first.",
                    "No Servers",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void TestButton_Click(object? sender, EventArgs e)
        {
            _testButton.Enabled = false;
            _statusLabel.Text = "Testing connection...";
            _statusLabel.ForeColor = Color.Orange;

            try
            {
                using var client = new NetworkClient();
                client.Connect(_serverHostText.Text, (int)_serverPortNumber.Value);
                
                var response = await client.SendRequestAsync(new ServerRequest
                {
                    Command = "ping"
                });

                if (response.Status == "success")
                {
                    _statusLabel.Text = $"✓ Connected to server: {response.ServerID}";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    _statusLabel.Text = $"Connection failed: {response.Message}";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"✗ Connection failed: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _testButton.Enabled = true;
            }
        }
    }

    public class ConnectionSettings
    {
        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 5000;
        public string BackupHost { get; set; } = "";
        public int BackupPort { get; set; } = 5000;
        public bool BackupEnabled { get; set; } = false;
        public string LogoPath { get; set; } = "";  // Path to logo image for receipts
    }
}
