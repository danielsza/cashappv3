using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CashDrawer.Shared.Models;

namespace CashDrawer.Client
{
    public class NetworkClient : IDisposable
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private string? _serverHost;
        private int _serverPort;

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public void Connect(string host, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(host, port);
                _stream = _tcpClient.GetStream();
                _serverHost = host;
                _serverPort = port;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to {host}:{port} - {ex.Message}");
            }
        }

        public async Task<List<ServerInfo>> DiscoverServersAsync(int timeout = 5)
        {
            var servers = new List<ServerInfo>();

            try
            {
                // Bind to any available port for receiving responses
                using var udpClient = new UdpClient(0);
                udpClient.EnableBroadcast = true;
                udpClient.Client.ReceiveTimeout = timeout * 1000;

                // Send discovery broadcast
                var discoveryMsg = new { command = "discover", type = "cash_client" };
                var msgJson = JsonSerializer.Serialize(discoveryMsg);
                var msgBytes = Encoding.UTF8.GetBytes(msgJson);

                // Try multiple broadcast addresses for better coverage
                var broadcastAddresses = new List<IPEndPoint>
                {
                    new IPEndPoint(IPAddress.Broadcast, 5001),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001),
                    new IPEndPoint(IPAddress.Parse("10.255.255.255"), 5001),
                    new IPEndPoint(IPAddress.Parse("192.168.255.255"), 5001)
                };

                // Send to all broadcast addresses
                foreach (var endpoint in broadcastAddresses)
                {
                    try
                    {
                        await udpClient.SendAsync(msgBytes, endpoint);
                        await Task.Delay(50);
                    }
                    catch
                    {
                        // Some addresses might fail, continue with others
                    }
                }

                // Listen for responses
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalSeconds < timeout)
                {
                    try
                    {
                        // Calculate remaining timeout
                        var remainingSeconds = timeout - (DateTime.Now - startTime).TotalSeconds;
                        if (remainingSeconds <= 0)
                            break;

                        // Use a CancellationToken with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(remainingSeconds));
                        var result = await udpClient.ReceiveAsync(cts.Token);
                        
                        var responseJson = Encoding.UTF8.GetString(result.Buffer);
                        var response = JsonSerializer.Deserialize<DiscoveryMessage>(responseJson);

                        if (response?.Type == "cash_server")
                        {
                            var serverInfo = new ServerInfo
                            {
                                Host = result.RemoteEndPoint.Address.ToString(),
                                Port = response.Port,
                                ServerID = response.ServerID
                            };

                            // Avoid duplicates
                            if (!servers.Exists(s => s.Host == serverInfo.Host && s.Port == serverInfo.Port))
                            {
                                servers.Add(serverInfo);
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Discovery failed: {ex.Message}");
            }

            return servers;
        }

        public async Task<ServerResponse> SendRequestAsync(ServerRequest request)
        {
            if (!IsConnected)
                throw new Exception("Not connected to server");

            try
            {
                // Serialize and send request
                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await _stream!.WriteAsync(requestBytes);

                // Read response
                var buffer = new byte[4096];
                var bytesRead = await _stream.ReadAsync(buffer);
                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
                return response ?? new ServerResponse { Status = "error", Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                throw new Exception($"Communication error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }

    /// <summary>
    /// Information about a discovered server
    /// </summary>
    public class ServerInfo
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ServerID { get; set; } = string.Empty;
    }
}
