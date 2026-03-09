using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading.Tasks;
using CashDrawer.Shared.Models;
using CashDrawer.Shared.Services;

namespace CashDrawer.Client
{
    public partial class MainForm : Form
    {
        private NetworkClient? _networkClient;
        private string? _connectedServerID;

        // Petty Cash Configuration
        private List<string> _pettyCashRecipients = new();
        private List<string> _pettyCashReasons = new();
        
        // Current Petty Cash Transaction (for printing)
        private string _currentPettyCashRecipient = "";
        private string _currentPettyCashReason = "";

        // Error Notifications
        private CheckBox _enableNotificationsCheck = null!;
        private System.Windows.Forms.Timer? _notificationTimer;
        private DateTime _lastNotificationCheck = DateTime.Now;

        // UI Controls
        private Label _statusLabel = null!;
        private Label _serverLabel = null!;
        private GroupBox _documentGroup = null!;
        private RadioButton _invoiceCheck = null!;
        private RadioButton _pettyCashCheck = null!;
        private RadioButton _changeCheck = null!;
        private RadioButton _refundCheck = null!;
        private RadioButton _bodCheck = null!;
        private RadioButton _eodCheck = null!;
        private GroupBox _transactionGroup = null!;
        private TextBox _docNumberText = null!;
        private TextBox _totalText = null!;
        private TextBox _inText = null!;
        private TextBox _outText = null!;
        private Button _openButton = null!;
        private Label _lastActionLabel = null!;
        private Button _settingsButton = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeClient();
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cleanup
            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();
            _networkClient?.Dispose();
        }

