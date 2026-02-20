using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashDrawer.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// UDP discovery service for auto-discovery by clients
    /// </summary>
    public class DiscoveryService : BackgroundService
    {
        private readonly ILogger<DiscoveryService> _logger;
        private readonly ServerConfig _config;
        private UdpClient? _udpClient;

        public DiscoveryService(
            ILogger<DiscoveryService> logger,
            IOptions<ServerConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _udpClient = new UdpClient(_config.DiscoveryPort);
                _logger.LogInformation($"Discovery service started on UDP port {_config.DiscoveryPort}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync(stoppingToken);
                        var message = Encoding.UTF8.GetString(result.Buffer);

                        _logger.LogDebug($"Discovery request from {result.RemoteEndPoint}");

                        // Send response
                        var response = new DiscoveryMessage
                        {
                            Type = "cash_server",
                            ServerID = _config.ServerID,
                            Port = _config.Port
                        };

                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                        await _udpClient.SendAsync(responseBytes, result.RemoteEndPoint, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in discovery service");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discovery service failed");
            }
            finally
            {
                _udpClient?.Close();
                _logger.LogInformation("Discovery service stopped");
            }
        }

        public override void Dispose()
        {
            _udpClient?.Dispose();
            base.Dispose();
        }
    }
}
