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
    public class ControlService : BackgroundService
    {
        private readonly ILogger<ControlService> _logger;
        private readonly ControlServiceConfig _config;
        private TcpListener? _listener;

        public ControlService(
            ILogger<ControlService> logger,
            IOptions<ControlServiceConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _config.Port);
                _listener.Start();
                _logger.LogInformation($"Server Control Service started on port {_config.Port}");
                _logger.LogInformation($"Monitoring service: {_config.ServiceName}");
                
                if (!string.IsNullOrWhiteSpace(_config.AuthToken) && _config.AuthToken != "CHANGE_THIS_TOKEN_TO_SECURE_VALUE")
                {
                    _logger.LogInformation("Authentication token configured");
                }
                else
                {
                    _logger.LogWarning("WARNING: No authentication token configured! Service is unsecured.");
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Server Control Service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Server Control Service");
            }
            finally
            {
                _listener?.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                _logger.LogInformation($"Client connected from: {clientEndpoint}");

                using var stream = client.GetStream();
                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead > 0)
                {
                    var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var request = JsonSerializer.Deserialize<ControlRequest>(requestJson);

                    // Validate authentication token if configured
                    if (!string.IsNullOrWhiteSpace(_config.AuthToken) && _config.AuthToken != "CHANGE_THIS_TOKEN_TO_SECURE_VALUE")
                    {
                        if (request?.AuthToken != _config.AuthToken)
                        {
                            _logger.LogWarning($"Authentication failed from {clientEndpoint}");
                            var errorResponse = new ControlResponse
                            {
                                Status = "error",
                                Message = "Authentication failed: Invalid token"
                            };
                            var errorJson = JsonSerializer.Serialize(errorResponse);
                            var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                            await stream.WriteAsync(errorBytes, cancellationToken);
                            return;
                        }
                    }

                    // Check IP whitelist if configured
                    if (_config.AllowedIPs.Count > 0)
                    {
                        var clientIP = client.Client.RemoteEndPoint is IPEndPoint ipEndpoint
                            ? ipEndpoint.Address.ToString()
                            : "";

                        if (!_config.AllowedIPs.Contains(clientIP))
                        {
                            _logger.LogWarning($"IP not allowed: {clientIP}");
                            var errorResponse = new ControlResponse
                            {
                                Status = "error",
                                Message = "Access denied: IP not in whitelist"
                            };
                            var errorJson = JsonSerializer.Serialize(errorResponse);
                            var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                            await stream.WriteAsync(errorBytes, cancellationToken);
                            return;
                        }
                    }

                    var response = HandleCommand(request);

                    var responseJson = JsonSerializer.Serialize(response);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await stream.WriteAsync(responseBytes, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client request");
            }
            finally
            {
                client?.Close();
            }
        }

        private ControlResponse HandleCommand(ControlRequest? request)
        {
            if (request == null)
            {
                return new ControlResponse { Status = "error", Message = "Invalid request" };
            }

            _logger.LogInformation($"Received command: {request.Command}");

            try
            {
                return request.Command switch
                {
                    "ping" => new ControlResponse { Status = "success", Message = "Server Control Service online" },
                    "get_status" => GetServiceStatus(),
                    "start" => StartService(),
                    "stop" => StopService(),
                    "restart" => RestartService(),
                    _ => new ControlResponse { Status = "error", Message = $"Unknown command: {request.Command}" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing command: {request.Command}");
                return new ControlResponse { Status = "error", Message = ex.Message };
            }
        }

        private ControlResponse GetServiceStatus()
        {
            try
            {
                using var service = new ServiceController(_config.ServiceName);
                service.Refresh();

                return new ControlResponse
                {
                    Status = "success",
                    Message = $"Service status: {service.Status}",
                    Data = new
                    {
                        ServiceStatus = service.Status.ToString(),
                        CanStop = service.CanStop,
                        CanPauseAndContinue = service.CanPauseAndContinue
                    }
                };
            }
            catch (Exception ex)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = $"Failed to get service status: {ex.Message}"
                };
            }
        }

        private ControlResponse StartService()
        {
            try
            {
                using var service = new ServiceController(_config.ServiceName);
                service.Refresh();

                if (service.Status == ServiceControllerStatus.Running)
                {
                    return new ControlResponse
                    {
                        Status = "success",
                        Message = "Service is already running"
                    };
                }

                _logger.LogInformation($"Starting service: {_config.ServiceName}");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                _logger.LogInformation($"Service started successfully: {_config.ServiceName}");
                return new ControlResponse
                {
                    Status = "success",
                    Message = "Service started successfully"
                };
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = "Service start timed out after 30 seconds"
                };
            }
            catch (Exception ex)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = $"Failed to start service: {ex.Message}"
                };
            }
        }

        private ControlResponse StopService()
        {
            try
            {
                using var service = new ServiceController(_config.ServiceName);
                service.Refresh();

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    return new ControlResponse
                    {
                        Status = "success",
                        Message = "Service is already stopped"
                    };
                }

                if (!service.CanStop)
                {
                    return new ControlResponse
                    {
                        Status = "error",
                        Message = "Service cannot be stopped at this time"
                    };
                }

                _logger.LogInformation($"Stopping service: {_config.ServiceName}");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                _logger.LogInformation($"Service stopped successfully: {_config.ServiceName}");
                return new ControlResponse
                {
                    Status = "success",
                    Message = "Service stopped successfully"
                };
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = "Service stop timed out after 30 seconds"
                };
            }
            catch (Exception ex)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = $"Failed to stop service: {ex.Message}"
                };
            }
        }

        private ControlResponse RestartService()
        {
            try
            {
                using var service = new ServiceController(_config.ServiceName);
                service.Refresh();

                // Stop if running
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    if (!service.CanStop)
                    {
                        return new ControlResponse
                        {
                            Status = "error",
                            Message = "Service cannot be stopped at this time"
                        };
                    }

                    _logger.LogInformation($"Stopping service for restart: {_config.ServiceName}");
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

                // Start
                _logger.LogInformation($"Starting service: {_config.ServiceName}");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                _logger.LogInformation($"Service restarted successfully: {_config.ServiceName}");
                return new ControlResponse
                {
                    Status = "success",
                    Message = "Service restarted successfully"
                };
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = "Service restart timed out"
                };
            }
            catch (Exception ex)
            {
                return new ControlResponse
                {
                    Status = "error",
                    Message = $"Failed to restart service: {ex.Message}"
                };
            }
        }

        public class ControlRequest
        {
            public string Command { get; set; } = "";
            public string? AuthToken { get; set; }
        }

        public class ControlResponse
        {
            public string Status { get; set; } = "";
            public string Message { get; set; } = "";
            public object? Data { get; set; }
        }
    }
}
