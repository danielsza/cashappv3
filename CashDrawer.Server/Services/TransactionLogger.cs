using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CashDrawer.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// Handles transaction logging to files
    /// </summary>
    public class TransactionLogger
    {
        private readonly ILogger<TransactionLogger> _logger;
        private readonly ServerConfig _config;
        private readonly object _lock = new();
        
        // Track synced transaction IDs to avoid duplicates
        private readonly HashSet<string> _syncedTransactionIds = new();

        public TransactionLogger(
            ILogger<TransactionLogger> logger,
            IOptions<ServerConfig> config)
        {
            _logger = logger;
            _config = config.Value;
            EnsureLogDirectories();
            LoadSyncedTransactionIds();
        }

        /// <summary>
        /// Ensure log directories exist
        /// </summary>
        private void EnsureLogDirectories()
        {
            try
            {
                if (!Directory.Exists(_config.LocalLogPath))
                    Directory.CreateDirectory(_config.LocalLogPath);

                _logger.LogInformation($"Local log directory: {_config.LocalLogPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create log directory");
            }
        }
        
        /// <summary>
        /// Load existing transaction IDs from today's log to prevent duplicates
        /// </summary>
        private void LoadSyncedTransactionIds()
        {
            try
            {
                var fileName = $"CashDrawer_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_config.LocalLogPath, fileName);
                
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length > 0)
                        {
                            var txnId = parts[0].Trim();
                            if (!string.IsNullOrEmpty(txnId))
                                _syncedTransactionIds.Add(txnId);
                        }
                    }
                    _logger.LogDebug($"Loaded {_syncedTransactionIds.Count} existing transaction IDs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load existing transaction IDs");
            }
        }

        /// <summary>
        /// Log transaction to file
        /// </summary>
        public void LogTransaction(Transaction transaction)
        {
            lock (_lock)
            {
                try
                {
                    // Generate unique ID if not set
                    transaction.GenerateId();
                    
                    // Skip if already logged (duplicate sync)
                    if (_syncedTransactionIds.Contains(transaction.TransactionId))
                    {
                        _logger.LogDebug($"Skipping duplicate transaction: {transaction.TransactionId}");
                        return;
                    }
                    
                    var logLine = transaction.ToString();
                    
                    // Log to local file
                    LogToFile(_config.LocalLogPath, logLine);
                    _syncedTransactionIds.Add(transaction.TransactionId);
                    
                    // Log to network path if configured
                    if (!string.IsNullOrEmpty(_config.LogPath))
                    {
                        try
                        {
                            LogToFile(_config.LogPath, logLine);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to log to network path, local log successful");
                        }
                    }

                    _logger.LogInformation($"Transaction logged: {transaction.TransactionId} - {transaction.Username} - {transaction.Reason}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log transaction");
                }
            }
        }

        /// <summary>
        /// Get all transactions for today (for sync)
        /// </summary>
        public List<Transaction> GetTodayTransactions()
        {
            var transactions = new List<Transaction>();
            
            try
            {
                var fileName = $"CashDrawer_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_config.LocalLogPath, fileName);
                
                if (File.Exists(filePath))
                {
                    // Read with shared access
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var txn = Transaction.FromLogLine(line);
                            if (txn != null)
                                transactions.Add(txn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read today's transactions");
            }
            
            return transactions;
        }
        
        /// <summary>
        /// Merge transactions from peer server
        /// </summary>
        public int MergeTransactions(List<Transaction> peerTransactions)
        {
            int added = 0;
            
            lock (_lock)
            {
                foreach (var txn in peerTransactions)
                {
                    // Skip if we already have this transaction
                    if (string.IsNullOrEmpty(txn.TransactionId) || _syncedTransactionIds.Contains(txn.TransactionId))
                        continue;
                    
                    // Only sync today's transactions
                    if (txn.Timestamp.Date != DateTime.Today)
                        continue;
                    
                    try
                    {
                        var logLine = txn.ToString();
                        LogToFile(_config.LocalLogPath, logLine);
                        _syncedTransactionIds.Add(txn.TransactionId);
                        added++;
                        
                        _logger.LogDebug($"Synced transaction from peer: {txn.TransactionId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to sync transaction {txn.TransactionId}");
                    }
                }
            }
            
            return added;
        }

        /// <summary>
        /// Write log entry to file
        /// </summary>
        private void LogToFile(string basePath, string logLine)
        {
            try
            {
                // Create directory if needed
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                // Log file name: CashDrawer_YYYY-MM-DD.log
                var fileName = $"CashDrawer_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(basePath, fileName);

                // Append to file
                File.AppendAllText(filePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write to log file in {basePath}");
                throw;
            }
        }
    }
}
