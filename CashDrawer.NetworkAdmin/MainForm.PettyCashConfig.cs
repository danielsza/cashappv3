using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private ListBox _pettyCashRecipientsList = null!;
        private ListBox _pettyCashReasonsList = null!;
        private Button _addPettyCashRecipientButton = null!;
        private Button _removePettyCashRecipientButton = null!;
        private Button _addPettyCashReasonButton = null!;
        private Button _removePettyCashReasonButton = null!;
        
        private void CreatePettyCashConfigTab()
        {
            var tab = new TabPage("Petty Cash");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            
            int y = 20;
            
            // Title
            var titleLabel = new Label
            {
                Text = "💰 Petty Cash Configuration",
                Location = new Point(20, y),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            panel.Controls.Add(titleLabel);
            y += 45;
            
            // Description
            var descLabel = new Label
            {
                Text = "Configure recipients and reasons for petty cash transactions.\n" +
                       "These lists will be shown to users when processing petty cash.",
                Location = new Point(20, y),
                Size = new Size(760, 40),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(descLabel);
            y += 55;
            
            // Recipients Section
            var recipientsLabel = new Label
            {
                Text = "Recipients (Who can receive petty cash):",
                Location = new Point(20, y),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            panel.Controls.Add(recipientsLabel);
            y += 30;
            
            _pettyCashRecipientsList = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(350, 200),
                Font = new Font("Segoe UI", 9)
            };
            panel.Controls.Add(_pettyCashRecipientsList);
            
            var recipientButtonsY = y;
            _addPettyCashRecipientButton = new Button
            {
                Text = "➕ Add Recipient",
                Location = new Point(380, recipientButtonsY),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _addPettyCashRecipientButton.Click += AddPettyCashRecipientButton_Click;
            panel.Controls.Add(_addPettyCashRecipientButton);
            
            _removePettyCashRecipientButton = new Button
            {
                Text = "➖ Remove",
                Location = new Point(380, recipientButtonsY + 45),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9)
            };
            _removePettyCashRecipientButton.Click += RemovePettyCashRecipientButton_Click;
            panel.Controls.Add(_removePettyCashRecipientButton);
            
            y += 220;
            
            // Reasons Section
            var reasonsLabel = new Label
            {
                Text = "Reasons (Purpose of petty cash):",
                Location = new Point(20, y),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            panel.Controls.Add(reasonsLabel);
            y += 30;
            
            _pettyCashReasonsList = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(350, 200),
                Font = new Font("Segoe UI", 9)
            };
            panel.Controls.Add(_pettyCashReasonsList);
            
            var reasonButtonsY = y;
            _addPettyCashReasonButton = new Button
            {
                Text = "➕ Add Reason",
                Location = new Point(380, reasonButtonsY),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _addPettyCashReasonButton.Click += AddPettyCashReasonButton_Click;
            panel.Controls.Add(_addPettyCashReasonButton);
            
            _removePettyCashReasonButton = new Button
            {
                Text = "➖ Remove",
                Location = new Point(380, reasonButtonsY + 45),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9)
            };
            _removePettyCashReasonButton.Click += RemovePettyCashReasonButton_Click;
            panel.Controls.Add(_removePettyCashReasonButton);
            
            y += 220;
            
            // Save button
            var savePettyCashButton = new Button
            {
                Text = "💾 Save Petty Cash Configuration",
                Location = new Point(20, y),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(0, 102, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            savePettyCashButton.Click += SavePettyCashConfig_Click;
            panel.Controls.Add(savePettyCashButton);
            
            tab.Controls.Add(panel);
            _tabControl.TabPages.Add(tab);
        }
        
        private async void LoadPettyCashConfig()
        {
            if (_selectedServer == null) return;
            
            try
            {
                // Load from server's appsettings
                var response = await SendCommandAsync(_selectedServer, new ServerRequest 
                { 
                    Command = "get_petty_cash_config" 
                });
                
                if (response?.Data != null)
                {
                    // Data might be a JsonElement, so handle it properly
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
                        _pettyCashRecipientsList.Items.Clear();
                        foreach (var recipient in parts[0].Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            _pettyCashRecipientsList.Items.Add(recipient);
                        }
                        
                        _pettyCashReasonsList.Items.Clear();
                        foreach (var reason in parts[1].Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            _pettyCashReasonsList.Items.Add(reason);
                        }
                    }
                }
                else
                {
                    LoadDefaultPettyCashConfig();
                }
            }
            catch
            {
                // Load defaults if server doesn't support this yet
                LoadDefaultPettyCashConfig();
            }
        }
        
        private void LoadDefaultPettyCashConfig()
        {
            _pettyCashRecipientsList.Items.Clear();
            _pettyCashRecipientsList.Items.AddRange(new object[]
            {
                "Store Supplies",
                "Office Supplies",
                "Employee Reimbursement",
                "Postage",
                "Cleaning Supplies",
                "Misc Expense"
            });
            
            _pettyCashReasonsList.Items.Clear();
            _pettyCashReasonsList.Items.AddRange(new object[]
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
        
        private void AddPettyCashRecipientButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new Form
            {
                Text = "Add Recipient",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var label = new Label
            {
                Text = "Enter recipient name:",
                Location = new Point(20, 20),
                Size = new Size(350, 20)
            };
            dialog.Controls.Add(label);
            
            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(350, 25)
            };
            dialog.Controls.Add(textBox);
            
            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 80),
                Size = new Size(75, 30)
            };
            dialog.Controls.Add(okButton);
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(305, 80),
                Size = new Size(75, 30)
            };
            dialog.Controls.Add(cancelButton);
            
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;
            
            if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                _pettyCashRecipientsList.Items.Add(textBox.Text.Trim());
            }
        }
        
        private void RemovePettyCashRecipientButton_Click(object? sender, EventArgs e)
        {
            if (_pettyCashRecipientsList.SelectedIndex >= 0)
            {
                _pettyCashRecipientsList.Items.RemoveAt(_pettyCashRecipientsList.SelectedIndex);
            }
        }
        
        private void AddPettyCashReasonButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new Form
            {
                Text = "Add Reason",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var label = new Label
            {
                Text = "Enter reason:",
                Location = new Point(20, 20),
                Size = new Size(350, 20)
            };
            dialog.Controls.Add(label);
            
            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(350, 25)
            };
            dialog.Controls.Add(textBox);
            
            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 80),
                Size = new Size(75, 30)
            };
            dialog.Controls.Add(okButton);
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(305, 80),
                Size = new Size(75, 30)
            };
            dialog.Controls.Add(cancelButton);
            
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;
            
            if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                _pettyCashReasonsList.Items.Add(textBox.Text.Trim());
            }
        }
        
        private void RemovePettyCashReasonButton_Click(object? sender, EventArgs e)
        {
            if (_pettyCashReasonsList.SelectedIndex >= 0)
            {
                _pettyCashReasonsList.Items.RemoveAt(_pettyCashReasonsList.SelectedIndex);
            }
        }
        
        private async void SavePettyCashConfig_Click(object? sender, EventArgs e)
        {
            if (_selectedServer == null)
            {
                MessageBox.Show("Please select a server first.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var recipients = string.Join("||", _pettyCashRecipientsList.Items.Cast<string>());
                var reasons = string.Join("||", _pettyCashReasonsList.Items.Cast<string>());
                var data = $"{recipients}|||{reasons}";
                
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "set_petty_cash_config",
                    Data = data
                });
                
                if (response != null && response.Status == "success")
                {
                    MessageBox.Show("Petty cash configuration saved successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _statusLabel.Text = "✓ Petty cash configuration saved";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    MessageBox.Show("Failed to save petty cash configuration.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving petty cash configuration: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
