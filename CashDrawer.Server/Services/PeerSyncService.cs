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
    /// Auto-discovers peer servers and syncs users, config, and transactions
    /// </summary>
    public class PeerSyncService : BackgroundService
    {
        private readonly ILogger<PeerSyncService> _logger;
        private readonly ServerConfig _config;
        private readonly UserService _userService;
        private readonly TransactionLogger _transactionLogger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _discoveryInterval = TimeSpan.FromMinutes(2);
        private List<PeerServer> _discoveredPeers = new();
        private DateTime _lastDiscovery = DateTime.MinValue;
        
        private static readonly string _pettyCashPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer", "pettycash.json");
        
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PeerSyncService(
            ILogger<PeerSyncService> logger,
            IOptions<ServerConfig> config,
            UserService userService,
            TransactionLogger transactionLogger)
        {
            _logger = logger;
            _config = config.Value;
            _userService = userService;
            _transactionLogger = transactionLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 Peer sync service starting (users, transactions, safe drops, BOD, petty cash config)");
            
            // Initial delay to let server start up
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Discover peers periodically
                    if ((DateTime.Now - _lastDiscovery) > _discoveryInterval)
                    {
                        await DiscoverPeersAsync(stoppingToken);
                        _lastDiscovery = DateTime.Now;
                    }

                    // Sync with all discovered peers
                    if (_discoveredPeers.Any())
                    {
                        foreach (var peer in _discoveredPeers.ToList())
                        {
                            await SyncWithPeerAsync(peer, stoppingToken);
                        }
                    }

                    await Task.Delay(_syncInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in peer sync service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task DiscoverPeersAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogDebug("🔍 Discovering peer servers on port 5003...");

                using var udpClient = new UdpClient(0);
                udpClient.EnableBroadcast = true;
                udpClient.Client.ReceiveTimeout = 3000;

                var discoveryMsg = new { command = "discover", type = "cash_client" };
                var msgBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(discoveryMsg));

                // Broadcast to control service port (5003)
                var broadcastAddresses = new List<IPEndPoint>
                {
                    new IPEndPoint(IPAddress.Broadcast, 5003),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003),
                    new IPEndPoint(IPAddress.Parse("10.255.255.255"), 5003),
                    new IPEndPoint(IPAddress.Parse("192.168.255.255"), 5003)
                };

                foreach (var endpoint in broadcastAddresses)
                {
                    try
                    {
                        await udpClient.SendAsync(msgBytes, endpoint);
                        await Task.Delay(50, stoppingToken);
                    }
                    catch { }
                }
                
                // Listen for responses
                var newPeers = new List<PeerServer>();
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalSeconds < 3)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
                        
                        var result = await udpClient.ReceiveAsync(linked.Token);
                        var responseJson = Encoding.UTF8.GetString(result.Buffer);
                        
                        _logger.LogDebug($"Received discovery response: {responseJson}");
                        
                        var response = JsonSerializer.Deserialize<DiscoveryMessage>(responseJson, _jsonOptions);

                        // Handle both cash_server (direct) and cash_control (control service) responses
                        if (response != null && response.ServerID != _config.ServerID)
                        {
                            int serverPort = response.Port;
                            
                            // If this is a control service response, get the main server port
                            if (response.Type == "cash_control")
                            {
                                serverPort = response.MainServerPort ?? 5000;
                                
                                // Skip if main server is not running
                                if (response.MainServerRunning != true)
                                {
                                    _logger.LogDebug($"Skipping {response.ServerID} - main server not running");
                                    continue;
                                }
                            }
                            else if (response.Type != "cash_server")
                            {
                                continue;
                            }

                            var peer = new PeerServer
                            {
                                ServerID = response.ServerID,
                                Host = result.RemoteEndPoint.Address.ToString(),
                                Port = serverPort
                            };

                            if (!newPeers.Any(p => p.Host == peer.Host && p.Port == peer.Port))
                            {
                                newPeers.Add(peer);
                                _logger.LogDebug($"Found peer: {peer.ServerID} at {peer.Host}:{peer.Port}");
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

                _discoveredPeers = newPeers;
                
                if (_discoveredPeers.Any())
                {
                    _logger.LogInformation($"✅ Discovered {_discoveredPeers.Count} peer server(s): {string.Join(", ", _discoveredPeers.Select(p => $"{p.ServerID}@{p.Host}:{p.Port}"))}");
                }
                else
                {
                    _logger.LogDebug("No peer servers discovered");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering peers");
            }
        }

        private async Task SyncWithPeerAsync(PeerServer peer, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogDebug($"🔄 Syncing with {peer.ServerID} at {peer.Host}:{peer.Port}");
                
                using var client = new TcpClient();
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;
                
                await client.ConnectAsync(peer.Host, peer.Port, stoppingToken);
                using var stream = client.GetStream();

                // Request full sync (users + petty cash)
                var request = new ServerRequest
                {
                    Command = "sync_all"
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await stream.WriteAsync(requestBytes, stoppingToken);

                // Read response
                var buffer = new byte[262144];  // 256KB buffer for transactions
                var bytesRead = await stream.ReadAsync(buffer, stoppingToken);
                
                if (bytesRead == 0)
                {
                    _logger.LogWarning($"No response from {peer.ServerID}");
                    return;
                }
                
                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                _logger.LogDebug($"Sync response length: {responseJson.Length} bytes");
                
                var response = JsonSerializer.Deserialize<ServerResponse>(responseJson, _jsonOptions);

                if (response?.Status == "success" && response.Data != null)
                {
                    var dataJson = JsonSerializer.Serialize(response.Data);
                    using var doc = JsonDocument.Parse(dataJson);
                    var root = doc.RootElement;
                    
                    // Sync users (two-way based on LastModified)
                    if (root.TryGetProperty("Users", out var usersElement))
                    {
                        var usersJson = usersElement.GetRawText();
                        var users = JsonSerializer.Deserialize<List<User>>(usersJson, _jsonOptions);
                        
                        if (users != null && users.Any())
                        {
                            var (added, updated) = _userService.MergeUsers(users);
                            if (added > 0 || updated > 0)
                            {
                                _logger.LogInformation($"🔄 Users from {peer.ServerID}: {added} added, {updated} updated");
                            }
                        }
                    }
                    
                    // Sync transactions
                    if (root.TryGetProperty("Transactions", out var transactionsElement))
                    {
                        var transactionsJson = transactionsElement.GetRawText();
                        var transactions = JsonSerializer.Deserialize<List<Transaction>>(transactionsJson, _jsonOptions);
                        
                        if (transactions != null && transactions.Any())
                        {
                            var synced = _transactionLogger.MergeTransactions(transactions);
                            if (synced > 0)
                            {
                                _logger.LogInformation($"🔄 Transactions from {peer.ServerID}: {synced} synced");
                            }
                        }
                    }
                    
                    // Sync safe drops
                    if (root.TryGetProperty("SafeDrops", out var safeDropsElement))
                    {
                        var safeDropsJson = safeDropsElement.GetRawText();
                        var safeDrops = JsonSerializer.Deserialize<List<SafeDropEntry>>(safeDropsJson, _jsonOptions);
                        
                        if (safeDrops != null && safeDrops.Any())
                        {
                            var synced = await MergeSafeDrops(safeDrops, peer.ServerID);
                            if (synced > 0)
                            {
                                _logger.LogInformation($"🔄 Safe drops from {peer.ServerID}: {synced} synced");
                            }
                        }
                    }
                    
                    // Sync BOD float (take the higher value - assumes both servers start with same float)
                    if (root.TryGetProperty("BodFloat", out var bodFloatElement) && 
                        root.TryGetProperty("BodDate", out var bodDateElement))
                    {
                        var peerBodFloat = bodFloatElement.GetDecimal();
                        var peerBodDate = bodDateElement.GetString();
                        
                        if (peerBodFloat > 0 && peerBodDate == DateTime.Today.ToString("yyyy-MM-dd"))
                        {
                            await MergeBodFloat(peerBodFloat, peer.ServerID);
                        }
                    }
                    
                    // Sync petty cash config (two-way based on LastModified)
                    if (root.TryGetProperty("PettyCash", out var pettyCashElement))
                    {
                        await MergePettyCashConfig(pettyCashElement, peer.ServerID);
                    }
                }
                else
                {
                    _logger.LogWarning($"Sync failed with {peer.ServerID}: {response?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not sync with {peer.ServerID}: {ex.Message}");
                
                // Remove peer if we can't connect
                _discoveredPeers.RemoveAll(p => p.ServerID == peer.ServerID);
            }
        }
        
        private static readonly string _safeDropsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer", "safe_drops.json");
            
        private static readonly string _bodFloatFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer", "bod_float.json");
        
        private async Task<int> MergeSafeDrops(List<SafeDropEntry> peerDrops, string peerId)
        {
            try
            {
                var localDrops = new List<SafeDropEntry>();
                var existingIds = new HashSet<string>();
                
                if (File.Exists(_safeDropsFile))
                {
                    var json = await File.ReadAllTextAsync(_safeDropsFile);
                    localDrops = JsonSerializer.Deserialize<List<SafeDropEntry>>(json, _jsonOptions) ?? new();
                    existingIds = localDrops.Select(d => d.Id).Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
                }
                
                int added = 0;
                foreach (var drop in peerDrops)
                {
                    // Only sync today's drops, skip if we already have it
                    if (drop.Timestamp.Date != DateTime.Today)
                        continue;
                    if (string.IsNullOrEmpty(drop.Id) || existingIds.Contains(drop.Id))
                        continue;
                    
                    localDrops.Add(drop);
                    existingIds.Add(drop.Id);
                    added++;
                }
                
                if (added > 0)
                {
                    var dir = Path.GetDirectoryName(_safeDropsFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                    
                    var json = JsonSerializer.Serialize(localDrops, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(_safeDropsFile, json);
                }
                
                return added;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging safe drops from {peerId}");
                return 0;
            }
        }
        
        private async Task MergeBodFloat(decimal peerBodFloat, string peerId)
        {
            try
            {
                var bodData = new Dictionary<string, decimal>();
                
                if (File.Exists(_bodFloatFile))
                {
                    var json = await File.ReadAllTextAsync(_bodFloatFile);
                    bodData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, _jsonOptions) ?? new();
                }
                
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                
                // Only set BOD if we don't have one for today
                if (!bodData.ContainsKey(today) || bodData[today] == 0)
                {
                    bodData[today] = peerBodFloat;
                    
                    var dir = Path.GetDirectoryName(_bodFloatFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                    
                    var json = JsonSerializer.Serialize(bodData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(_bodFloatFile, json);
                    
                    _logger.LogInformation($"🔄 BOD float synced from {peerId}: ${peerBodFloat:F2}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging BOD float from {peerId}");
            }
        }

        private async Task MergePettyCashConfig(JsonElement peerConfig, string peerId)
        {
            try
            {
                // Get peer's petty cash config
                var peerRecipients = new List<string>();
                var peerReasons = new List<string>();
                DateTime peerLastModified = DateTime.MinValue;
                
                if (peerConfig.TryGetProperty("Recipients", out var r))
                    peerRecipients = r.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (peerConfig.TryGetProperty("Reasons", out var reas))
                    peerReasons = reas.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (peerConfig.TryGetProperty("LastModified", out var lm))
                    DateTime.TryParse(lm.GetString(), out peerLastModified);
                
                // Skip if peer has no data
                if (!peerRecipients.Any() && !peerReasons.Any())
                    return;
                
                // Get local petty cash config
                var localRecipients = new List<string>();
                var localReasons = new List<string>();
                DateTime localLastModified = DateTime.MinValue;
                
                var dataDir = Path.GetDirectoryName(_pettyCashPath);
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir!);
                
                if (File.Exists(_pettyCashPath))
                {
                    var json = await File.ReadAllTextAsync(_pettyCashPath);
                    var config = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    if (config.TryGetProperty("Recipients", out var lr))
                        localRecipients = lr.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    if (config.TryGetProperty("Reasons", out var lreas))
                        localReasons = lreas.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    if (config.TryGetProperty("LastModified", out var llm))
                        DateTime.TryParse(llm.GetString(), out localLastModified);
                    
                    // If no LastModified in file, use file's write time
                    if (localLastModified == DateTime.MinValue)
                        localLastModified = File.GetLastWriteTime(_pettyCashPath);
                }
                
                // Two-way sync: peer's version wins if newer
                if (peerLastModified > localLastModified)
                {
                    var newConfig = new
                    {
                        Recipients = peerRecipients,
                        Reasons = peerReasons,
                        LastModified = peerLastModified.ToString("o")
                    };
                    
                    var newJson = JsonSerializer.Serialize(newConfig, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(_pettyCashPath, newJson);
                    
                    _logger.LogInformation($"🔄 Petty cash config updated from {peerId} ({peerRecipients.Count} recipients, {peerReasons.Count} reasons)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging petty cash config from {peerId}");
            }
        }

        private class PeerServer
        {
            public string ServerID { get; set; } = "";
            public string Host { get; set; } = "";
            public int Port { get; set; }
        }
    }
}
