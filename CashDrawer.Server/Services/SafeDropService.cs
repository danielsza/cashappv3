using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CashDrawer.Server.Services
{
    public class SafeDropService
    {
        private readonly ILogger<SafeDropService> _logger;
        private readonly string _safeDropFile = "safe_drops.json";
        private readonly object _lock = new();
        private List<SafeDropRecord> _safeDrops = new();

        public SafeDropService(ILogger<SafeDropService> logger)
        {
            _logger = logger;
            LoadSafeDrops();
        }

        private void LoadSafeDrops()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_safeDropFile))
                    {
                        var json = File.ReadAllText(_safeDropFile);
                        _safeDrops = JsonSerializer.Deserialize<List<SafeDropRecord>>(json) ?? new List<SafeDropRecord>();
                        _logger.LogInformation($"Loaded {_safeDrops.Count} safe drop records");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load safe drops");
                    _safeDrops = new List<SafeDropRecord>();
                }
            }
        }

        private void SaveSafeDrops()
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_safeDrops, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_safeDropFile, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save safe drops");
                }
            }
        }

        public void RecordSafeDrop(SafeDropRecord record)
        {
            lock (_lock)
            {
                _safeDrops.Add(record);
                SaveSafeDrops();
                _logger.LogInformation($"Safe drop recorded: ${record.Amount:F2} by {record.Username}");
            }
        }

        public List<SafeDropRecord> GetSafeDropsForToday()
        {
            return GetSafeDropsForDate(DateTime.Today);
        }

        public List<SafeDropRecord> GetSafeDropsForDate(DateTime date)
        {
            lock (_lock)
            {
                return _safeDrops
                    .Where(d => d.Timestamp.Date == date.Date)
                    .OrderBy(d => d.Timestamp)
                    .ToList();
            }
        }

        public decimal GetTotalSafeDropsForToday()
        {
            return GetSafeDropsForToday()
                .Where(d => d.Confirmed)
                .Sum(d => d.Amount);
        }

        public decimal GetTotalSafeDropsForDate(DateTime date)
        {
            return GetSafeDropsForDate(date)
                .Where(d => d.Confirmed)
                .Sum(d => d.Amount);
        }

        public List<SafeDropRecord> GetUnconfirmedDrops()
        {
            lock (_lock)
            {
                return _safeDrops
                    .Where(d => !d.Confirmed)
                    .OrderByDescending(d => d.Timestamp)
                    .ToList();
            }
        }

        public SafeDropSummary GetSummaryForDate(DateTime date)
        {
            var drops = GetSafeDropsForDate(date);
            var confirmedDrops = drops.Where(d => d.Confirmed).ToList();
            var skippedDrops = drops.Where(d => !d.Confirmed).ToList();

            return new SafeDropSummary
            {
                Date = date,
                TotalAmount = confirmedDrops.Sum(d => d.Amount),
                DropCount = confirmedDrops.Count,
                SkippedCount = skippedDrops.Count,
                ConfirmedDrops = confirmedDrops,
                SkippedDrops = skippedDrops
            };
        }

        public void ClearOldRecords(int daysToKeep = 90)
        {
            lock (_lock)
            {
                var cutoffDate = DateTime.Today.AddDays(-daysToKeep);
                var originalCount = _safeDrops.Count;
                
                _safeDrops = _safeDrops
                    .Where(d => d.Timestamp.Date >= cutoffDate)
                    .ToList();

                var removed = originalCount - _safeDrops.Count;
                if (removed > 0)
                {
                    SaveSafeDrops();
                    _logger.LogInformation($"Cleaned {removed} old safe drop records");
                }
            }
        }
    }

    public class SafeDropRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TransactionId { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string Username { get; set; } = "";
        public string Invoice { get; set; } = "";
        public bool Confirmed { get; set; }
        public string ServerName { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public class SafeDropSummary
    {
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public int DropCount { get; set; }
        public int SkippedCount { get; set; }
        public List<SafeDropRecord> ConfirmedDrops { get; set; } = new();
        public List<SafeDropRecord> SkippedDrops { get; set; } = new();
    }
}
