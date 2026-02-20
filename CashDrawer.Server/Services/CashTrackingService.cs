using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CashDrawer.Shared.Models;

namespace CashDrawer.Server.Services
{
    public class CashTrackingService
    {
        private readonly ILogger<CashTrackingService> _logger;
        private readonly ServerConfig _config;
        private readonly string _cashDataFile = "cash_data.json";
        private readonly object _lock = new();

        public CashTrackingService(
            ILogger<CashTrackingService> logger,
            IOptions<ServerConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public CashData GetLocalCashData()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_cashDataFile))
                    {
                        var json = File.ReadAllText(_cashDataFile);
                        return JsonSerializer.Deserialize<CashData>(json) ?? new CashData();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load cash data");
                }

                return new CashData { ServerID = _config.ServerID };
            }
        }

        public void UpdateCashData(CashData data)
        {
            lock (_lock)
            {
                try
                {
                    data.ServerID = _config.ServerID;
                    data.LastUpdated = DateTime.Now;
                    
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_cashDataFile, json);
                    
                    _logger.LogInformation($"Cash data updated: ${data.CurrentTotal:F2}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save cash data");
                }
            }
        }

        public void RecordTransaction(decimal amount, string type)
        {
            var data = GetLocalCashData();
            data.CurrentTotal += amount;
            data.LastTransactionAmount = amount;
            data.LastTransactionType = type;
            data.LastTransactionTime = DateTime.Now;
            UpdateCashData(data);
        }

        public void SetStartingFloat(decimal amount, Dictionary<string, int> denominations)
        {
            var data = GetLocalCashData();
            data.StartingFloat = amount;
            data.CurrentTotal = amount;
            data.StartingDenominations = denominations;
            data.DayStartTime = DateTime.Now;
            UpdateCashData(data);
            
            _logger.LogInformation($"Starting float set: ${amount:F2}");
        }

        public void SetEndingCount(decimal amount, Dictionary<string, int> denominations)
        {
            var data = GetLocalCashData();
            data.EndingCount = amount;
            data.EndingDenominations = denominations;
            data.DayEndTime = DateTime.Now;
            UpdateCashData(data);
            
            _logger.LogInformation($"Ending count set: ${amount:F2}");
        }

        public void AddAdjustment(string reason, decimal amount, string enteredBy)
        {
            var data = GetLocalCashData();
            data.Adjustments ??= new List<CashAdjustment>();
            
            data.Adjustments.Add(new CashAdjustment
            {
                Reason = reason,
                Amount = amount,
                Timestamp = DateTime.Now,
                EnteredBy = enteredBy
            });
            
            data.CurrentTotal += amount;
            UpdateCashData(data);
            
            _logger.LogWarning($"Cash adjustment: {reason} = ${amount:F2} by {enteredBy}");
        }

        public async Task<CombinedCashData> GetCombinedCashDataAsync()
        {
            var combined = new CombinedCashData();
            combined.Servers = new List<CashData>();

            // Add local server data
            var localData = GetLocalCashData();
            combined.Servers.Add(localData);
            combined.TotalCash += localData.CurrentTotal;

            // Get data from peer servers
            if (_config.PeerServers != null)
            {
                foreach (var peer in _config.PeerServers)
                {
                    try
                    {
                        var peerData = await GetPeerCashDataAsync(peer);
                        if (peerData != null)
                        {
                            combined.Servers.Add(peerData);
                            combined.TotalCash += peerData.CurrentTotal;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to get cash data from peer {peer}");
                    }
                }
            }

            combined.LastUpdated = DateTime.Now;
            return combined;
        }

        private async Task<CashData?> GetPeerCashDataAsync(string peer)
        {
            try
            {
                var parts = peer.Split(':');
                var host = parts[0];
                var port = parts.Length > 1 ? int.Parse(parts[1]) : _config.Port;

                using var client = new TcpClient();
                await client.ConnectAsync(host, port);

                using var stream = client.GetStream();

                var request = new ServerRequest { Command = "get_cash_data" };
                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await stream.WriteAsync(requestBytes);

                var buffer = new byte[16384];
                var bytesRead = await stream.ReadAsync(buffer);
                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);

                if (response?.Status == "success" && response.Data != null)
                {
                    var dataJson = JsonSerializer.Serialize(response.Data);
                    return JsonSerializer.Deserialize<CashData>(dataJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cash data from {peer}");
            }

            return null;
        }
    }

    public class CashData
    {
        public string ServerID { get; set; } = "";
        public decimal StartingFloat { get; set; }
        public decimal CurrentTotal { get; set; }
        public decimal EndingCount { get; set; }
        public Dictionary<string, int>? StartingDenominations { get; set; }
        public Dictionary<string, int>? EndingDenominations { get; set; }
        public List<CashAdjustment>? Adjustments { get; set; }
        public DateTime? DayStartTime { get; set; }
        public DateTime? DayEndTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public decimal LastTransactionAmount { get; set; }
        public string LastTransactionType { get; set; } = "";
        public DateTime LastTransactionTime { get; set; }
    }

    public class CashAdjustment
    {
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string EnteredBy { get; set; } = "";
    }

    public class CombinedCashData
    {
        public decimal TotalCash { get; set; }
        public List<CashData> Servers { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
