using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CashDrawer.Shared.Models;

namespace CashDrawer.Server.Services
{
    public class UserSyncService : BackgroundService
    {
        private readonly ILogger<UserSyncService> _logger;
        private readonly UserService _userService;
        private readonly ServerConfig _config;
        private readonly NotificationService _notificationService;
        private DateTime _lastSyncCheck = DateTime.MinValue;

        public UserSyncService(
            ILogger<UserSyncService> logger,
            UserService userService,
            IOptions<ServerConfig> config,
            NotificationService notificationService)
        {
            _logger = logger;
            _userService = userService;
            _config = config.Value;
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check every 30 seconds
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                    // Only sync if users file was modified
                    if (_userService.HasUsersChanged(_lastSyncCheck))
                    {
                        _logger.LogInformation("Users file changed, triggering sync to peers");
                        await SyncUsersToPeersAsync();
                        _lastSyncCheck = DateTime.Now;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in user sync service");
                    await _notificationService.SendErrorNotificationAsync("User sync failed", ex);
                }
            }

            _logger.LogInformation("User Sync Service stopped");
        }

        private async Task SyncUsersToPeersAsync()
        {
            if (_config.PeerServers == null || _config.PeerServers.Count == 0)
            {
                return;
            }

            var users = _userService.GetAllUsers();
            _logger.LogInformation($"Syncing {users.Count} users to {_config.PeerServers.Count} peer(s)");

            foreach (var peer in _config.PeerServers)
            {
                try
                {
                    await SyncToPeerAsync(peer, users);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to sync to peer {peer}");
                }
            }
        }

        private async Task SyncToPeerAsync(string peer, Dictionary<string, User> users)
        {
            try
            {
                var parts = peer.Split(':');
                var host = parts[0];
                var port = parts.Length > 1 ? int.Parse(parts[1]) : _config.Port;

                using var client = new TcpClient();
                await client.ConnectAsync(host, port);

                using var stream = client.GetStream();

                var request = new ServerRequest
                {
                    Command = "sync_users",
                    Data = users
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await stream.WriteAsync(requestBytes);

                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer);
                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);

                if (response?.Status == "success")
                {
                    _logger.LogInformation($"Successfully synced users to {peer}");
                }
                else
                {
                    _logger.LogWarning($"Failed to sync to {peer}: {response?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing to peer {peer}");
                throw;
            }
        }
    }
}
