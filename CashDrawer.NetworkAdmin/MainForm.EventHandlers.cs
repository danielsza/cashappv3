using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private void UsersList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = _usersList.SelectedIndex >= 0;
            _editUserButton.Enabled = hasSelection;
            _deleteUserButton.Enabled = hasSelection;
            _resetPasswordButton.Enabled = hasSelection;
            
            if (hasSelection && _selectedServer?.Users != null)
            {
                var user = _selectedServer.Users[_usersList.SelectedIndex];
                _unlockUserButton.Enabled = user.IsLocked;
                
                _userDetailsLabel.Text = $@"Username: {user.Username}
Role: {user.Role}
Status: {(user.IsLocked ? "LOCKED" : "Active")}
Failed Attempts: {user.FailedAttempts}
Last Login: {(user.LastLogin.HasValue ? user.LastLogin.Value.ToString("g") : "Never")}";
            }
            else
            {
                _unlockUserButton.Enabled = false;
                _userDetailsLabel.Text = "";
            }
        }

        private async Task SaveButton_Click()
        {
            if (_selectedServer == null) return;

            try
            {
                _statusLabel.Text = "💾 Saving changes...";
                _statusLabel.ForeColor = Color.Orange;
                _saveButton.Enabled = false;
                Application.DoEvents();

                var config = new ServerConfig
                {
                    ServerID = _serverIdText.Text,
                    Port = (int)_portNumber.Value,
                    DiscoveryPort = _selectedServer.Config?.DiscoveryPort ?? 5001,
                    COMPort = _comPortCombo.Text,
                    RelayPin = Enum.Parse<RelayType>(_relayTypeCombo.SelectedItem?.ToString() ?? "DTR"),
                    RelayDuration = (double)_relayDurationNumber.Value,
                    LogPath = _logPathText.Text,
                    LocalLogPath = _localLogPathText.Text,
                    TestMode = _testModeCheck.Checked
                };

                var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "set_config", Data = config });

                if (response?.Status == "success")
                {
                    _selectedServer.Config = config;
                    
                    var testModeWarning = config.TestMode ? "\n\n⚠️ TEST MODE IS ENABLED\nRelay will NOT be triggered!" : "";
                    
                    MessageBox.Show(
                        $"✅ Configuration saved successfully!{testModeWarning}\n\n" +
                        "Note: You may need to restart the server for some changes to take effect.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    _statusLabel.Text = $"✅ Saved to {_selectedServer.ServerID}";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    MessageBox.Show($"❌ Failed to save:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "❌ Save failed";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error saving:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "❌ Save failed";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _saveButton.Enabled = true;
            }
        }

        private async Task TestRelayButton_Click()
        {
            if (_selectedServer == null) return;

            try
            {
                _statusLabel.Text = "🔌 Testing relay...";
                _statusLabel.ForeColor = Color.Orange;
                _testRelayButton.Enabled = false;
                Application.DoEvents();

                var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "test_relay" });

                if (response?.Status == "success")
                {
                    MessageBox.Show("✅ Relay test successful!\nDrawer should have opened.", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _statusLabel.Text = "✅ Relay test passed";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    MessageBox.Show($"❌ Relay test failed:\n{response?.Message}", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "❌ Relay test failed";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Test error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "❌ Test failed";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _testRelayButton.Enabled = true;
            }
        }
        
        private async Task TestNotificationButton_Click()
        {
            if (_selectedServer == null) return;

            try
            {
                _statusLabel.Text = "🔔 Sending test notification...";
                _statusLabel.ForeColor = Color.Orange;
                Application.DoEvents();

                var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "test_notification" });

                if (response?.Status == "success")
                {
                    MessageBox.Show(
                        "✅ Test notification sent!\n\n" +
                        "To see it in the Client:\n" +
                        "1. Open Client app\n" +
                        "2. Check '🔔 Enable Error Notifications'\n" +
                        "3. Wait 10 seconds (polls automatically)\n\n" +
                        "The notification will pop up.", 
                        "Test Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _statusLabel.Text = "✅ Test notification sent";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    MessageBox.Show($"❌ Failed:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "❌ Test notification failed";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "❌ Test failed";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private async Task ServerControlButton_Click(string action)
        {
            if (_selectedServer == null) return;

            var actionName = action == "restart" ? "restart" : action == "stop" ? "stop" : "start";
            var actionTitle = action == "restart" ? "Restart" : action == "stop" ? "Stop" : "Start";
            
            var result = MessageBox.Show(
                $"Are you sure you want to {actionName} the server?\n\n" +
                $"Server: {_selectedServer.ServerID}\n" +
                $"Host: {_selectedServer.Host}\n\n" +
                (action != "start" ? $"This will affect all users connected to this server." : "This will start the server service."),
                $"{actionTitle} Server Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                _statusLabel.Text = $"🔄 Sending {actionName} command...";
                _statusLabel.ForeColor = Color.Orange;
                Application.DoEvents();

                // Use control service on port 5002 for start/stop/restart
                var controlResponse = await SendControlCommandAsync(_selectedServer.Host, actionName);

                if (controlResponse?.Status == "success")
                {
                    MessageBox.Show(
                        $"✅ Server {actionName} command sent successfully!\n\n{controlResponse.Message}",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        
                    _statusLabel.Text = $"✅ Server {actionName} command sent";
                    _statusLabel.ForeColor = Color.Green;
                    
                    // If started or restarted, enable buttons after delay
                    if (action == "start" || action == "restart")
                    {
                        await Task.Delay(2000);
                        await RefreshSelectedServerAsync();
                    }
                    // If stopped, disable most buttons but keep start enabled
                    else if (action == "stop")
                    {
                        _saveButton.Enabled = false;
                        _testRelayButton.Enabled = false;
                        _startServerButton.Enabled = true; // Keep start button enabled!
                        _restartServerButton.Enabled = false;
                        _stopServerButton.Enabled = false;
                        _addUserButton.Enabled = false;
                        _refreshButton.Enabled = false;
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"❌ Failed to {actionName} server:\n{controlResponse?.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                        
                    _statusLabel.Text = $"❌ {actionTitle} failed";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error sending {actionName} command:\n{ex.Message}\n\n" +
                    "Make sure the Server Control Service is installed and running on port 5002.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                    
                _statusLabel.Text = $"❌ {actionTitle} failed";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private async Task<ControlResponse?> SendControlCommandAsync(string host, string command)
        {
            const int CONTROL_PORT = 5002;
            
            using var client = new TcpClient();
            await client.ConnectAsync(host, CONTROL_PORT);
            
            using var stream = client.GetStream();
            
            var request = new { Command = command };
            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            
            await stream.WriteAsync(requestBytes);
            
            var buffer = new byte[4096];
            var bytesRead = await stream.ReadAsync(buffer);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            return JsonSerializer.Deserialize<ControlResponse>(responseJson);
        }

        private class ControlResponse
        {
            public string Status { get; set; } = "";
            public string Message { get; set; } = "";
        }
    }
}
