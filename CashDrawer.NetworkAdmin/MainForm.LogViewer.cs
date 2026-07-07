using System;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private TabControl _logTabControl = null!;
        private TextBox _transactionLogText = null!;
        private TextBox _errorLogText = null!;
        private Button _refreshLogsButton = null!;
        private CheckBox _autoRefreshLogsCheck = null!;
        private Timer? _logRefreshTimer;
        private DateTimePicker _logStartDate = null!;
        private DateTimePicker _logEndDate = null!;
        private TextBox _logSearchText = null!;
        // Raw (unfiltered-by-search) log text from the last server fetch, so the
        // Search box can filter CLIENT-SIDE instantly — no server round-trip and
        // no server update required (the date range is still fetched server-side).
        private string _transactionLogRaw = "";
        private string _errorLogRaw = "";
        
        private void CreateLogViewerTab()
        {
            var tab = new TabPage("Logs");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            int y = 20;
            
            // Title
            var titleLabel = new Label
            {
                Text = "📋 Transaction & Error Logs",
                Location = new Point(20, y),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            panel.Controls.Add(titleLabel);
            y += 45;
            
            // Filter controls
            var filterPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(760, 50),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var startLabel = new Label
            {
                Text = "From:",
                Location = new Point(10, 15),
                Size = new Size(40, 20),
                Font = new Font("Segoe UI", 9)
            };
            filterPanel.Controls.Add(startLabel);
            
            _logStartDate = new DateTimePicker
            {
                Location = new Point(55, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short
            };
            _logStartDate.Value = DateTime.Now.AddDays(-7);
            filterPanel.Controls.Add(_logStartDate);
            
            var endLabel = new Label
            {
                Text = "To:",
                Location = new Point(185, 15),
                Size = new Size(25, 20),
                Font = new Font("Segoe UI", 9)
            };
            filterPanel.Controls.Add(endLabel);
            
            _logEndDate = new DateTimePicker
            {
                Location = new Point(215, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short
            };
            filterPanel.Controls.Add(_logEndDate);
            
            var searchLabel = new Label
            {
                Text = "Search:",
                Location = new Point(345, 15),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 9)
            };
            filterPanel.Controls.Add(searchLabel);
            
            _logSearchText = new TextBox
            {
                Location = new Point(400, 12),
                Width = 150,
                PlaceholderText = "Filter logs..."
            };
            // Filter as you type — client-side over the already-loaded logs (instant,
            // no server call). Date-range changes still re-fetch from the server.
            _logSearchText.TextChanged += (s, e) => ApplyLogFilter();
            filterPanel.Controls.Add(_logSearchText);
            
            _refreshLogsButton = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(560, 10),
                Size = new Size(90, 28),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _refreshLogsButton.Click += RefreshLogsButton_Click;
            filterPanel.Controls.Add(_refreshLogsButton);
            
            _autoRefreshLogsCheck = new CheckBox
            {
                Text = "Auto",
                Location = new Point(660, 15),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 8)
            };
            _autoRefreshLogsCheck.CheckedChanged += AutoRefreshLogsCheck_CheckedChanged;
            filterPanel.Controls.Add(_autoRefreshLogsCheck);
            
            panel.Controls.Add(filterPanel);
            y += 65;
            
            // Log tabs
            _logTabControl = new TabControl
            {
                Location = new Point(20, y),
                Size = new Size(760, 450)
            };
            
            // Transaction log tab
            var transactionTab = new TabPage("Transactions");
            _transactionLogText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };
            transactionTab.Controls.Add(_transactionLogText);
            _logTabControl.TabPages.Add(transactionTab);
            
            // Error log tab
            var errorTab = new TabPage("Errors");
            _errorLogText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };
            errorTab.Controls.Add(_errorLogText);
            _logTabControl.TabPages.Add(errorTab);
            
            panel.Controls.Add(_logTabControl);
            
            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
            
            // Changing the date range re-fetches from the server (date filtering is
            // server-side); the Search box filters the result client-side.
            _logStartDate.ValueChanged += (s, e) => RefreshLogs();
            _logEndDate.ValueChanged += (s, e) => RefreshLogs();

            // Initialize timer
            _logRefreshTimer = new Timer { Interval = 30000 }; // 30 seconds
            _logRefreshTimer.Tick += (s, e) => RefreshLogs();
        }
        
        private void RefreshLogsButton_Click(object? sender, EventArgs e)
        {
            RefreshLogs();
        }
        
        private void AutoRefreshLogsCheck_CheckedChanged(object? sender, EventArgs e)
        {
            if (_autoRefreshLogsCheck.Checked)
            {
                _logRefreshTimer?.Start();
                RefreshLogs();
            }
            else
            {
                _logRefreshTimer?.Stop();
            }
        }
        
        private async void RefreshLogs()
        {
            if (_selectedServer == null) return;
            
            try
            {
                _statusLabel.Text = "Loading logs...";
                _statusLabel.ForeColor = Color.Blue;
                
                // Fetch the full date-range set (search is applied client-side in
                // ApplyLogFilter, so we send an EMPTY search term to the server —
                // that way filtering works even against a not-yet-updated server).
                var startDate = _logStartDate.Value.ToString("yyyy-MM-dd");
                var endDate = _logEndDate.Value.ToString("yyyy-MM-dd");

                var transactionLogsResponse = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_transaction_logs",
                    Data = $"{startDate}||{endDate}||"
                });

                if (transactionLogsResponse?.Data != null)
                {
                    // Data might be a JsonElement, so handle it properly
                    string logsData;
                    if (transactionLogsResponse.Data is System.Text.Json.JsonElement jsonElement)
                    {
                        logsData = jsonElement.GetString() ?? "";
                    }
                    else
                    {
                        logsData = transactionLogsResponse.Data.ToString() ?? "";
                    }
                    _transactionLogRaw = logsData;
                }

                // Get error logs
                var errorLogsResponse = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_error_logs",
                    Data = $"{startDate}||{endDate}||"
                });

                if (errorLogsResponse?.Data != null)
                {
                    // Data might be a JsonElement, so handle it properly
                    string logsData;
                    if (errorLogsResponse.Data is System.Text.Json.JsonElement jsonElement)
                    {
                        logsData = jsonElement.GetString() ?? "";
                    }
                    else
                    {
                        logsData = errorLogsResponse.Data.ToString() ?? "";
                    }
                    _errorLogRaw = logsData;
                }

                ApplyLogFilter();
                _statusLabel.Text = $"✓ Logs refreshed at {DateTime.Now:HH:mm:ss}";
                _statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"✗ Error loading logs: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Filter the already-loaded logs by the Search box, client-side (instant).
        private void ApplyLogFilter()
        {
            var term = _logSearchText.Text?.Trim() ?? "";
            _transactionLogText.Text = FilterLogText(_transactionLogRaw, term);
            _errorLogText.Text = FilterLogText(_errorLogRaw, term);
        }

        private static string FilterLogText(string raw, string term)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var lines = raw.Split(new[] { "||" }, StringSplitOptions.None);
            if (!string.IsNullOrWhiteSpace(term))
                lines = lines.Where(l => l.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            return string.Join(Environment.NewLine, lines);
        }
    }
}
