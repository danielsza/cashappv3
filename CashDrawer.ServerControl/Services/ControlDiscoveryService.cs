using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CashDrawer.ServerControl.Models;

namespace CashDrawer.ServerControl.Services
{
    /// <summary>
    /// UDP discovery service for ServerControl - allows NetworkAdmin to find control service
    /// and reports main server status
    /// </summary>
    public class ControlDiscoveryService : BackgroundService
    {
        private readonly ILogger<ControlDiscoveryService> _logger;
        private readonly ControlServiceConfig _config;
        private UdpClient? _udpClient;

        public ControlDiscoveryService(
            ILogger<ControlDiscoveryService> logger,
            IOptions<ControlServiceConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Listen on port 5003 (different from main server's 5001 to avoid conflict)
                _udpClient = new UdpClient(5003);
                _logger.LogInformation("Control Discovery service started on UDP port 5003");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync(stoppingToken);
                        var message = Encoding.UTF8.GetString(result.Buffer);

                        _logger.LogDebug($"Discovery request from {result.RemoteEndPoint}");

                        // Check main server status
                        bool mainServerRunning = IsMainServerRunning();
                        
                        _logger.LogInformation($"Discovery request received - Main server status: {(mainServerRunning ? "Running" : "Stopped")}");
                        
                        // Send response with main server status
                        var response = new
                        {
                            type = "cash_control",
                            ServerID = _config.ServiceName,
                            Port = _config.Port,  // Control service port (5002)
                            MainServerRunning = mainServerRunning,
                            MainServerPort = 5000  // Main server port
                        };

                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                        await _udpClient.SendAsync(responseBytes, result.RemoteEndPoint, stoppingToken);
                        
                        _logger.LogInformation($"Sent discovery response - Main server: {(mainServerRunning ? "Running" : "Stopped")}");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in control discovery service");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Control discovery service failed");
            }
            finally
            {
                _udpClient?.Close();
                _logger.LogInformation("Control discovery service stopped");
            }
        }

        private bool IsMainServerRunning()
        {
            try
            {
                using var service = new ServiceController(_config.ServiceName);
                return service.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            _udpClient?.Dispose();
            base.Dispose();
        }
    }
}
