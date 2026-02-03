using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CashDrawer.Server.Services
{
    public class DailyLogService
    {
        private readonly string _logDirectory;
        private string _currentLogFile = "";
        private DateTime _currentLogDate;
        private readonly object _lock = new();

        public DailyLogService(string logDirectory = "logs")
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
            EnsureCurrentLogFile();
        }

        private void EnsureCurrentLogFile()
        {
            var today = DateTime.Now.Date;
            
            if (_currentLogDate != today || !File.Exists(_currentLogFile))
            {
                _currentLogDate = today;
                _currentLogFile = Path.Combine(_logDirectory, $"cashdrawer_{today:yyyy-MM-dd}.log");
                
                // Create file with header if new
                if (!File.Exists(_currentLogFile))
                {
                    var header = new StringBuilder();
                    header.AppendLine("=".PadRight(80, '='));
                    header.AppendLine($"Cash Drawer Server Log - {today:yyyy-MM-dd}");
                    header.AppendLine($"Server: {Environment.MachineName}");
                    header.AppendLine("=".PadRight(80, '='));
                    header.AppendLine();
                    
                    File.WriteAllText(_currentLogFile, header.ToString());
                }
            }
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStack: {ex.StackTrace}"
                : message;
            
            Log("ERROR", fullMessage);
        }

        public void LogCritical(string message, Exception? ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStack: {ex.StackTrace}"
                : message;
            
            Log("CRITICAL", fullMessage);
        }

        private void Log(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    EnsureCurrentLogFile();
                    
                    var logEntry = new StringBuilder();
                    logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}]");
                    logEntry.AppendLine(message);
                    logEntry.AppendLine();
                    
                    File.AppendAllText(_currentLogFile, logEntry.ToString());
                }
                catch
                {
                    // Silent fail for logging errors
                }
            }
        }

        public string[] GetLogFiles(int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-days);
                var files = Directory.GetFiles(_logDirectory, "cashdrawer_*.log")
                    .Where(f => File.GetCreationTime(f) >= cutoffDate)
                    .OrderByDescending(f => f)
                    .ToArray();
                
                return files;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public string ReadLogFile(string filename)
        {
            try
            {
                var fullPath = Path.Combine(_logDirectory, Path.GetFileName(filename));
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }
            }
            catch
            {
                // Silent fail
            }
            
            return "";
        }

        public ErrorSummary GetErrorSummary(int days = 1)
        {
            var summary = new ErrorSummary();
            var files = GetLogFiles(days);
            
            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var lines = content.Split('\n');
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("[ERROR]"))
                            summary.ErrorCount++;
                        else if (line.Contains("[CRITICAL]"))
                            summary.CriticalCount++;
                        else if (line.Contains("[WARNING]"))
                            summary.WarningCount++;
                    }
                    
                    // Extract recent errors (last 10)
                    var errorLines = lines
                        .Select((line, index) => new { line, index })
                        .Where(x => x.line.Contains("[ERROR]") || x.line.Contains("[CRITICAL]"))
                        .OrderByDescending(x => x.index)
                        .Take(10)
                        .ToList();
                    
                    foreach (var errorLine in errorLines)
                    {
                        // Get context (next 3 lines)
                        var context = new StringBuilder();
                        context.AppendLine(errorLine.line);
                        for (int i = 1; i <= 3 && errorLine.index + i < lines.Length; i++)
                        {
                            context.AppendLine(lines[errorLine.index + i]);
                        }
                        
                        summary.RecentErrors.Add(context.ToString().Trim());
                    }
                }
                catch
                {
                    // Continue with next file
                }
            }
            
            return summary;
        }

        public void CleanOldLogs(int keepDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var files = Directory.GetFiles(_logDirectory, "cashdrawer_*.log");
                
                foreach (var file in files)
                {
                    if (File.GetCreationTime(file) < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Silent fail
            }
        }
    }

    public class ErrorSummary
    {
        public int ErrorCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public List<string> RecentErrors { get; set; } = new();
        public DateTime Generated { get; set; } = DateTime.Now;
    }
}