        private void InitializeComponent()
        {
            // Form settings
            this.Text = "Cash Drawer Client";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Status bar at top
            var statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            _statusLabel = new Label
            {
                Text = "● Not Connected",
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _serverLabel = new Label
            {
                Text = "",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                Location = new Point(10, 35)
            };

            statusPanel.Controls.Add(_statusLabel);
            statusPanel.Controls.Add(_serverLabel);
            this.Controls.Add(statusPanel);

            // Main content panel
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            int yPos = 70;

            // Document types (Radio buttons - only ONE selection allowed)
            _documentGroup = new GroupBox
            {
                Text = "Document Type (Select One)",
                Location = new Point(15, yPos),
                Size = new Size(450, 150)
            };

            _invoiceCheck = new RadioButton { Text = "Invoice", Location = new Point(15, 25), AutoSize = true, Checked = true };
            _pettyCashCheck = new RadioButton { Text = "Petty Cash", Location = new Point(15, 55), AutoSize = true };
            _changeCheck = new RadioButton { Text = "Change", Location = new Point(15, 85), AutoSize = true };
            _refundCheck = new RadioButton { Text = "Refund", Location = new Point(230, 25), AutoSize = true };
            _bodCheck = new RadioButton { Text = "BOD", Location = new Point(230, 55), AutoSize = true };
            _eodCheck = new RadioButton { Text = "EOD", Location = new Point(230, 85), AutoSize = true };

            _documentGroup.Controls.AddRange(new Control[] {
                _invoiceCheck, _pettyCashCheck, _changeCheck,
                _refundCheck, _bodCheck, _eodCheck
            });

            contentPanel.Controls.Add(_documentGroup);
            yPos += 160;

            // Transaction details
            _transactionGroup = new GroupBox
            {
                Text = "Transaction Details",
                Location = new Point(15, yPos),
                Size = new Size(450, 160)
            };

            // Document number
            var docNumLabel = new Label
            {
                Text = "Document #:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            _docNumberText = new TextBox
            {
                Location = new Point(120, 27),
                Width = 300
            };
            _docNumberText.KeyDown += TextBox_KeyDown;
            _docNumberText.Enter += (s, e) => BeginInvoke((Action)_docNumberText.SelectAll);

            // Total
            var totalLabel = new Label
            {
                Text = "Total:",
                Location = new Point(15, 60),
                AutoSize = true
            };
            _totalText = new TextBox
            {
                Location = new Point(120, 57),
                Width = 150,
                Text = "0.00"
            };
            _totalText.TextChanged += CalculateOut;
            _totalText.KeyDown += TextBox_KeyDown;
            _totalText.Enter += (s, e) => BeginInvoke((Action)_totalText.SelectAll);

            // IN
            var inLabel = new Label
            {
                Text = "IN:",
                Location = new Point(15, 90),
                AutoSize = true
            };
            _inText = new TextBox
            {
                Location = new Point(120, 87),
                Width = 150,
                Text = "0.00"
            };
            _inText.TextChanged += CalculateOut;
            _inText.KeyDown += TextBox_KeyDown;
            _inText.Enter += (s, e) => BeginInvoke((Action)_inText.SelectAll);

            // OUT (auto-calculated, read-only)
            var outLabel = new Label
            {
                Text = "OUT (auto):",
                Location = new Point(15, 120),
                AutoSize = true
            };
            _outText = new TextBox
            {
                Location = new Point(120, 117),
                Width = 150,
                Text = "0.00",
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            _transactionGroup.Controls.AddRange(new Control[] {
                docNumLabel, _docNumberText,
                totalLabel, _totalText,
                inLabel, _inText,
                outLabel, _outText
            });

            contentPanel.Controls.Add(_transactionGroup);
            yPos += 170;

            // Action buttons
            _openButton = new Button
            {
                Text = "Open Drawer",
                Location = new Point(115, yPos),
                Size = new Size(250, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _openButton.Click += OpenButton_Click;

            contentPanel.Controls.Add(_openButton);
            yPos += 55;

            // Last action label
            _lastActionLabel = new Label
            {
                Text = "Ready",
                Location = new Point(15, yPos),
                Size = new Size(450, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(_lastActionLabel);

            // Enable Notifications checkbox (bottom left)
            _enableNotificationsCheck = new CheckBox
            {
                Text = "🔔 Enable Error Notifications",
                Location = new Point(10, yPos + 55),
                Size = new Size(200, 25),
                Checked = false,
                Font = new Font("Segoe UI", 9)
            };
            _enableNotificationsCheck.CheckedChanged += EnableNotificationsCheck_CheckedChanged;
            contentPanel.Controls.Add(_enableNotificationsCheck);

            // Settings button (bottom right)
            _settingsButton = new Button
            {
                Text = "⚙ Settings",
                Location = new Point(370, yPos + 50),
                Size = new Size(95, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _settingsButton.Click += SettingsButton_Click;
            contentPanel.Controls.Add(_settingsButton);

            this.Controls.Add(contentPanel);
        }

        private void EnableNotificationsCheck_CheckedChanged(object? sender, EventArgs e)
        {
            if (_enableNotificationsCheck.Checked)
            {
                // Start polling for notifications
                StartNotificationPolling();
            }
            else
            {
                // Stop polling
                StopNotificationPolling();
            }
        }

        private void StartNotificationPolling()
        {
            if (_notificationTimer == null)
            {
                _notificationTimer = new System.Windows.Forms.Timer();
                _notificationTimer.Interval = 10000; // Poll every 10 seconds
                _notificationTimer.Tick += async (s, e) => await PollForNotificationsAsync();
            }

            _lastNotificationCheck = DateTime.Now;
            _notificationTimer.Start();
            
            _statusLabel.Text += " | 🔔 Notifications ON";
        }

        private void StopNotificationPolling()
        {
            _notificationTimer?.Stop();
            
            // Remove notification indicator from status
            var statusText = _statusLabel.Text;
            if (statusText.Contains(" | 🔔 Notifications ON"))
            {
                _statusLabel.Text = statusText.Replace(" | 🔔 Notifications ON", "");
            }
        }

        // Track seen notification IDs to prevent duplicates
        private HashSet<string> _seenNotificationIds = new();
        
        private async Task PollForNotificationsAsync()
        {
            if (_networkClient == null || !_networkClient.IsConnected)
                return;

            try
            {
                var request = new ServerRequest
                {
                    Command = "get_notifications",
                    Data = _lastNotificationCheck.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var response = await _networkClient.SendRequestAsync(request);

                if (response?.Status == "success" && response.Data != null)
                {
                    string dataStr;
                    if (response.Data is System.Text.Json.JsonElement jsonElement)
                    {
                        dataStr = jsonElement.GetString() ?? "";
                    }
                    else
                    {
                        dataStr = response.Data.ToString() ?? "";
                    }

                    if (!string.IsNullOrEmpty(dataStr) && dataStr != "[]")
                    {
                        // Deserialize notifications
                        var notifications = JsonSerializer.Deserialize<List<ErrorNotificationDto>>(dataStr);

                        if (notifications != null && notifications.Any())
                        {
                            foreach (var notification in notifications)
                            {
                                // Only show if we haven't seen this notification before
                                if (!_seenNotificationIds.Contains(notification.Id))
                                {
                                    _seenNotificationIds.Add(notification.Id);
                                    ShowNotificationPopup(notification);
                                }
                            }

                            _lastNotificationCheck = notifications.Max(n => n.Timestamp);
                            
                            // Keep seen IDs list from growing too large (keep last 200)
                            if (_seenNotificationIds.Count > 200)
                            {
                                _seenNotificationIds = new HashSet<string>(
                                    _seenNotificationIds.TakeLast(100));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - don't interrupt user
                System.Diagnostics.Debug.WriteLine($"Error polling notifications: {ex.Message}");
            }
        }

        private void ShowNotificationPopup(ErrorNotificationDto notification)
        {
            // Create a toast-style notification that appears at bottom-right of screen
            // and doesn't require focus
            var toastForm = new Form
            {
                Text = "",
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(350, 120),
                BackColor = notification.Severity == "Critical" ? Color.FromArgb(192, 0, 0) : Color.FromArgb(255, 140, 0),
                TopMost = true,
                ShowInTaskbar = false
            };
            
            // Position at bottom-right of primary screen
            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            toastForm.Location = new Point(screen.Right - toastForm.Width - 10, screen.Bottom - toastForm.Height - 10);
            
            var iconLabel = new Label
            {
                Text = notification.Severity == "Critical" ? "❌" : "⚠️",
                Location = new Point(10, 10),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 24),
                ForeColor = Color.White
            };
            toastForm.Controls.Add(iconLabel);
            
            var titleLabel = new Label
            {
                Text = $"Server {notification.Severity}",
                Location = new Point(55, 10),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            toastForm.Controls.Add(titleLabel);
            
            var messageLabel = new Label
            {
                Text = $"{notification.Timestamp:HH:mm:ss} - {notification.Message}",
                Location = new Point(55, 35),
                Size = new Size(280, 50),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White
            };
            toastForm.Controls.Add(messageLabel);
            
            var closeButton = new Button
            {
                Text = "✕",
                Location = new Point(320, 5),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => toastForm.Close();
            toastForm.Controls.Add(closeButton);
            
            // Click anywhere to see details if there's an exception
            if (!string.IsNullOrEmpty(notification.Exception))
            {
                toastForm.Cursor = Cursors.Hand;
                toastForm.Click += (s, e) =>
                {
                    toastForm.Close();
                    var detailsForm = new Form
                    {
                        Text = "Error Details",
                        Size = new Size(600, 400),
                        StartPosition = FormStartPosition.CenterScreen
                    };

                    var textBox = new TextBox
                    {
                        Multiline = true,
                        ScrollBars = ScrollBars.Both,
                        Dock = DockStyle.Fill,
                        Text = notification.Exception,
                        ReadOnly = true,
                        Font = new Font("Consolas", 9)
                    };

                    detailsForm.Controls.Add(textBox);
                    detailsForm.Show();
                };
                
                messageLabel.Text += " (Click for details)";
            }
            
            // Auto-close after 8 seconds
            var closeTimer = new System.Windows.Forms.Timer { Interval = 8000 };
            closeTimer.Tick += (s, e) => { closeTimer.Stop(); toastForm.Close(); };
            closeTimer.Start();
            
            // Show non-modal
            toastForm.Show();
            
            // Play system notification sound
            System.Media.SystemSounds.Exclamation.Play();
        }

        // DTO class for notifications
        private class ErrorNotificationDto
        {
            public string Id { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public string Message { get; set; } = "";
            public string Source { get; set; } = "";
            public string? Exception { get; set; }
            public string Severity { get; set; } = "Warning";
        }


        private async void InitializeClient()
        {
            try
            {
                _networkClient = new NetworkClient();
                await DiscoverAndConnectAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize client: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadPettyCashConfigAsync()
        {
            try
            {
                var request = new ServerRequest
                {
                    Command = "get_petty_cash_config"
                };

                var response = await _networkClient!.SendRequestAsync(request);

                if (response?.Data != null)
                {
                    // Handle JsonElement or string
                    string data;
                    if (response.Data is System.Text.Json.JsonElement jsonElement)
                    {
                        data = jsonElement.GetString() ?? "";
                    }
                    else
                    {
                        data = response.Data.ToString() ?? "";
                    }

                    var parts = data.Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        _pettyCashRecipients = parts[0]
                            .Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                        _pettyCashReasons = parts[1]
                            .Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                    }
                }

                // Use defaults if nothing loaded
                if (!_pettyCashRecipients.Any())
                {
                    _pettyCashRecipients = new List<string>
                    {
                        "Store Supplies",
                        "Office Supplies",
                        "Employee Reimbursement",
                        "Postage",
                        "Cleaning Supplies",
                        "Misc Expense"
                    };
                }

                if (!_pettyCashReasons.Any())
                {
                    _pettyCashReasons = new List<string>
                    {
                        "Office Supplies",
                        "Postage",
                        "Employee Lunch",
                        "Cleaning Supplies",
                        "Emergency Purchase",
                        "Store Maintenance",
                        "Other"
                    };
                }
            }
            catch
            {
                // Use defaults if config loading fails
                _pettyCashRecipients = new List<string>
                {
                    "Store Supplies",
                    "Office Supplies",
                    "Employee Reimbursement",
                    "Postage",
                    "Cleaning Supplies",
                    "Misc Expense"
                };
                _pettyCashReasons = new List<string>
                {
                    "Office Supplies",
                    "Postage",
                    "Employee Lunch",
                    "Cleaning Supplies",
                    "Emergency Purchase",
                    "Store Maintenance",
                    "Other"
                };
            }
        }

        private async Task DiscoverAndConnectAsync()
        {
            _statusLabel.Text = "● Connecting...";
            _statusLabel.ForeColor = Color.Orange;

            try
            {
                // Try saved settings first
                var settings = LoadClientSettings();
                bool connected = false;

                if (settings != null && !string.IsNullOrEmpty(settings.ServerHost))
                {
                    try
                    {
                        _networkClient!.Connect(settings.ServerHost, settings.ServerPort);
                        _connectedServerID = "SAVED";

                        _statusLabel.Text = "● Connected";
                        _statusLabel.ForeColor = Color.Green;
                        _serverLabel.Text = $"Server: {settings.ServerHost}:{settings.ServerPort}";

                        _openButton.Enabled = true;
                        
                        // Load petty cash configuration
                        await LoadPettyCashConfigAsync();
                        
                        connected = true;
                    }
                    catch
                    {
                        // Saved settings didn't work, try auto-discovery
                    }
                }

                // If saved settings didn't work, try auto-discovery
                if (!connected)
                {
                    _statusLabel.Text = "● Discovering servers...";

                    var servers = await _networkClient!.DiscoverServersAsync();

                    if (servers.Count > 0)
                    {
                        // Connect to first server found
                        var server = servers[0];
                        _networkClient.Connect(server.Host, server.Port);
                        _connectedServerID = server.ServerID;

                        _statusLabel.Text = "● Connected";
                        _statusLabel.ForeColor = Color.Green;
                        _serverLabel.Text = $"Server: {server.ServerID} ({server.Host}:{server.Port})";

                        _openButton.Enabled = true;
                        
                        // Load petty cash configuration
                        await LoadPettyCashConfigAsync();
                        
                    }
                    else
                    {
                        _statusLabel.Text = "● No servers found";
                        _statusLabel.ForeColor = Color.Red;
                        _serverLabel.Text = "Click ⚙ Settings to configure manually";

                        _openButton.Enabled = false;
                        
                    }
                }
            }
            catch (Exception)
            {
                _statusLabel.Text = "● Connection failed";
                _statusLabel.ForeColor = Color.Red;
                _serverLabel.Text = "Click ⚙ Settings to configure";

                _openButton.Enabled = false;
                
            }
        }

        private async void OpenButton_Click(object? sender, EventArgs e)
        {
            await OpenDrawerAsync();
        }

        private async Task OpenDrawerAsync()
        {
            // Don't check IsConnected here - let failover logic handle it
            if (_networkClient == null)
            {
                _networkClient = new NetworkClient();
                var settings = LoadClientSettings();
                if (settings != null)
                {
                    try
                    {
                        _networkClient.Connect(settings.ServerHost, settings.ServerPort);
                    }
                    catch
                    {
                        // Will be handled by failover logic below
                    }
                }
                else
                {
                    MessageBox.Show("No server configured. Please configure in Settings.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Get selected document type
            string docType = "Manual";
            if (_invoiceCheck.Checked) docType = "Invoice";
            else if (_pettyCashCheck.Checked) docType = "Petty Cash";
            else if (_changeCheck.Checked) docType = "Change";
            else if (_refundCheck.Checked) docType = "Refund";
            else if (_bodCheck.Checked) docType = "BOD";
            else if (_eodCheck.Checked) docType = "EOD";

            // Show BOD/EOD forms to calculate totals
            if (docType == "BOD")
            {
                // Require authentication
                using var authDialog = new AuthenticationDialog("BOD - Authentication Required");
                if (authDialog.ShowDialog(this) != DialogResult.OK)
                    return;
                
                // Verify credentials and open drawer immediately so user can place cash while counting
                // This doesn't log a transaction yet - just opens the drawer for setup
                try
                {
                    var openRequest = new ServerRequest
                    {
                        Command = "open_drawer_only",  // Just open, no transaction log
                        Password = authDialog.Password,
                        Username = authDialog.Username
                    };
                    
                    var openResponse = await _networkClient!.SendRequestAsync(openRequest);
                    
                    if (openResponse?.Status != "success")
                    {
                        MessageBox.Show(
                            openResponse?.Message ?? "Authentication failed",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    
                    _lastActionLabel.Text = $"✓ Drawer opened for BOD setup at {DateTime.Now:h:mm:ss tt}";
                    _lastActionLabel.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to open drawer: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                
                // Show BOD form for counting
                using var bodForm = new BODFloatForm();
                if (bodForm.ShowDialog(this) != DialogResult.OK)
                    return;
                
                // Now log the actual BOD transaction with the float amount
                try
                {
                    var bodRequest = new ServerRequest
                    {
                        Command = "open_drawer",
                        Password = authDialog.Password,
                        Reason = "Beginning of Day",
                        DocumentType = "BOD",
                        DocumentNumber = DateTime.Today.ToString("yyyyMMdd"),
                        Total = bodForm.TotalFloat,
                        AmountIn = bodForm.TotalFloat,
                        AmountOut = 0
                    };
                    
                    var bodResponse = await _networkClient!.SendRequestAsync(bodRequest);
                    
                    if (bodResponse?.Status == "success")
                    {
                        _lastActionLabel.Text = $"✓ BOD ${bodForm.TotalFloat:F2} recorded by {bodResponse.Name ?? bodResponse.Username} at {DateTime.Now:h:mm:ss tt}";
                        _lastActionLabel.ForeColor = Color.Green;
                        
                        // Update display fields
                        _totalText.Text = bodForm.TotalFloat.ToString("0.00");
                        _inText.Text = bodForm.TotalFloat.ToString("0.00");
                        _outText.Text = "0.00";
                    }
                    else
                    {
                        MessageBox.Show(
                            bodResponse?.Message ?? "Failed to record BOD",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to record BOD: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                
                // Store BOD float on server for EOD calculations
                try
                {
                    await _networkClient!.SendRequestAsync(new ServerRequest
                    {
                        Command = "set_bod_float",
                        Total = bodForm.TotalFloat
                    });
                }
                catch { /* Best effort */ }
                
                // BOD is complete - don't continue to normal transaction flow
                return;
            }
            else if (docType == "EOD")
            {
                // Require authentication
                using var authDialog = new AuthenticationDialog("EOD - Authentication Required");
                if (authDialog.ShowDialog(this) != DialogResult.OK)
                    return;
                
                // Verify credentials first
                try
                {
                    var verifyRequest = new ServerRequest
                    {
                        Command = "authenticate",
                        Username = authDialog.Username,
                        Password = authDialog.Password
                    };
                    
                    var verifyResponse = await _networkClient!.SendRequestAsync(verifyRequest);
                    
                    if (verifyResponse?.Status != "success")
                    {
                        MessageBox.Show(
                            verifyResponse?.Message ?? "Authentication failed",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Authentication failed: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                
                // Get day summary from server
                decimal expectedTotal = 0;
                decimal safeDropTotal = 0;
                var safeDrops = new List<SafeDropInfo>();
                
                try
                {
                    var summaryResponse = await _networkClient!.SendRequestAsync(new ServerRequest
                    {
                        Command = "get_day_summary"
                    });
                    
                    if (summaryResponse?.Status == "success" && summaryResponse.Data != null)
                    {
                        var dataJson = summaryResponse.Data.ToString();
                        if (!string.IsNullOrEmpty(dataJson))
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
                            var root = doc.RootElement;
                            
                            if (root.TryGetProperty("ExpectedTotal", out var expProp))
                                expectedTotal = expProp.GetDecimal();
                            if (root.TryGetProperty("TotalSafeDrops", out var sdProp))
                                safeDropTotal = sdProp.GetDecimal();
                            
                            // Parse safe drops array
                            if (root.TryGetProperty("SafeDrops", out var dropsArray))
                            {
                                foreach (var drop in dropsArray.EnumerateArray())
                                {
                                    var info = new SafeDropInfo
                                    {
                                        Timestamp = drop.TryGetProperty("Timestamp", out var ts) 
                                            ? ts.GetDateTime() : DateTime.Now,
                                        Amount = drop.TryGetProperty("Amount", out var amt) 
                                            ? amt.GetDecimal() : 0,
                                        Username = drop.TryGetProperty("Username", out var user) 
                                            ? user.GetString() ?? "" : "",
                                        Invoice = drop.TryGetProperty("Invoice", out var inv) 
                                            ? inv.GetString() ?? "" : ""
                                    };
                                    safeDrops.Add(info);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not get day summary: {ex.Message}\nUsing manual entry.", 
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                using var eodForm = new EODCountForm(expectedTotal, safeDropTotal, safeDrops);
                if (eodForm.ShowDialog(this) != DialogResult.OK)
                    return;
                
                // Submit EOD transaction with the credentials from auth dialog
                try
                {
                    var eodRequest = new ServerRequest
                    {
                        Command = "open_drawer",
                        Password = authDialog.Password,
                        Reason = "End of Day",
                        DocumentType = "EOD",
                        DocumentNumber = DateTime.Today.ToString("yyyyMMdd"),
                        Total = eodForm.ActualTotal,
                        AmountIn = 0,
                        AmountOut = 0
                    };
                    
                    var eodResponse = await _networkClient!.SendRequestAsync(eodRequest);
                    
                    if (eodResponse?.Status == "success")
                    {
                        _lastActionLabel.Text = $"✓ EOD ${eodForm.ActualTotal:F2} recorded by {eodResponse.Name ?? eodResponse.Username} at {DateTime.Now:h:mm:ss tt}";
                        _lastActionLabel.ForeColor = Color.Green;
                        
                        // Update display fields
                        _totalText.Text = eodForm.ActualTotal.ToString("0.00");
                        _inText.Text = "0.00";
                        _outText.Text = "0.00";
                        
                        // Show variance if any
                        var variance = eodForm.ActualTotal - expectedTotal;
                        if (variance != 0)
                        {
                            var varianceMsg = variance > 0 ? $"OVER by ${variance:F2}" : $"SHORT by ${Math.Abs(variance):F2}";
                            MessageBox.Show(
                                $"EOD Complete\n\nExpected: ${expectedTotal:F2}\nActual: ${eodForm.ActualTotal:F2}\n\n{varianceMsg}",
                                "EOD Summary",
                                MessageBoxButtons.OK,
                                variance == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            eodResponse?.Message ?? "Failed to record EOD",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to record EOD: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                
                // EOD is complete - don't continue to normal transaction flow
                return;
            }
            else if (docType == "Petty Cash")
            {
                string recipient = "";
                string reason = "";
                
                // Prompt for recipient
                var recipientDialog = new Form
                {
                    Text = "Petty Cash Recipient",
                    Size = new Size(400, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                var recipientLabel = new Label
                {
                    Text = "Who is receiving the petty cash?",
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                recipientDialog.Controls.Add(recipientLabel);
                
                var recipientCombo = new ComboBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(340, 25),
                    DropDownStyle = ComboBoxStyle.DropDown
                };
                
                // Use loaded configuration
                if (_pettyCashRecipients.Any())
                {
                    recipientCombo.Items.AddRange(_pettyCashRecipients.ToArray());
                }
                else
                {
                    // Fallback to defaults
                    recipientCombo.Items.AddRange(new object[]
                    {
                        "Store Supplies",
                        "Office Supplies",
                        "Employee Reimbursement",
                        "Postage",
                        "Cleaning Supplies",
                        "Misc Expense"
                    });
                }
                recipientCombo.SelectedIndex = 0;
                recipientDialog.Controls.Add(recipientCombo);
                
                var recipientOkButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(140, 100),
                    Size = new Size(80, 30)
                };
                recipientDialog.Controls.Add(recipientOkButton);
                
                var recipientCancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(230, 100),
                    Size = new Size(80, 30)
                };
                recipientDialog.Controls.Add(recipientCancelButton);
                
                recipientDialog.AcceptButton = recipientOkButton;
                recipientDialog.CancelButton = recipientCancelButton;
                
                if (recipientDialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(recipientCombo.Text))
                {
                    return;
                }
                
                recipient = recipientCombo.Text;
                _currentPettyCashRecipient = recipient;  // Store for printing
                
                // Prompt for reason
                var reasonDialog = new Form
                {
                    Text = "Petty Cash Reason",
                    Size = new Size(400, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                var reasonLabel = new Label
                {
                    Text = "What is the reason for this petty cash?",
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                reasonDialog.Controls.Add(reasonLabel);
                
                var reasonCombo = new ComboBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(340, 25),
                    DropDownStyle = ComboBoxStyle.DropDown
                };
                
                // Use loaded configuration
                if (_pettyCashReasons.Any())
                {
                    reasonCombo.Items.AddRange(_pettyCashReasons.ToArray());
                }
                else
                {
                    // Fallback to defaults
                    reasonCombo.Items.AddRange(new object[]
                    {
                        "Office Supplies",
                        "Postage",
                        "Employee Lunch",
                        "Cleaning Supplies",
                        "Emergency Purchase",
                        "Store Maintenance",
                        "Other"
                    });
                }
                reasonCombo.SelectedIndex = 0;
                reasonDialog.Controls.Add(reasonCombo);
                
                var reasonOkButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(140, 100),
                    Size = new Size(80, 30)
                };
                reasonDialog.Controls.Add(reasonOkButton);
                
                var reasonCancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(230, 100),
                    Size = new Size(80, 30)
                };
                reasonDialog.Controls.Add(reasonCancelButton);
                
                reasonDialog.AcceptButton = reasonOkButton;
                reasonDialog.CancelButton = reasonCancelButton;
                
                if (reasonDialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(reasonCombo.Text))
                {
                    return;
                }
                
                reason = reasonCombo.Text;
                _currentPettyCashReason = reason;  // Store for printing
                
                // Store recipient and reason in document number if empty
                if (string.IsNullOrWhiteSpace(_docNumberText.Text))
                {
                    _docNumberText.Text = $"{recipient} - {reason}";
                }
            }

            // Validate document number for certain types
            if (docType == "Invoice" || docType == "Refund" || docType == "Petty Cash")
            {
                if (string.IsNullOrWhiteSpace(_docNumberText.Text))
                {
                    MessageBox.Show($"{docType} requires a document number", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _docNumberText.Focus();
                    return;
                }
            }

            // Parse amounts
            if (!decimal.TryParse(_totalText.Text, out decimal total))
            {
                MessageBox.Show("Please enter a valid total amount", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _totalText.Focus();
                return;
            }

            // Validate total is required for invoice/refund/petty cash/BOD/EOD
            if (docType == "Invoice" || docType == "Refund" || docType == "Petty Cash" || docType == "BOD" || docType == "EOD")
            {
                if (total == 0)
                {
                    MessageBox.Show($"{docType} requires a total amount", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _totalText.Focus();
                    return;
                }
            }

            if (!decimal.TryParse(_inText.Text, out decimal amountIn)) amountIn = 0;
            if (!decimal.TryParse(_outText.Text, out decimal amountOut)) amountOut = 0;

            // Validate IN is required for Invoice
            if (docType == "Invoice")
            {
                if (amountIn == 0)
                {
                    MessageBox.Show("Invoice requires an IN amount (payment received)", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _inText.Focus();
                    return;
                }
            }

            // For Refund, make total negative
            if (docType == "Refund" && total > 0)
            {
                total = -total;
            }
            
            // For Petty Cash, make total negative (money leaving drawer)
            if (docType == "Petty Cash" && total > 0)
            {
                total = -total;
            }

            // Show password dialog
            using var passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var password = passwordDialog.Password;

            try
            {
                // Send request
                var request = new ServerRequest
                {
                    Command = "open_drawer",
                    Password = password,
                    Reason = "Transaction",
                    DocumentType = docType,
                    DocumentNumber = _docNumberText.Text,
                    Total = total,
                    AmountIn = amountIn,
                    AmountOut = amountOut
                };

                ServerResponse? response = null;

                try
                {
                    response = await _networkClient.SendRequestAsync(request);
                }
                catch (Exception ex)
                {
                    // Primary failed, try backup if configured
                    var settings = LoadClientSettings();
                    if (settings?.BackupEnabled == true 
                        && !string.IsNullOrWhiteSpace(settings.BackupHost))
                    {
                        try
                        {
                            _statusLabel.Text = "● Failover to backup...";
                            _statusLabel.ForeColor = Color.Orange;
                            Application.DoEvents();

                            // Try backup server
                            _networkClient.Dispose();
                            _networkClient = new NetworkClient();
                            _networkClient.Connect(settings.BackupHost, settings.BackupPort);
                            
                            response = await _networkClient.SendRequestAsync(request);
                            
                            // Success! Update status
                            _statusLabel.Text = $"● Connected to {settings.BackupHost} (BACKUP)";
                            _statusLabel.ForeColor = Color.Orange;
                            _serverLabel.Text = $"Server: {settings.BackupHost}:{settings.BackupPort} (BACKUP)";
                        }
                        catch (Exception backupEx)
                        {
                            // Both failed
                            throw new Exception(
                                $"Primary server failed: {ex.Message}\n" +
                                $"Backup server failed: {backupEx.Message}");
                        }
                    }
                    else
                    {
                        // No backup configured, rethrow original error
                        throw;
                    }
                }

                if (response != null && response.Status == "success")
                {
                    _lastActionLabel.Text = $"✓ Drawer opened by {response.Name ?? response.Username} at {DateTime.Now:h:mm:ss tt}";
                    _lastActionLabel.ForeColor = Color.Green;

                    // Show change due popup for transactions where cash goes back to customer
                    if ((docType == "Invoice" || docType == "Refund" || docType == "Change") && amountOut > 0)
                    {
                        MessageBox.Show(
                            $"Change Due:  ${amountOut:F2}",
                            "Change",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.None);
                    }

                    // Print transaction receipt for ALL transactions
                    try
                    {
                        var settings = LoadClientSettings();
                        var printService = new PrintingService(settings?.LogoPath ?? "");
                        var printData = new TransactionPrintData
                        {
                            TransactionType = docType.ToUpper(),
                            Timestamp = DateTime.Now,
                            Username = response.Name ?? response.Username ?? "Unknown",
                            TransactionId = $"{docType.Substring(0, Math.Min(3, docType.Length)).ToUpper()}-{DateTime.Now:yyyyMMddHHmmss}",
                            Amount = total,  // Keep sign - negative for refunds/petty cash
                            Invoice = _docNumberText.Text,
                            Reason = docType == "Petty Cash" ? _currentPettyCashReason : null,
                            RecipientName = docType == "Petty Cash" ? _currentPettyCashRecipient : null
                        };
                        
                        printService.PrintTransaction(printData);
                        _lastActionLabel.Text += " | Receipt printed";
                    }
                    catch (Exception printEx)
                    {
                        // Don't fail the transaction if printing fails
                        _lastActionLabel.Text += $" | Print failed: {printEx.Message}";
                    }

                    // Check if safe drop is needed (transaction > $200)
                    const decimal SAFE_DROP_THRESHOLD = 200.00m;
                    if (Math.Abs(total) >= SAFE_DROP_THRESHOLD && docType == "Invoice")
                    {
                        try
                        {
                            using var safeDropDialog = new SafeDropDialog(Math.Abs(total));
                            safeDropDialog.ShowDialog(this);
                            
                            if (safeDropDialog.DropConfirmed)
                            {
                                // Print safe drop slip
                                var sdSettings = LoadClientSettings();
                                var printService = new PrintingService(sdSettings?.LogoPath ?? "");
                                var safeDropPrint = new TransactionPrintData
                                {
                                    TransactionType = "SAFE DROP",
                                    Timestamp = DateTime.Now,
                                    Username = response.Name ?? response.Username ?? "Unknown",
                                    TransactionId = $"SD-{DateTime.Now:yyyyMMddHHmmss}",
                                    Amount = Math.Abs(total),
                                    Invoice = _docNumberText.Text,
                                    Notes = "Cash deposited to safe"
                                };
                                printService.PrintTransaction(safeDropPrint);
                                _lastActionLabel.Text += " | Safe drop completed";
                                
                                // Record safe drop on server
                                try
                                {
                                    await _networkClient!.SendRequestAsync(new ServerRequest
                                    {
                                        Command = "record_safe_drop",
                                        Username = response.Username,
                                        Total = Math.Abs(total),
                                        DocumentNumber = _docNumberText.Text,
                                        Data = "confirmed"
                                    });
                                }
                                catch { /* Best effort */ }
                            }
                            else
                            {
                                _lastActionLabel.Text += " | Safe drop SKIPPED";
                                // Record skipped safe drop
                                try
                                {
                                    await _networkClient!.SendRequestAsync(new ServerRequest
                                    {
                                        Command = "record_safe_drop",
                                        Username = response.Username,
                                        Total = Math.Abs(total),
                                        DocumentNumber = _docNumberText.Text,
                                        Data = "skipped"
                                    });
                                }
                                catch { /* Best effort */ }
                            }
                        }
                        catch (Exception sdEx)
                        {
                            _lastActionLabel.Text += $" | Safe drop error: {sdEx.Message}";
                        }
                    }
                    
                    // Clear petty cash data
                    _currentPettyCashRecipient = "";
                    _currentPettyCashReason = "";

                    // Clear form
                    ClearForm();
                }
                else if (response != null)
                {
                    MessageBox.Show(response.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _lastActionLabel.Text = $"✗ Failed: {response.Message}";
                    _lastActionLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lastActionLabel.Text = $"✗ Error: {ex.Message}";
                _lastActionLabel.ForeColor = Color.Red;
                
                // Mark as disconnected
                _statusLabel.Text = "● Disconnected";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private void ClearForm()
        {
            _invoiceCheck.Checked = true; // Reset to first option
            _docNumberText.Clear();
            _totalText.Text = "0.00";
            _inText.Text = "0.00";
            _outText.Text = "0.00";
        }

        private void CalculateOut(object? sender, EventArgs e)
        {
            // Don't auto-calculate for BOD/EOD - they set their own values
            if (_bodCheck.Checked || _eodCheck.Checked)
                return;
                
            // Auto-calculate OUT based on document type
            if (!decimal.TryParse(_totalText.Text, out decimal total))
                return; // Can't calculate without valid total
                
            decimal.TryParse(_inText.Text, out decimal amountIn); // Default to 0 if empty
            
            decimal rawChange;
            
            // For Refund: OUT = -Total (negative, money going out to customer)
            // For Invoice: OUT = IN - Total (change to customer)
            // For Petty Cash: OUT = -Total (negative, money going out)
            if (_refundCheck.Checked)
            {
                rawChange = -Math.Abs(total); // Refund: negative OUT (money leaving drawer)
            }
            else if (_pettyCashCheck.Checked)
            {
                rawChange = -Math.Abs(total); // Petty Cash: negative OUT (money leaving drawer)
            }
            else
            {
                rawChange = amountIn - total; // Invoice: change calculation
            }
            
            // Apply Canadian penny rounding (round to nearest nickel)
            decimal roundedChange = CashDrawer.Shared.Utils.CanadianRounding.RoundToNickel(rawChange);
            
            _outText.Text = roundedChange.ToString("0.00");
        }

        /// <summary>
        /// Handle Enter key to move to next field (like Tab)
        /// </summary>
        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevent beep
                
                if (sender == _docNumberText)
                {
                    _totalText.Focus();
                    _totalText.SelectAll();
                }
                else if (sender == _totalText)
                {
                    _inText.Focus();
                    _inText.SelectAll();
                }
                else if (sender == _inText)
                {
                    // Last field - trigger the Open Drawer button
                    _openButton.PerformClick();
                }
            }
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new SettingsDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // Reconnect with new settings
                _networkClient?.Dispose();
                _networkClient = new NetworkClient();
                Task.Run(async () =>
                {
                    try
                    {
                        // Try direct connection first
                        _networkClient.Connect(dialog.ServerHost, dialog.ServerPort);
                        _connectedServerID = "MANUAL";

                        this.Invoke(() =>
                        {
                            _statusLabel.Text = "● Connected";
                            _statusLabel.ForeColor = Color.Green;
                            _serverLabel.Text = $"Server: {dialog.ServerHost}:{dialog.ServerPort}";
                            _openButton.Enabled = true;
                            
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() =>
                        {
                            _statusLabel.Text = "● Connection failed";
                            _statusLabel.ForeColor = Color.Red;
                            _serverLabel.Text = ex.Message;
                            _openButton.Enabled = false;
                            
                        });
                    }
                });
            }
        }

        private ConnectionSettings? LoadClientSettings()
        {
            try
            {
                var configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CashDrawer",
                    "client_settings.json"
                );
                
                if (File.Exists(configFile))
                {
                    var json = File.ReadAllText(configFile);
                    return JsonSerializer.Deserialize<ConnectionSettings>(json);
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }
    }
}
