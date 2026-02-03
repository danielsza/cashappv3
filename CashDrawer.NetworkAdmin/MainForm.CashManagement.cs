using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private ListBox _cashServersList = null!;
        private Label _combinedTotalLabel = null!;
        private Label _lastUpdatedLabel = null!;
        private Button _refreshCashButton = null!;
        private Button _viewDetailsButton = null!;
        private ListBox _recipientsList = null!;
        private Button _addRecipientButton = null!;
        private Button _removeRecipientButton = null!;

        private void CreateCashManagementTab()
        {
            var tab = new TabPage("Cash Management");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            
            int y = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "💰 Cash Management",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            panel.Controls.Add(titleLabel);
            y += 45;

            // Current Totals Section
            var totalsLabel = new Label
            {
                Text = "CURRENT CASH TOTALS",
                Location = new Point(20, y),
                Size = new Size(800, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            panel.Controls.Add(totalsLabel);
            y += 35;

            // Server cash list
            _cashServersList = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(600, 150),
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(_cashServersList);
            y += 160;

            // Combined total
            var separator = new Label
            {
                Location = new Point(20, y),
                Size = new Size(600, 2),
                BorderStyle = BorderStyle.Fixed3D
            };
            panel.Controls.Add(separator);
            y += 15;

            var totalLabel = new Label
            {
                Text = "TOTAL CASH (ALL SERVERS):",
                Location = new Point(20, y),
                Size = new Size(350, 50),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(totalLabel);

            _combinedTotalLabel = new Label
            {
                Text = "$0.00",
                Location = new Point(380, y),
                Size = new Size(240, 50),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(_combinedTotalLabel);
            y += 65;

            _lastUpdatedLabel = new Label
            {
                Text = "Last Updated: --",
                Location = new Point(20, y),
                Size = new Size(600, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(_lastUpdatedLabel);
            y += 35;

            // Buttons
            _refreshCashButton = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(20, y),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _refreshCashButton.Click += async (s, e) => await RefreshCashTotalsAsync();
            panel.Controls.Add(_refreshCashButton);

            _viewDetailsButton = new Button
            {
                Text = "📊 View Details",
                Location = new Point(150, y),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _viewDetailsButton.Click += ViewDetailsButton_Click;
            _viewDetailsButton.Enabled = false;
            panel.Controls.Add(_viewDetailsButton);
            y += 60;

            // Petty Cash Recipients Section
            var recipientsLabel = new Label
            {
                Text = "PETTY CASH RECIPIENTS",
                Location = new Point(20, y),
                Size = new Size(800, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            panel.Controls.Add(recipientsLabel);
            y += 35;

            var recipientsInfo = new Label
            {
                Text = "Manage the list of people who can receive petty cash disbursements.",
                Location = new Point(20, y),
                Size = new Size(800, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(recipientsInfo);
            y += 30;

            _recipientsList = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(400, 200),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(_recipientsList);

            // Recipient buttons
            _addRecipientButton = new Button
            {
                Text = "+ Add Recipient",
                Location = new Point(430, y),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _addRecipientButton.Click += AddRecipientButton_Click;
            panel.Controls.Add(_addRecipientButton);

            _removeRecipientButton = new Button
            {
                Text = "✏️ Edit",
                Location = new Point(430, y + 45),
                Size = new Size(65, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _removeRecipientButton.Click += EditRecipientButton_Click;
            panel.Controls.Add(_removeRecipientButton);

            var deleteRecipientButton = new Button
            {
                Text = "❌ Delete",
                Location = new Point(505, y + 45),
                Size = new Size(65, 35),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            deleteRecipientButton.Click += DeleteRecipientButton_Click;
            panel.Controls.Add(deleteRecipientButton);

            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }

        private async Task RefreshCashTotalsAsync()
        {
            if (_selectedServer == null)
            {
                MessageBox.Show("Please select a server first", "No Server Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _refreshCashButton.Enabled = false;
                _cashServersList.Items.Clear();
                
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_combined_cash"
                });

                if (response?.Status == "success" && response.Data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(response.Data);
                    var cashData = System.Text.Json.JsonSerializer.Deserialize<CombinedCashData>(json);

                    if (cashData != null)
                    {
                        foreach (var server in cashData.Servers)
                        {
                            _cashServersList.Items.Add($"{server.ServerID,-20} ${server.CurrentTotal,10:F2}");
                        }

                        _combinedTotalLabel.Text = $"${cashData.TotalCash:F2}";
                        _lastUpdatedLabel.Text = $"Last Updated: {cashData.LastUpdated:yyyy-MM-dd HH:mm:ss}";
                        _viewDetailsButton.Enabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Failed to retrieve cash data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing cash totals:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _refreshCashButton.Enabled = true;
            }
        }

        private void ViewDetailsButton_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Detailed cash breakdown view will be implemented.\n\n" +
                "This will show:\n" +
                "- BOD starting float\n" +
                "- Transactions during day\n" +
                "- Expected vs Actual\n" +
                "- Any adjustments",
                "Cash Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void LoadRecipientsAsync()
        {
            if (_selectedServer == null) return;

            try
            {
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_recipients"
                });

                if (response?.Status == "success" && response.Data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(response.Data);
                    var recipients = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);

                    _recipientsList.Items.Clear();
                    if (recipients != null)
                    {
                        foreach (var recipient in recipients)
                        {
                            _recipientsList.Items.Add(recipient);
                        }
                    }
                }
            }
            catch
            {
                // Silent fail on load
            }
        }

        private async void AddRecipientButton_Click(object? sender, EventArgs e)
        {
            if (_selectedServer == null)
            {
                MessageBox.Show("Please select a server first", "No Server Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var inputDialog = new InputDialog("Add Recipient", "Recipient Name:", "");
            if (inputDialog.ShowDialog(this) == DialogResult.OK)
            {
                var name = inputDialog.Value.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                try
                {
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest
                    {
                        Command = "add_recipient",
                        Data = new { Name = name }
                    });

                    if (response?.Status == "success")
                    {
                        _recipientsList.Items.Add(name);
                        MessageBox.Show($"✅ Recipient '{name}' added", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding recipient:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EditRecipientButton_Click(object? sender, EventArgs e)
        {
            if (_recipientsList.SelectedItem == null)
            {
                MessageBox.Show("Please select a recipient to edit", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var oldName = _recipientsList.SelectedItem.ToString();
            using var inputDialog = new InputDialog("Edit Recipient", "Recipient Name:", oldName ?? "");
            if (inputDialog.ShowDialog(this) == DialogResult.OK)
            {
                var newName = inputDialog.Value.Trim();
                if (!string.IsNullOrWhiteSpace(newName) && newName != oldName)
                {
                    var index = _recipientsList.SelectedIndex;
                    _recipientsList.Items[index] = newName;
                    // TODO: Update on server
                }
            }
        }

        private async void DeleteRecipientButton_Click(object? sender, EventArgs e)
        {
            if (_recipientsList.SelectedItem == null)
            {
                MessageBox.Show("Please select a recipient to delete", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var name = _recipientsList.SelectedItem.ToString();
            var result = MessageBox.Show(
                $"Delete recipient '{name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes && _selectedServer != null)
            {
                try
                {
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest
                    {
                        Command = "remove_recipient",
                        Data = new { Name = name }
                    });

                    if (response?.Status == "success")
                    {
                        _recipientsList.Items.Remove(name);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting recipient:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class CombinedCashData
        {
            public decimal TotalCash { get; set; }
            public List<ServerCashData> Servers { get; set; } = new();
            public DateTime LastUpdated { get; set; }
        }

        private class ServerCashData
        {
            public string ServerID { get; set; } = "";
            public decimal CurrentTotal { get; set; }
        }

        private class InputDialog : Form
        {
            private TextBox _inputText = null!;
            public string Value => _inputText.Text;

            public InputDialog(string title, string label, string defaultValue)
            {
                this.Text = title;
                this.Size = new Size(400, 180);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;

                var labelControl = new Label
                {
                    Text = label,
                    Location = new Point(20, 20),
                    Size = new Size(340, 25),
                    Font = new Font("Segoe UI", 10)
                };
                this.Controls.Add(labelControl);

                _inputText = new TextBox
                {
                    Location = new Point(20, 50),
                    Width = 340,
                    Text = defaultValue,
                    Font = new Font("Segoe UI", 10)
                };
                this.Controls.Add(_inputText);

                var okButton = new Button
                {
                    Text = "OK",
                    Location = new Point(190, 90),
                    Size = new Size(80, 35),
                    DialogResult = DialogResult.OK
                };
                this.Controls.Add(okButton);

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Location = new Point(280, 90),
                    Size = new Size(80, 35),
                    DialogResult = DialogResult.Cancel
                };
                this.Controls.Add(cancelButton);

                this.AcceptButton = okButton;
                this.CancelButton = cancelButton;
            }
        }
    }
}
