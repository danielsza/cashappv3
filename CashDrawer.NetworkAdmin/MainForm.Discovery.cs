using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        // Case-insensitive JSON options for discovery responses
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        private async Task DiscoverServersAsync()
        {
            try
            {
                _statusLabel.Text = "🔍 Discovering servers...";
                _statusLabel.ForeColor = System.Drawing.Color.Orange;
                _discoverButton.Enabled = false;
                Application.DoEvents();

                _discoveredServers.Clear();
                _serversList.Items.Clear();

                using var udpClient = new UdpClient(0);
                udpClient.EnableBroadcast = true;
                udpClient.Client.ReceiveTimeout = 5000;

                var discoveryMsg = new { command = "discover", type = "cash_client" };
                var msgJson = JsonSerializer.Serialize(discoveryMsg);
                var msgBytes = Encoding.UTF8.GetBytes(msgJson);

                // ONLY broadcast to Control Service (port 5003)
                // Control service will report if main server is running or not
                var broadcastAddresses = new List<IPEndPoint>
                {
                    new IPEndPoint(IPAddress.Broadcast, 5003),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003),
                    new IPEndPoint(IPAddress.Parse("10.255.255.255"), 5003),
                    new IPEndPoint(IPAddress.Parse("192.168.255.255"), 5003)
                };

                // Send to all broadcast addresses
                foreach (var endpoint in broadcastAddresses)
                {
                    try
                    {
                        await udpClient.SendAsync(msgBytes, msgBytes.Length, endpoint);
                        await Task.Delay(50);
                    }
                    catch
                    {
                        // Some addresses might fail, continue with others
                    }
                }

                // Listen for responses
                var startTime = DateTime.Now;
                var timeout = 5;

                while ((DateTime.Now - startTime).TotalSeconds < timeout)
                {
                    try
                    {
                        var remainingSeconds = timeout - (DateTime.Now - startTime).TotalSeconds;
                        if (remainingSeconds <= 0)
                            break;

                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(remainingSeconds));
                        var result = await udpClient.ReceiveAsync(cts.Token);
                        
                        var responseJson = Encoding.UTF8.GetString(result.Buffer);
                        var response = JsonSerializer.Deserialize<DiscoveryMessage>(responseJson, _jsonOptions);

                        if (response?.Type == "cash_control")
                        {
                            // Response from control service
                            var serverID = response.ServerID ?? "CashDrawer";
                            serverID = serverID.Replace("Control", "").Replace("_", " ").Trim();
                            if (string.IsNullOrEmpty(serverID)) serverID = "CashDrawer";
                            
                            var mainPort = response.MainServerPort ?? 5000;
                            var controlPort = response.Port;
                            var isOnline = response.MainServerRunning ?? false;
                            
                            var server = new DiscoveredServer
                            {
                                ServerID = serverID,
                                Host = result.RemoteEndPoint.Address.ToString(),
                                Port = mainPort,
                                ControlPort = controlPort,
                                IsConnected = isOnline
                            };

                            // Avoid duplicates
                            if (!_discoveredServers.Any(s => s.Host == server.Host && s.Port == server.Port))
                            {
                                _discoveredServers.Add(server);
                                _serversList.Items.Add(server);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                }

                if (_discoveredServers.Any())
                {
                    var onlineCount = _discoveredServers.Count(s => s.IsConnected);
                    var offlineCount = _discoveredServers.Count(s => !s.IsConnected);
                    _statusLabel.Text = $"✅ Found {_discoveredServers.Count} server(s)";
                    if (offlineCount > 0)
                        _statusLabel.Text += $"\n   ({onlineCount} online, {offlineCount} offline)";
                    _statusLabel.Text += "\nSelect one to manage";
                    _statusLabel.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    _statusLabel.Text = "❌ No servers found\n\n" +
                                        "Check:\n" +
                                        "• Control service running?\n" +
                                        "• Firewall allows UDP 5003?";
                    _statusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Discovery failed:\n{ex.Message}";
                _statusLabel.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                _discoverButton.Enabled = true;
            }
        }

        private async Task<ServerResponse?> SendCommandAsync(DiscoveredServer server, ServerRequest request)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(server.Host, server.Port);
            using var stream = client.GetStream();

            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

            var buffer = new byte[65536];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }
        
        /// <summary>
        /// Send command to control service (for start/stop/restart)
        /// </summary>
        private async Task<ServerResponse?> SendControlCommandAsync(DiscoveredServer server, ServerRequest request)
        {
            if (server.ControlPort == 0)
            {
                return new ServerResponse { Status = "error", Message = "Control service port not known" };
            }
            
            using var client = new TcpClient();
            await client.ConnectAsync(server.Host, server.ControlPort);
            using var stream = client.GetStream();

            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

            var buffer = new byte[65536];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }
    }
}
