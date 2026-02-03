using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private ListBox _errorLogsList = null!;
        private TextBox _errorDetailsText = null!;
        private Label _errorSummaryLabel = null!;
        private Button _refreshErrorsButton = null!;
        private ComboBox _errorDaysCombo = null!;

        private void CreateErrorViewerTab()
        {
            var tab = new TabPage("Error Logs");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            
            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "📋 Server Error Logs",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            panel.Controls.Add(titleLabel);
            y += 40;

            // Summary
            _errorSummaryLabel = new Label
            {
                Text = "Loading error summary...",
                Location = new Point(20, y),
                Size = new Size(700, 80),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };
            panel.Controls.Add(_errorSummaryLabel);
            y += 90;

            // Filter controls
            var filterLabel = new Label
            {
                Text = "Show logs from:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(filterLabel);

            _errorDaysCombo = new ComboBox
            {
                Location = new Point(130, y),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _errorDaysCombo.Items.AddRange(new object[] { "Today", "Last 3 Days", "Last 7 Days", "Last 30 Days" });
            _errorDaysCombo.SelectedIndex = 0;
            _errorDaysCombo.SelectedIndexChanged += async (s, e) => await RefreshErrorLogsAsync();
            panel.Controls.Add(_errorDaysCombo);

            _refreshErrorsButton = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(300, y - 3),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _refreshErrorsButton.Click += async (s, e) => await RefreshErrorLogsAsync();
            panel.Controls.Add(_refreshErrorsButton);

            var clearOldButton = new Button
            {
                Text = "🗑️ Clean Old Logs",
                Location = new Point(430, y - 3),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            clearOldButton.Click += async (s, e) => await CleanOldLogsAsync();
            panel.Controls.Add(clearOldButton);
            y += 50;

            // Split container for list and details
            var splitContainer = new SplitContainer
            {
                Location = new Point(20, y),
                Size = new Size(900, 400),
                Orientation = Orientation.Vertical,
                SplitterDistance = 200
            };

            // Left: Log file list
            var listLabel = new Label
            {
                Text = "Log Files:",
                Location = new Point(5, 5),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            splitContainer.Panel1.Controls.Add(listLabel);

            _errorLogsList = new ListBox
            {
                Location = new Point(5, 30),
                Size = new Size(185, 365),
                Font = new Font("Consolas", 9)
            };
            _errorLogsList.SelectedIndexChanged += ErrorLogsList_SelectedIndexChanged;
            splitContainer.Panel1.Controls.Add(_errorLogsList);

            // Right: Error details
            var detailsLabel = new Label
            {
                Text = "Error Details:",
                Location = new Point(5, 5),
                Size = new Size(680, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            splitContainer.Panel2.Controls.Add(detailsLabel);

            _errorDetailsText = new TextBox
            {
                Location = new Point(5, 30),
                Size = new Size(680, 365),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                WordWrap = false
            };
            splitContainer.Panel2.Controls.Add(_errorDetailsText);

            panel.Controls.Add(splitContainer);

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private async Task RefreshErrorLogsAsync()
        {
            if (_selectedServer == null)
            {
                _errorSummaryLabel.Text = "⚠️ No server selected";
                return;
            }

            try
            {
                _refreshErrorsButton.Enabled = false;
                _errorSummaryLabel.Text = "Loading...";

                var days = _errorDaysCombo.SelectedIndex switch
                {
                    0 => 1,   // Today
                    1 => 3,   // Last 3 days
                    2 => 7,   // Last 7 days
                    3 => 30,  // Last 30 days
                    _ => 1
                };

                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_error_summary",
                    Data = new { Days = days }
                });

                if (response?.Status == "success" && response.Data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(response.Data);
                    var summary = System.Text.Json.JsonSerializer.Deserialize<ErrorSummary>(json);

                    if (summary != null)
                    {
                        _errorSummaryLabel.Text = 
                            $"🔴 Critical: {summary.CriticalCount}  " +
                            $"⚠️ Errors: {summary.ErrorCount}  " +
                            $"⚡ Warnings: {summary.WarningCount}\n" +
                            $"Period: Last {days} day(s)  |  " +
                            $"Updated: {DateTime.Now:HH:mm:ss}";

                        _errorSummaryLabel.ForeColor = summary.CriticalCount > 0 ? Color.Red :
                                                       summary.ErrorCount > 0 ? Color.DarkOrange :
                                                       Color.Green;
                    }
                }

                // Get log file list
                var filesResponse = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_log_files",
                    Data = new { Days = days }
                });

                if (filesResponse?.Status == "success" && filesResponse.Data != null)
                {
                    var filesJson = System.Text.Json.JsonSerializer.Serialize(filesResponse.Data);
                    var files = System.Text.Json.JsonSerializer.Deserialize<string[]>(filesJson);

                    _errorLogsList.Items.Clear();
                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            var fileName = System.IO.Path.GetFileName(file);
                            _errorLogsList.Items.Add(fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load error logs:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _refreshErrorsButton.Enabled = true;
            }
        }

        private async void ErrorLogsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_errorLogsList.SelectedItem == null || _selectedServer == null)
                return;

            try
            {
                var filename = _errorLogsList.SelectedItem.ToString();
                
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_log_content",
                    Data = new { FileName = filename }
                });

                if (response?.Status == "success" && response.Data != null)
                {
                    var content = response.Data.ToString();
                    _errorDetailsText.Text = content;
                }
            }
            catch (Exception ex)
            {
                _errorDetailsText.Text = $"Error loading log file:\n{ex.Message}";
            }
        }

        private async Task CleanOldLogsAsync()
        {
            if (_selectedServer == null) return;

            var result = MessageBox.Show(
                "This will delete log files older than 30 days.\n\nContinue?",
                "Clean Old Logs",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "clean_old_logs"
                });

                if (response?.Status == "success")
                {
                    MessageBox.Show("✅ Old logs cleaned successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RefreshErrorLogsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clean logs:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ErrorSummary
        {
            public int ErrorCount { get; set; }
            public int CriticalCount { get; set; }
            public int WarningCount { get; set; }
        }
    }
}
