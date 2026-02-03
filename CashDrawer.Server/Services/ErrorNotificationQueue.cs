using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// Stores recent errors for admin notifications
    /// </summary>
    public class ErrorNotificationQueue
    {
        private readonly ConcurrentQueue<ErrorNotification> _notifications = new();
        private readonly ILogger<ErrorNotificationQueue> _logger;
        private const int MaxQueueSize = 100;

        public ErrorNotificationQueue(ILogger<ErrorNotificationQueue> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Add an error notification to the queue
        /// </summary>
        public void AddError(string message, string source, Exception? exception = null)
        {
            var notification = new ErrorNotification
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Message = message,
                Source = source,
                Exception = exception?.ToString(),
                Severity = exception != null ? "Critical" : "Warning"
            };

            _notifications.Enqueue(notification);
            
            // Keep queue size manageable
            while (_notifications.Count > MaxQueueSize)
            {
                _notifications.TryDequeue(out _);
            }

            _logger.LogInformation($"Error notification queued: {message}");
        }

        /// <summary>
        /// Get notifications since a specific timestamp
        /// </summary>
        public List<ErrorNotification> GetNotificationsSince(DateTime since)
        {
            return _notifications
                .Where(n => n.Timestamp > since)
                .OrderBy(n => n.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Get all recent notifications (last 5 minutes)
        /// </summary>
        public List<ErrorNotification> GetRecentNotifications()
        {
            var cutoff = DateTime.Now.AddMinutes(-5);
            return GetNotificationsSince(cutoff);
        }
    }

    public class ErrorNotification
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";
        public string? Exception { get; set; }
        public string Severity { get; set; } = "Warning";
    }
}
