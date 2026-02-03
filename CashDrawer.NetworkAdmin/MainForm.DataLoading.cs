using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private async void ServersList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_serversList.SelectedIndex < 0) return;
            _selectedServer = _discoveredServers[_serversList.SelectedIndex];
            
            // Check if server is OFFLINE
            if (!_selectedServer.IsConnected)
            {
                _statusLabel.Text = $"🔴 {_selectedServer.ServerID} is OFFLINE\n\n" +
                                    "Only 'Start Server' is available.\n" +
                                    "Use Server Control tab to start it.";
                _statusLabel.ForeColor = System.Drawing.Color.Orange;
                
                // Show limited UI for offline server
                await ShowOfflineServerUI();
                return;
            }
            
            // Server is ONLINE - proceed with authentication
            _statusLabel.Text = $"📡 Connecting to {_selectedServer.ServerID}...";
            _statusLabel.ForeColor = System.Drawing.Color.Orange;
            Application.DoEvents();
            
            try
            {
                // Get users to check if any exist (this doesn't require auth)
                var usersResponse = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "get_all_users" });
                Dictionary<string, User>? usersDict = null;
                
                if (usersResponse?.Data != null)
                {
                    var usersJson = JsonSerializer.Serialize(usersResponse.Data);
                    usersDict = JsonSerializer.Deserialize<Dictionary<string, User>>(usersJson);
                    _selectedServer.Users = usersDict?.Values.ToList() ?? new List<User>();
                }
                
                // If NO users exist, skip authentication and allow creating first admin
                if (_selectedServer.Users == null || _selectedServer.Users.Count == 0)
                {
                    _statusLabel.Text = "⚠️ No users - setup required";
                    _statusLabel.ForeColor = System.Drawing.Color.Orange;
                    
                    // Go directly to first user creation
                    await HandleFirstUserCreation();
                    return;
                }
                
                // Users exist - require admin authentication before allowing access
                if (!await AuthenticateAdminAsync())
                {
                    // Authentication failed - clear selection
                    _selectedServer = null;
                    _serversList.SelectedIndex = -1;
                    _statusLabel.Text = "❌ Authentication failed";
                    _statusLabel.ForeColor = System.Drawing.Color.Red;
                    return;
                }
                
                // Authentication successful - load server data
                await LoadServerDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _selectedServer = null;
                _serversList.SelectedIndex = -1;
            }
        }
        
        /// <summary>
        /// Show limited UI for offline server - only Start button available
        /// </summary>
        private async Task ShowOfflineServerUI()
        {
            // Clear user list
            _usersList.Items.Clear();
            
            // Show message in tab container
            MessageBox.Show(
                $"Server '{_selectedServer?.ServerID}' is currently OFFLINE.\n\n" +
                "To start the server, click the 'Start Server' button.\n\n" +
                "Once the server is running, click 'Discover' again to refresh the status.",
                "Server Offline",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            
            // Try to start the server if user wants
            var startResult = MessageBox.Show(
                "Would you like to start the server now?",
                "Start Server",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (startResult == DialogResult.Yes)
            {
                await StartServerAsync();
            }
        }
        
        /// <summary>
        /// Start the main server via control service
        /// </summary>
        private async Task StartServerAsync()
        {
            if (_selectedServer == null) return;
            
            try
            {
                _statusLabel.Text = "🚀 Starting server...";
                _statusLabel.ForeColor = System.Drawing.Color.Orange;
                Application.DoEvents();
                
                // Send start command to CONTROL service
                var response = await SendControlCommandAsync(_selectedServer, new ServerRequest { Command = "start" });
                
                if (response?.Status == "success")
                {
                    _statusLabel.Text = "✅ Server started!\n\nClick 'Discover' to refresh.";
                    _statusLabel.ForeColor = System.Drawing.Color.Green;
                    
                    MessageBox.Show(
                        "Server started successfully!\n\n" +
                        "Click 'Discover' to refresh the server list and connect.",
                        "Server Started",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    
                    // Clear selection so user can re-discover
                    _selectedServer = null;
                    _serversList.SelectedIndex = -1;
                }
                else
                {
                    MessageBox.Show($"Failed to start server:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async Task HandleFirstUserCreation()
        {
            var result = MessageBox.Show(
                $"Server '{_selectedServer!.ServerID}' has no users!\n\n" +
                "Would you like to create an admin user now?\n\n" +
                "⚠️ This is required before you can manage this server.",
                "First-Time Setup Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                using var dialog = new UserDialog(null, isFirstUser: true);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var addResponse = await SendCommandAsync(_selectedServer, new ServerRequest
                    {
                        Command = "add_user",
                        Password = dialog.Password,
                        Data = new User
                        {
                            Username = dialog.Username,
                            Name = dialog.UserName,  // UserName is the display name
                            Level = UserLevel.Admin  // First user is always Admin
                        }
                    });

                    if (addResponse?.Status == "success")
                    {
                        MessageBox.Show(
                            $"✅ Admin user '{dialog.Username}' created successfully!\n\n" +
                            "You can now login with this account.",
                            "Setup Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        
                        // Refresh user list
                        var usersResponse = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "get_all_users" });
                        if (usersResponse?.Data != null)
                        {
                            var usersJson = JsonSerializer.Serialize(usersResponse.Data);
                            var usersDict = JsonSerializer.Deserialize<Dictionary<string, User>>(usersJson);
                            _selectedServer.Users = usersDict?.Values.ToList() ?? new List<User>();
                        }
                        
                        // Now authenticate with the new user
                        if (await AuthenticateAdminAsync())
                        {
                            await LoadServerDataAsync();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Failed to create user:\n{addResponse?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _selectedServer = null;
                        _serversList.SelectedIndex = -1;
                    }
                }
                else
                {
                    // User cancelled - clear selection
                    _selectedServer = null;
                    _serversList.SelectedIndex = -1;
                }
            }
            else
            {
                // User said No - clear selection
                _selectedServer = null;
                _serversList.SelectedIndex = -1;
            }
        }
        
        private async Task<bool> AuthenticateAdminAsync()
        {
            if (_selectedServer == null) return false;
            
            // Show login dialog
            using var loginDialog = new Form
            {
                Text = "Administrator Login Required",
                Width = 400,
                Height = 250,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var lblInfo = new Label
            {
                Text = "⚠️ Only administrators can access NetworkAdmin.\n\nPlease enter your admin credentials:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(340, 50),
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            
            var lblUsername = new Label
            {
                Text = "Username:",
                Location = new System.Drawing.Point(20, 85),
                Size = new System.Drawing.Size(80, 20)
            };
            
            var txtUsername = new TextBox
            {
                Location = new System.Drawing.Point(110, 82),
                Size = new System.Drawing.Size(250, 23)
            };
            
            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new System.Drawing.Point(20, 115),
                Size = new System.Drawing.Size(80, 20)
            };
            
            var txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(110, 112),
                Size = new System.Drawing.Size(250, 23),
                UseSystemPasswordChar = true
            };
            
            var btnLogin = new Button
            {
                Text = "Login",
                Location = new System.Drawing.Point(200, 160),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.OK
            };
            
            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(290, 160),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            
            loginDialog.Controls.AddRange(new Control[] { lblInfo, lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnCancel });
            loginDialog.AcceptButton = btnLogin;
            loginDialog.CancelButton = btnCancel;
            
            if (loginDialog.ShowDialog(this) != DialogResult.OK)
            {
                return false; // User cancelled
            }
            
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Username and password are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            
            // Send admin authentication request
            _statusLabel.Text = "🔐 Authenticating...";
            _statusLabel.ForeColor = System.Drawing.Color.Orange;
            Application.DoEvents();
            
            try
            {
                var authResponse = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "admin_authenticate",
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                });
                
                if (authResponse?.Status == "success")
                {
                    _statusLabel.Text = $"✅ Authenticated as {txtUsername.Text}";
                    _statusLabel.ForeColor = System.Drawing.Color.Green;
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        authResponse?.Message ?? "Authentication failed",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Authentication error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task LoadServerDataAsync()
        {
            if (_selectedServer == null) return;

            try
            {
                _statusLabel.Text = $"📡 Loading {_selectedServer.ServerID}...";
                _statusLabel.ForeColor = System.Drawing.Color.Orange;
                Application.DoEvents();

                // First, get users to check if any exist
                var usersResponse = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "get_all_users" });
                Dictionary<string, User>? usersDict = null;
                
                if (usersResponse?.Data != null)
                {
                    var usersJson = JsonSerializer.Serialize(usersResponse.Data);
                    usersDict = JsonSerializer.Deserialize<Dictionary<string, User>>(usersJson);
                    _selectedServer.Users = usersDict?.Values.ToList() ?? new List<User>();
                }

                // Users already loaded, authentication already done in ServersList_SelectedIndexChanged
                // Just load the rest of the data

                // Get config
                var configResponse = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "get_config" });
                if (configResponse?.Data != null)
                {
                    var configJson = JsonSerializer.Serialize(configResponse.Data);
                    _selectedServer.Config = JsonSerializer.Deserialize<ServerConfig>(configJson);
                    LoadConfigToUI();
                }

                LoadUsersToUI();
                LoadEmailConfigurationFromServer();
                LoadPettyCashConfig();

                _saveButton.Enabled = true;
                _testRelayButton.Enabled = true;
                _startServerButton.Enabled = true;
                _restartServerButton.Enabled = true;
                _stopServerButton.Enabled = true;
                _addUserButton.Enabled = true;
                _refreshButton.Enabled = true;

                _statusLabel.Text = $"✅ Connected to {_selectedServer.ServerID}\n({_selectedServer.Host}:{_selectedServer.Port})";
                _statusLabel.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Failed to load:\n{ex.Message}";
                _statusLabel.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void LoadConfigToUI()
        {
            if (_selectedServer?.Config == null) return;

            var config = _selectedServer.Config;
            _serverIdText.Text = config.ServerID;
            _portNumber.Value = config.Port;
            _comPortCombo.Text = config.COMPort;
            _relayTypeCombo.SelectedItem = config.RelayPin.ToString();
            
            // Handle legacy milliseconds values (convert to seconds)
            var relayDuration = config.RelayDuration;
            if (relayDuration > 10) // Likely in milliseconds
            {
                relayDuration = relayDuration / 1000.0;
            }
            _relayDurationNumber.Value = Math.Min(5.0M, Math.Max(0.1M, (decimal)relayDuration));
            
            _logPathText.Text = config.LogPath;
            _localLogPathText.Text = config.LocalLogPath;
            _testModeCheck.Checked = config.TestMode;
        }

        private void LoadUsersToUI()
        {
            _usersList.Items.Clear();
            if (_selectedServer?.Users == null) return;

            foreach (var user in _selectedServer.Users.OrderBy(u => u.Username))
            {
                var locked = user.IsLocked ? " [LOCKED]" : "";
                _usersList.Items.Add($"{user.Username} - {user.Role}{locked}");
            }
        }

        private async Task RefreshSelectedServerAsync()
        {
            if (_selectedServer != null)
            {
                await LoadServerDataAsync();
            }
        }
    }
}
