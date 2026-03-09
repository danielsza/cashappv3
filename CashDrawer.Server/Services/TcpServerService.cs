using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// TCP server that handles client connections
    /// </summary>
    public class TcpServerService : BackgroundService
    {
        private readonly ILogger<TcpServerService> _logger;
        private readonly ServerConfig _config;
        private readonly UserService _userService;
        private readonly SerialPortService _serialPortService;
        private readonly TransactionLogger _transactionLogger;
        private readonly ErrorNotificationQueue _errorQueue;
        private TcpListener? _listener;
        
        /// <summary>
        /// Read all lines from a file that may be locked by another process (like Serilog)
        /// </summary>
        private static string[] ReadAllLinesShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new System.Collections.Generic.List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line != null)
                    lines.Add(line);
            }
            return lines.ToArray();
        }

        public TcpServerService(
            ILogger<TcpServerService> logger,
            IOptions<ServerConfig> config,
            UserService userService,
            SerialPortService serialPortService,
            TransactionLogger transactionLogger,
            ErrorNotificationQueue errorQueue)
        {
            _logger = logger;
            _config = config.Value;
            _userService = userService;
            _serialPortService = serialPortService;
            _transactionLogger = transactionLogger;
            _errorQueue = errorQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _config.Port);
                _listener.Start();
                _logger.LogInformation($"TCP server started on port {_config.Port}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                        _ = HandleClientAsync(client, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accepting client");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP server failed");
            }
            finally
            {
                _listener?.Stop();
                _logger.LogInformation("TCP server stopped");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var _ = client;
            
            var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            // Extract just IP address (remove port)
            var clientIP = remoteEndPoint.Contains(':') ? remoteEndPoint.Substring(0, remoteEndPoint.LastIndexOf(':')) : remoteEndPoint;
            
            try
            {
                _logger.LogDebug($"Client connected: {remoteEndPoint}");

                using var stream = client.GetStream();
                var buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0) break; // Client closed connection gracefully

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var request = JsonSerializer.Deserialize<ServerRequest>(json);

                    if (request != null)
                    {
                        // Set client IP from connection (security: don't trust client-provided IP)
                        request.ClientIP = clientIP;
                        
                        var response = ProcessRequest(request);
                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                        await stream.WriteAsync(responseBytes, cancellationToken);
                    }
                }
                
                _logger.LogDebug($"Client disconnected: {remoteEndPoint}");
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx && 
                                          (socketEx.SocketErrorCode == SocketError.ConnectionReset ||
                                           socketEx.SocketErrorCode == SocketError.ConnectionAborted))
            {
                // Client closed connection - this is normal, just log at debug level
                _logger.LogDebug($"Client disconnected (connection closed): {remoteEndPoint}");
            }
            catch (OperationCanceledException)
            {
                // Server shutting down - this is normal
                _logger.LogDebug($"Client connection cancelled (shutdown): {remoteEndPoint}");
            }
            catch (Exception ex)
            {
                // Unexpected error - log as error
                _logger.LogError(ex, $"Unexpected error handling client {remoteEndPoint}");
            }
        }

        private ServerResponse ProcessRequest(ServerRequest request)
        {
            try
            {
                return request.Command switch
                {
                    "ping" => new ServerResponse
                    {
                        Status = "success",
                        ServerID = _config.ServerID
                    },

                    "authenticate" => HandleAuthenticate(request),
                    "admin_authenticate" => HandleAdminAuthenticate(request),  // NEW: Admin-only authentication
                    "open_drawer" => HandleOpenDrawer(request),
                    "open_drawer_only" => HandleOpenDrawerOnly(request),  // Open drawer without logging transaction
                    "get_status" => HandleGetStatus(),
                    "get_users" => HandleGetUsers(),
                    "sync_users" => HandleSyncUsers(request),
                    "sync_all" => HandleSyncAll(request),  // Full sync: users + petty cash config
                    
                    // Admin commands
                    "get_config" => HandleGetConfig(request),
                    "set_config" => HandleSetConfig(request),
                    "reload_config" => HandleReloadConfig(request),
                    "get_all_users" => HandleGetAllUsers(request),
                    "add_user" => HandleAddUser(request),
                    "update_user" => HandleUpdateUser(request),
                    "delete_user" => HandleDeleteUser(request),
                    "reset_password" => HandleResetPassword(request),
                    "test_relay" => HandleTestRelay(request),
                    
                    // Petty cash config commands
                    "get_petty_cash_config" => HandleGetPettyCashConfig(request),
                    "set_petty_cash_config" => HandleSetPettyCashConfig(request),
                    
                    // Log viewer commands
                    "get_transaction_logs" => HandleGetTransactionLogs(request),
                    "get_error_logs" => HandleGetErrorLogs(request),
                    
                    // Admin notification commands
                    "get_notifications" => HandleGetNotifications(request),
                    "test_notification" => HandleTestNotification(request),
                    
                    // Server control commands
                    "restart_server" => HandleRestartServer(request),
                    "stop_server" => HandleStopServer(request),
                    "start_server" => HandleStartServer(request),
                    
                    // EOD/BOD commands
                    "get_day_summary" => HandleGetDaySummary(request),
                    "set_bod_float" => HandleSetBodFloat(request),
                    "get_bod_float" => HandleGetBodFloat(request),
                    "record_safe_drop" => HandleRecordSafeDrop(request),

                    _ => new ServerResponse
                    {
                        Status = "error",
                        Message = $"Unknown command: {request.Command}"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing command: {request.Command}");
                return new ServerResponse
                {
                    Status = "error",
                    Message = ex.Message
                };
            }
        }

        private ServerResponse HandleAuthenticate(ServerRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return new ServerResponse { Status = "error", Message = "Username and password required" };
            }

            var (success, message, user) = _userService.AuthenticateUser(request.Username, request.Password);

            if (success && user != null)
            {
                return new ServerResponse
                {
                    Status = "success",
                    ServerID = _config.ServerID,
                    Data = new AuthResponse
                    {
                        Username = user.Username,
                        Name = user.Name,
                        Level = user.Level,
                        SessionToken = Guid.NewGuid().ToString()
                    }
                };
            }

            _logger.LogWarning($"SECURITY: Failed login attempt for user '{request.Username}' - {message}");
            return new ServerResponse { Status = "error", Message = message ?? "Authentication failed" };
        }

        private ServerResponse HandleOpenDrawer(ServerRequest request)
        {
            // Authenticate by password
            User? user = null;
            
            if (!string.IsNullOrEmpty(request.Password))
            {
                user = _userService.FindUserByPassword(request.Password);
            }

            if (user == null)
            {
                _logger.LogWarning("SECURITY: Failed authentication attempt - invalid password entered");
                return new ServerResponse { Status = "error", Message = "Invalid password" };
            }

            // Open drawer
            if (!_serialPortService.OpenDrawer())
            {
                _logger.LogWarning($"Drawer open failed for user '{user.Username}' - relay/COM port error");
                return new ServerResponse { Status = "error", Message = "Failed to open drawer" };
            }

            // Log transaction
            var transaction = new Transaction
            {
                Timestamp = DateTime.Now,
                ServerID = _config.ServerID,
                Username = user.Username,
                Reason = request.Reason ?? "Transaction",
                DocumentType = request.DocumentType ?? "",
                DocumentNumber = request.DocumentNumber ?? "",
                Total = request.Total,
                AmountIn = request.AmountIn,
                AmountOut = request.AmountOut
            };

            _transactionLogger.LogTransaction(transaction);

            return new ServerResponse
            {
                Status = "success",
                Message = "Drawer opened",
                ServerID = _config.ServerID,
                Username = user.Username,
                Name = user.Name
            };
        }

        /// <summary>
        /// Opens the drawer without logging a transaction.
        /// Used for BOD setup where the drawer needs to open before counting.
        /// </summary>
        private ServerResponse HandleOpenDrawerOnly(ServerRequest request)
        {
            // Authenticate by password
            User? user = null;
            
            if (!string.IsNullOrEmpty(request.Password))
            {
                user = _userService.FindUserByPassword(request.Password);
            }

            if (user == null)
            {
                _logger.LogWarning("SECURITY: Failed authentication attempt - invalid password entered");
                return new ServerResponse { Status = "error", Message = "Invalid password" };
            }

            // Open drawer
            if (!_serialPortService.OpenDrawer())
            {
                _logger.LogWarning($"Drawer open failed for user '{user.Username}' - relay/COM port error");
                return new ServerResponse { Status = "error", Message = "Failed to open drawer" };
            }

            _logger.LogInformation($"Drawer opened (no transaction) by {user.Username}");

            return new ServerResponse
            {
                Status = "success",
                Message = "Drawer opened",
                ServerID = _config.ServerID,
                Username = user.Username,
                Name = user.Name
            };
        }

        private ServerResponse HandleGetStatus()
        {
            return new ServerResponse
            {
                Status = "success",
                ServerID = _config.ServerID,
                Data = new
                {
                    ServerID = _config.ServerID,
                    COMPort = _config.COMPort,
                    RelayType = _config.RelayPin.ToString()
                }
            };
        }

        private ServerResponse HandleGetUsers()
        {
            var users = _userService.GetAllUsers();
            return new ServerResponse
            {
                Status = "success",
                Data = users
            };
        }

        private ServerResponse HandleSyncUsers(ServerRequest request)
        {
            // Return all users for syncing
            var users = _userService.GetUsers();
            return new ServerResponse
            {
                Status = "success",
                Data = users.Values.ToList()
            };
        }

        /// <summary>
        /// Full sync - returns users, petty cash config, transactions, safe drops, and BOD for two-way sync
        /// </summary>
        private ServerResponse HandleSyncAll(ServerRequest request)
        {
            try
            {
                // Get users
                var users = _userService.GetUsers().Values.ToList();
                
                // Get today's transactions
                var transactions = _transactionLogger.GetTodayTransactions();
                
                // Get petty cash config
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "CashDrawer");
                var pettyCashPath = Path.Combine(dataDir, "pettycash.json");
                
                List<string> recipients = new();
                List<string> reasons = new();
                DateTime pettyCashLastModified = DateTime.MinValue;
                
                if (File.Exists(pettyCashPath))
                {
                    var json = File.ReadAllText(pettyCashPath);
                    var config = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    if (config.TryGetProperty("Recipients", out var r))
                        recipients = r.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                    if (config.TryGetProperty("Reasons", out var reas))
                        reasons = reas.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                    if (config.TryGetProperty("LastModified", out var lm))
                        DateTime.TryParse(lm.GetString(), out pettyCashLastModified);
                    
                    // If no LastModified in file, use file's write time
                    if (pettyCashLastModified == DateTime.MinValue)
                        pettyCashLastModified = File.GetLastWriteTime(pettyCashPath);
                }
                
                // Get safe drops for today
                var safeDrops = new List<SafeDropEntry>();
                if (File.Exists(_safeDropsFile))
                {
                    var allDrops = JsonSerializer.Deserialize<List<SafeDropEntry>>(File.ReadAllText(_safeDropsFile)) ?? new();
                    safeDrops = allDrops.Where(d => d.Timestamp.Date == DateTime.Today).ToList();
                }
                
                // Get BOD float for today
                decimal bodFloat = 0;
                if (File.Exists(_bodFloatFile))
                {
                    var bodData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(File.ReadAllText(_bodFloatFile));
                    bodData?.TryGetValue(DateTime.Today.ToString("yyyy-MM-dd"), out bodFloat);
                }
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = new
                    {
                        Users = users,
                        Transactions = transactions,
                        SafeDrops = safeDrops,
                        BodFloat = bodFloat,
                        BodDate = DateTime.Today.ToString("yyyy-MM-dd"),
                        PettyCash = new
                        {
                            Recipients = recipients,
                            Reasons = reasons,
                            LastModified = pettyCashLastModified
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sync_all");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleGetConfig(ServerRequest request)
        {
            return new ServerResponse
            {
                Status = "success",
                Data = _config
            };
        }

        private ServerResponse HandleSetConfig(ServerRequest request)
        {
            try
            {
                if (request.Data == null)
                    return new ServerResponse { Status = "error", Message = "No config data provided" };

                // Deserialize the new config
                var configJson = JsonSerializer.Serialize(request.Data);
                var newConfig = JsonSerializer.Deserialize<ServerConfig>(configJson);

                if (newConfig == null)
                    return new ServerResponse { Status = "error", Message = "Invalid config format" };

                // Save to ProgramData (writable location), not Program Files (read-only)
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "CashDrawer", "appsettings.json");
                
                var json = $$"""
                {
                  "Server": {
                    "ServerID": "{{newConfig.ServerID}}",
                    "Port": {{newConfig.Port}},
                    "DiscoveryPort": {{newConfig.DiscoveryPort}},
                    "COMPort": "{{newConfig.COMPort}}",
                    "RelayPin": "{{newConfig.RelayPin}}",
                    "RelayDuration": {{newConfig.RelayDuration}},
                    "LogPath": "{{newConfig.LogPath.Replace("\\", "\\\\")}}",
                    "LocalLogPath": "{{newConfig.LocalLogPath.Replace("\\", "\\\\")}}",
                    "PeerServerHost": {{(newConfig.PeerServerHost != null ? $"\"{newConfig.PeerServerHost}\"" : "null")}},
                    "PeerServerPort": {{newConfig.PeerServerPort}},
                    "TestMode": {{newConfig.TestMode.ToString().ToLower()}}
                  },
                  "Security": {
                    "MaxFailedAttempts": 3,
                    "LockoutDurationSeconds": 300,
                    "SessionTimeoutSeconds": 3600
                  },
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information",
                      "Microsoft": "Warning",
                      "Microsoft.Hosting.Lifetime": "Information"
                    }
                  }
                }
                """;

                File.WriteAllText(configPath, json);
                _logger.LogInformation($"Config saved to {configPath}");

                return new ServerResponse
                {
                    Status = "success",
                    Message = "Config saved to C:\\ProgramData\\CashDrawer\\appsettings.json. Restart server to apply changes."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting config");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleGetAllUsers(ServerRequest request)
        {
            var users = _userService.GetUsers();
            return new ServerResponse
            {
                Status = "success",
                Data = users
            };
        }

        private ServerResponse HandleAddUser(ServerRequest request)
        {
            try
            {
                if (request.Data == null)
                    return new ServerResponse { Status = "error", Message = "No user data provided" };

                var userJson = JsonSerializer.Serialize(request.Data);
                var user = JsonSerializer.Deserialize<User>(userJson);

                if (user == null || string.IsNullOrEmpty(user.Username))
                    return new ServerResponse { Status = "error", Message = "Invalid user data" };

                // AddUser signature: (username, password, name, level)
                var result = _userService.AddUser(
                    user.Username, 
                    request.Password ?? "", 
                    user.Name ?? user.Username,
                    user.Level);
                    
                if (result)
                {
                    _logger.LogInformation($"User '{user.Username}' added remotely");
                    return new ServerResponse { Status = "success", Message = "User added" };
                }

                return new ServerResponse { Status = "error", Message = "User already exists" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleUpdateUser(ServerRequest request)
        {
            try
            {
                if (request.Data == null)
                    return new ServerResponse { Status = "error", Message = "No user data provided" };

                var userJson = JsonSerializer.Serialize(request.Data);
                var user = JsonSerializer.Deserialize<User>(userJson);

                if (user == null || string.IsNullOrEmpty(user.Username))
                    return new ServerResponse { Status = "error", Message = "Invalid user data" };

                var existing = _userService.GetUser(user.Username);
                if (existing == null)
                    return new ServerResponse { Status = "error", Message = "User not found" };

                // Update properties
                existing.Name = user.Name;
                existing.Level = user.Level;
                existing.FailedAttempts = user.FailedAttempts;
                existing.LockedUntil = user.LockedUntil;
                existing.LastModified = DateTime.Now;  // Mark as modified for sync
                
                _userService.SaveUsers();
                _logger.LogInformation($"User '{user.Username}' updated remotely");

                return new ServerResponse { Status = "success", Message = "User updated" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleDeleteUser(ServerRequest request)
        {
            try
            {
                var username = request.Username;
                if (string.IsNullOrEmpty(username))
                    return new ServerResponse { Status = "error", Message = "Username required" };

                var result = _userService.DeleteUser(username);
                if (result)
                {
                    _logger.LogInformation($"User '{username}' deleted remotely");
                    return new ServerResponse { Status = "success", Message = "User deleted" };
                }

                return new ServerResponse { Status = "error", Message = "User not found" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleResetPassword(ServerRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return new ServerResponse { Status = "error", Message = "Username and password required" };
                }

                var user = _userService.GetUser(request.Username);
                if (user != null)
                {
                    // Update password - HASH it first!
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    user.FailedAttempts = 0;
                    user.LockedUntil = null;
                    user.LastModified = DateTime.Now;
                    
                    _userService.SaveUsers();
                    _logger.LogInformation($"Password reset for user: {request.Username}");

                    return new ServerResponse
                    {
                        Status = "success",
                        Message = $"Password reset successfully for user '{request.Username}'"
                    };
                }

                return new ServerResponse { Status = "error", Message = "User not found" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleTestRelay(ServerRequest request)
        {
            try
            {
                _logger.LogInformation("Remote relay test requested");
                var result = _serialPortService.OpenDrawer();
                
                return new ServerResponse
                {
                    Status = result ? "success" : "error",
                    Message = result ? "Relay test successful" : "Relay test failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing relay");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleReloadConfig(ServerRequest request)
        {
            try
            {
                _logger.LogInformation("Configuration reload requested remotely - restarting service");
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    _logger.LogInformation("Exiting for service restart...");
                    Environment.Exit(0); // Service recovery will restart
                });

                return new ServerResponse
                {
                    Status = "success",
                    Message = "Server will restart shortly"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading config");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleRestartServer(ServerRequest request)
        {
            try
            {
                _logger.LogWarning("Server restart requested remotely");
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    _logger.LogInformation("Exiting for service restart...");
                    Environment.Exit(0); // Service recovery will restart
                });

                return new ServerResponse
                {
                    Status = "success",
                    Message = "Server will restart shortly (via service recovery)"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting server");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleStopServer(ServerRequest request)
        {
            try
            {
                _logger.LogWarning("Server stop requested remotely");
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    _logger.LogInformation("Stopping server...");
                    Environment.Exit(1); // Exit code 1 = stop (not restart)
                });

                return new ServerResponse
                {
                    Status = "success",
                    Message = "Server will stop shortly"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping server");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }

        private ServerResponse HandleStartServer(ServerRequest request)
        {
            // This cannot be called via TCP since server is stopped
            // This exists for completeness but won't be reachable when server is down
            return new ServerResponse
            {
                Status = "error",
                Message = "Cannot start server via TCP when server is stopped. Use Windows Services or sc.exe."
            };
        }
        
        private ServerResponse HandleGetPettyCashConfig(ServerRequest request)
        {
            try
            {
                // Read petty cash config from ProgramData
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "CashDrawer");
                
                var configPath = Path.Combine(dataDir, "pettycash.json");
                _logger.LogInformation($"Loading petty cash config from: {configPath}");
                
                string recipients;
                string reasons;
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    recipients = config.TryGetProperty("Recipients", out var r)
                        ? string.Join("||", r.EnumerateArray().Select(x => x.GetString() ?? ""))
                        : "Store Supplies||Office Supplies||Employee Reimbursement||Postage||Cleaning Supplies||Misc Expense";
                    
                    reasons = config.TryGetProperty("Reasons", out var reas)
                        ? string.Join("||", reas.EnumerateArray().Select(x => x.GetString() ?? ""))
                        : "Office Supplies||Postage||Employee Lunch||Cleaning Supplies||Emergency Purchase||Store Maintenance||Other";
                    
                    _logger.LogInformation($"Loaded {r.GetArrayLength()} recipients and {reas.GetArrayLength()} reasons");
                }
                else
                {
                    _logger.LogInformation("No petty cash config found, using defaults");
                    recipients = "Store Supplies||Office Supplies||Employee Reimbursement||Postage||Cleaning Supplies||Misc Expense";
                    reasons = "Office Supplies||Postage||Employee Lunch||Cleaning Supplies||Emergency Purchase||Store Maintenance||Other";
                }
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = $"{recipients}|||{reasons}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting petty cash config");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleSetPettyCashConfig(ServerRequest request)
        {
            try
            {
                // Store petty cash config in ProgramData (writable location)
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "CashDrawer");
                
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }
                
                var configPath = Path.Combine(dataDir, "pettycash.json");
                _logger.LogInformation($"Saving petty cash config to: {configPath}");
                
                // Cast Data to string before splitting
                var dataStr = request.Data?.ToString();
                if (string.IsNullOrEmpty(dataStr))
                {
                    return new ServerResponse { Status = "error", Message = "Invalid data format" };
                }
                
                var parts = dataStr.Split(new[] { "|||" }, StringSplitOptions.None);
                if (parts.Length < 2)
                {
                    return new ServerResponse { Status = "error", Message = "Invalid data format" };
                }
                
                var recipients = parts[0].Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                var reasons = parts[1].Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                
                _logger.LogInformation($"Saving {recipients.Length} recipients and {reasons.Length} reasons");
                
                // Create simple JSON config with LastModified for sync
                var config = new
                {
                    Recipients = recipients.ToList(),
                    Reasons = reasons.ToList(),
                    LastModified = DateTime.Now.ToString("o")  // ISO 8601 format
                };
                
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                
                _logger.LogInformation("Petty cash configuration saved successfully");
                
                return new ServerResponse
                {
                    Status = "success",
                    Message = "Petty cash configuration saved"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting petty cash config");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleGetTransactionLogs(ServerRequest request)
        {
            try
            {
                // Handle JsonElement Data type
                string dataStr;
                if (request.Data is System.Text.Json.JsonElement jsonElement)
                {
                    dataStr = jsonElement.GetString() ?? "";
                }
                else
                {
                    dataStr = request.Data?.ToString() ?? "";
                }
                
                var parts = string.IsNullOrEmpty(dataStr) ? Array.Empty<string>() : dataStr.Split(new[] { "||" }, StringSplitOptions.None);
                var startDate = parts.Length > 0 && DateTime.TryParse(parts[0], out var sd) ? sd : DateTime.Now.AddDays(-7);
                var endDate = parts.Length > 1 && DateTime.TryParse(parts[1], out var ed) ? ed : DateTime.Now.AddDays(1);
                var searchTerm = parts.Length > 2 ? parts[2] : "";
                
                _logger.LogInformation($"Getting transaction logs from {_config.LocalLogPath}, date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}, search: '{searchTerm}'");
                
                var logDir = _config.LocalLogPath;
                if (!Directory.Exists(logDir))
                {
                    _logger.LogWarning($"Log directory does not exist: {logDir}");
                    return new ServerResponse { Status = "success", Data = "No log directory found" };
                }
                
                var logs = new System.Collections.Generic.List<string>();
                
                // Only read CashDrawer_ transaction files (not Errors_ files)
                var logFiles = Directory.GetFiles(logDir, "CashDrawer_*.log");
                
                _logger.LogInformation($"Found {logFiles.Length} transaction log files");
                
                foreach (var file in logFiles.OrderByDescending(f => File.GetLastWriteTime(f)))
                {
                    var lines = ReadAllLinesShared(file);
                    _logger.LogInformation($"Reading {lines.Length} lines from {Path.GetFileName(file)}");
                    
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        // Parse log entry date (format: 2026-01-28 08:49:27 | ...)
                        if (line.Length >= 19 && DateTime.TryParse(line.Substring(0, 19), out var logDate))
                        {
                            // Filter by date range
                            if (logDate < startDate.Date || logDate > endDate.Date.AddDays(1))
                                continue;
                        }
                        
                        // Filter by search term
                        if (!string.IsNullOrWhiteSpace(searchTerm) && 
                            !line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        logs.Add(line);
                    }
                }
                
                _logger.LogInformation($"Returning {logs.Count} log entries (limited to 1000)");
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = logs.Any() ? string.Join("||", logs.Take(1000)) : "No logs found for the specified criteria"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction logs");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleGetErrorLogs(ServerRequest request)
        {
            try
            {
                // Handle JsonElement Data type
                string dataStr;
                if (request.Data is System.Text.Json.JsonElement jsonElement)
                {
                    dataStr = jsonElement.GetString() ?? "";
                }
                else
                {
                    dataStr = request.Data?.ToString() ?? "";
                }
                
                var parts = string.IsNullOrEmpty(dataStr) ? Array.Empty<string>() : dataStr.Split(new[] { "||" }, StringSplitOptions.None);
                var startDate = parts.Length > 0 && DateTime.TryParse(parts[0], out var sd) ? sd : DateTime.Now.AddDays(-7);
                var endDate = parts.Length > 1 && DateTime.TryParse(parts[1], out var ed) ? ed : DateTime.Now.AddDays(1);
                var searchTerm = parts.Length > 2 ? parts[2] : "";
                
                _logger.LogInformation($"Getting error logs from {_config.LocalLogPath}, date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                var logDir = _config.LocalLogPath;
                if (!Directory.Exists(logDir))
                {
                    _logger.LogWarning($"Log directory does not exist: {logDir}");
                    return new ServerResponse { Status = "success", Data = "No log directory found" };
                }
                
                var logs = new System.Collections.Generic.List<string>();
                
                // Only search Errors_ files (not CashDrawer_ transaction files)
                var logFiles = Directory.GetFiles(logDir, "Errors_*.log");
                
                _logger.LogInformation($"Searching for errors in {logFiles.Length} error log files");
                
                foreach (var file in logFiles.OrderByDescending(f => File.GetLastWriteTime(f)))
                {
                    var lines = ReadAllLinesShared(file);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        // Parse log entry date (format: 2026-01-28 08:49:27 | ...)
                        if (line.Length >= 19 && DateTime.TryParse(line.Substring(0, 19), out var logDate))
                        {
                            // Filter by date range
                            if (logDate < startDate.Date || logDate > endDate.Date.AddDays(1))
                                continue;
                        }
                        
                        // All entries in Errors_ files are already errors/warnings
                        // Just apply search term filter
                        if (!string.IsNullOrWhiteSpace(searchTerm) &&
                            !line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        logs.Add(line);
                    }
                }
                
                _logger.LogInformation($"Found {logs.Count} error log entries (limited to 1000)");
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = logs.Any() ? string.Join("||", logs.Take(1000)) : "No errors found for the specified criteria"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error logs");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleGetNotifications(ServerRequest request)
        {
            try
            {
                // Get timestamp from request (format: "yyyy-MM-dd HH:mm:ss")
                DateTime since = DateTime.Now.AddMinutes(-5); // Default: last 5 minutes
                
                if (request.Data != null)
                {
                    string dataStr;
                    if (request.Data is System.Text.Json.JsonElement jsonElement)
                    {
                        dataStr = jsonElement.GetString() ?? "";
                    }
                    else
                    {
                        dataStr = request.Data.ToString() ?? "";
                    }
                    
                    if (!string.IsNullOrEmpty(dataStr) && DateTime.TryParse(dataStr, out var parsedDate))
                    {
                        since = parsedDate;
                    }
                }
                
                var notifications = _errorQueue.GetNotificationsSince(since);
                
                _logger.LogInformation($"Returning {notifications.Count} notifications since {since:yyyy-MM-dd HH:mm:ss}");
                
                // Serialize notifications to JSON
                var json = JsonSerializer.Serialize(notifications);
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = json
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleTestNotification(ServerRequest request)
        {
            try
            {
                _logger.LogWarning("TEST NOTIFICATION: This is a test notification triggered by admin.");
                
                return new ServerResponse
                {
                    Status = "success",
                    Message = "Test notification created. Enable notifications in client to see it."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test notification");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        /// <summary>
        /// Authenticate admin user - Only allows UserLevel.Admin
        /// </summary>
        private ServerResponse HandleAdminAuthenticate(ServerRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return new ServerResponse { Status = "error", Message = "Username and password required" };
            }

            var (success, message, user) = _userService.AuthenticateUser(request.Username, request.Password);

            if (success && user != null)
            {
                // Check if user is Admin level
                if (user.Level != UserLevel.Admin)
                {
                    _logger.LogWarning($"User '{user.Username}' (Level: {user.Level}) attempted to access NetworkAdmin - DENIED");
                    return new ServerResponse 
                    { 
                        Status = "error", 
                        Message = "⛔ Access Denied\n\nOnly Administrator accounts can access NetworkAdmin.\n\nYour account level is: " + user.Level 
                    };
                }
                
                _logger.LogInformation($"Admin user '{user.Username}' authenticated for NetworkAdmin");
                
                return new ServerResponse
                {
                    Status = "success",
                    ServerID = _config.ServerID,
                    Data = new AuthResponse
                    {
                        Username = user.Username,
                        Name = user.Name,
                        Level = user.Level,
                        SessionToken = Guid.NewGuid().ToString()
                    }
                };
            }

            _logger.LogWarning($"SECURITY: Failed admin login attempt for user '{request.Username}' - {message}");
            return new ServerResponse { Status = "error", Message = message ?? "Authentication failed" };
        }

        #region EOD/BOD/SafeDrop Handlers
        
        private static readonly string _bodFloatFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer", "bod_float.json");
            
        private static readonly string _safeDropsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer", "safe_drops.json");

        private ServerResponse HandleGetDaySummary(ServerRequest request)
        {
            try
            {
                var today = DateTime.Today;
                
                // Get BOD float for today
                decimal bodFloat = 0;
                if (File.Exists(_bodFloatFile))
                {
                    var bodData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(File.ReadAllText(_bodFloatFile));
                    bodData?.TryGetValue(today.ToString("yyyy-MM-dd"), out bodFloat);
                }
                
                // Get safe drops for today
                var safeDrops = new List<object>();
                decimal totalSafeDrops = 0;
                if (File.Exists(_safeDropsFile))
                {
                    var allDrops = JsonSerializer.Deserialize<List<SafeDropEntry>>(File.ReadAllText(_safeDropsFile)) ?? new();
                    var todayDrops = allDrops.Where(d => d.Timestamp.Date == today).ToList();
                    totalSafeDrops = todayDrops.Where(d => d.Confirmed).Sum(d => d.Amount);
                    safeDrops = todayDrops.Cast<object>().ToList();
                }
                
                // Get transaction totals for today from logs
                decimal totalIn = 0;
                decimal totalOut = 0;
                decimal totalTransactions = 0;
                int transactionCount = 0;
                
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "CashDrawer", "Logs");
                var logFile = Path.Combine(logDir, $"CashDrawer_{today:yyyy-MM-dd}.log");
                
                if (File.Exists(logFile))
                {
                    var lines = ReadAllLinesShared(logFile);
                    foreach (var line in lines)
                    {
                        // Parse log line format (new with TransactionId): 
                        // TransactionId | 2026-01-30 11:21:43 | SERVER1 | 709 | Transaction | BOD |  | Total: 50.00 | IN: 50.00 | OUT: 0.00
                        var parts = line.Split('|');
                        if (parts.Length >= 10)
                        {
                            // New format with TransactionId
                            var docType = parts[5].Trim();
                            
                            // Skip BOD/EOD from the cash flow calculation (they're not transactions)
                            if (docType == "BOD" || docType == "EOD")
                            {
                                transactionCount++;
                                continue;
                            }
                            
                            // Parse "Total: 50.00", "IN: 50.00" and "OUT: -10.00" format
                            var totalPart = parts[7].Trim(); // "Total: 50.00"
                            var inPart = parts[8].Trim();    // "IN: 50.00"
                            var outPart = parts[9].Trim();   // "OUT: -10.00"
                            
                            if (totalPart.StartsWith("Total:"))
                            {
                                var totalValue = totalPart.Substring(6).Trim();
                                if (decimal.TryParse(totalValue, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out var txTotal))
                                    totalTransactions += txTotal;
                            }
                            
                            if (inPart.StartsWith("IN:"))
                            {
                                var inValue = inPart.Substring(3).Trim();
                                if (decimal.TryParse(inValue, out var amtIn))
                                    totalIn += amtIn;
                            }
                            
                            if (outPart.StartsWith("OUT:"))
                            {
                                var outValue = outPart.Substring(4).Trim();
                                if (decimal.TryParse(outValue, out var amtOut))
                                    totalOut += amtOut; // Keep sign (negative for refunds/petty cash)
                            }
                            
                            transactionCount++;
                        }
                        else if (parts.Length >= 9)
                        {
                            // Old format without TransactionId (for backwards compatibility)
                            var docType = parts[4].Trim();
                            
                            if (docType == "BOD" || docType == "EOD")
                            {
                                transactionCount++;
                                continue;
                            }
                            
                            var totalPartOld = parts[6].Trim(); // "Total: 50.00"
                            var inPart = parts[7].Trim();
                            var outPart = parts[8].Trim();
                            
                            if (totalPartOld.StartsWith("Total:"))
                            {
                                var totalValue = totalPartOld.Substring(6).Trim();
                                if (decimal.TryParse(totalValue, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out var txTotal))
                                    totalTransactions += txTotal;
                            }
                            
                            if (inPart.StartsWith("IN:"))
                            {
                                var inValue = inPart.Substring(3).Trim();
                                if (decimal.TryParse(inValue, out var amtIn))
                                    totalIn += amtIn;
                            }
                            
                            if (outPart.StartsWith("OUT:"))
                            {
                                var outValue = outPart.Substring(4).Trim();
                                if (decimal.TryParse(outValue, out var amtOut))
                                    totalOut += amtOut;
                            }
                            
                            transactionCount++;
                        }
                    }
                }
                
                // Expected = BOD + sum of transaction Totals (not IN+OUT, which double-counts change)
                // Note: Don't subtract SafeDrops here - client's EODCountForm does that
                var expectedTotal = bodFloat + totalTransactions;
                
                // Get breakdown by server (for multi-server environments)
                var serverBreakdown = new Dictionary<string, object>();
                if (File.Exists(logFile))
                {
                    var lines = ReadAllLinesShared(logFile);
                    var serverTotals = new Dictionary<string, (decimal In, decimal Out, int Count)>();
                    
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        string serverID = "";
                        string docType = "";
                        decimal amtIn = 0, amtOut = 0;
                        
                        if (parts.Length >= 10)
                        {
                            serverID = parts[2].Trim();
                            docType = parts[5].Trim();
                            
                            if (docType != "BOD" && docType != "EOD")
                            {
                                var inPart = parts[8].Trim();
                                var outPart = parts[9].Trim();
                                if (inPart.StartsWith("IN:"))
                                    decimal.TryParse(inPart.Substring(3).Trim(), out amtIn);
                                if (outPart.StartsWith("OUT:"))
                                    decimal.TryParse(outPart.Substring(4).Trim(), out amtOut);
                            }
                        }
                        else if (parts.Length >= 9)
                        {
                            serverID = parts[1].Trim();
                            docType = parts[4].Trim();
                            
                            if (docType != "BOD" && docType != "EOD")
                            {
                                var inPart = parts[7].Trim();
                                var outPart = parts[8].Trim();
                                if (inPart.StartsWith("IN:"))
                                    decimal.TryParse(inPart.Substring(3).Trim(), out amtIn);
                                if (outPart.StartsWith("OUT:"))
                                    decimal.TryParse(outPart.Substring(4).Trim(), out amtOut);
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(serverID) && docType != "BOD" && docType != "EOD")
                        {
                            if (!serverTotals.ContainsKey(serverID))
                                serverTotals[serverID] = (0, 0, 0);
                            var current = serverTotals[serverID];
                            serverTotals[serverID] = (current.In + amtIn, current.Out + amtOut, current.Count + 1);
                        }
                    }
                    
                    foreach (var kvp in serverTotals)
                    {
                        serverBreakdown[kvp.Key] = new { In = kvp.Value.In, Out = kvp.Value.Out, Count = kvp.Value.Count };
                    }
                }
                
                return new ServerResponse
                {
                    Status = "success",
                    Data = new
                    {
                        Date = today.ToString("yyyy-MM-dd"),
                        BodFloat = bodFloat,
                        TotalIn = totalIn,
                        TotalOut = totalOut,
                        TotalSafeDrops = totalSafeDrops,
                        ExpectedTotal = expectedTotal,
                        TransactionCount = transactionCount,
                        SafeDrops = safeDrops,
                        ServerBreakdown = serverBreakdown
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting day summary");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleSetBodFloat(ServerRequest request)
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var bodData = new Dictionary<string, decimal>();
                
                if (File.Exists(_bodFloatFile))
                {
                    bodData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(File.ReadAllText(_bodFloatFile)) ?? new();
                }
                
                bodData[today] = request.Total;
                
                var dir = Path.GetDirectoryName(_bodFloatFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                
                File.WriteAllText(_bodFloatFile, JsonSerializer.Serialize(bodData, new JsonSerializerOptions { WriteIndented = true }));
                
                _logger.LogInformation($"BOD float set to {request.Total:C} for {today}");
                
                return new ServerResponse { Status = "success", Message = $"BOD float set to {request.Total:C}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting BOD float");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleGetBodFloat(ServerRequest request)
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                decimal bodFloat = 0;
                
                if (File.Exists(_bodFloatFile))
                {
                    var bodData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(File.ReadAllText(_bodFloatFile));
                    bodData?.TryGetValue(today, out bodFloat);
                }
                
                return new ServerResponse { Status = "success", Data = bodFloat };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting BOD float");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        private ServerResponse HandleRecordSafeDrop(ServerRequest request)
        {
            try
            {
                var drops = new List<SafeDropEntry>();
                
                if (File.Exists(_safeDropsFile))
                {
                    drops = JsonSerializer.Deserialize<List<SafeDropEntry>>(File.ReadAllText(_safeDropsFile)) ?? new();
                }
                
                var entry = new SafeDropEntry
                {
                    Timestamp = DateTime.Now,
                    Amount = request.Total,
                    Username = request.Username ?? "Unknown",
                    Invoice = request.DocumentNumber ?? "",
                    Confirmed = request.Data?.ToString() == "confirmed"
                };
                entry.GenerateId(_config.ServerID);
                
                drops.Add(entry);
                
                var dir = Path.GetDirectoryName(_safeDropsFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                
                File.WriteAllText(_safeDropsFile, JsonSerializer.Serialize(drops, new JsonSerializerOptions { WriteIndented = true }));
                
                _logger.LogInformation($"Safe drop recorded: {request.Total:C} by {request.Username} - {(entry.Confirmed ? "CONFIRMED" : "SKIPPED")}");
                
                return new ServerResponse { Status = "success", Message = "Safe drop recorded" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording safe drop");
                return new ServerResponse { Status = "error", Message = ex.Message };
            }
        }
        
        #endregion
    }
}
