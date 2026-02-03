using System;
using Serilog.Core;
using Serilog.Events;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// Serilog sink that captures errors and warnings for admin notifications
    /// </summary>
    public class ErrorNotificationSink : ILogEventSink
    {
        private readonly ErrorNotificationQueue _errorQueue;

        public ErrorNotificationSink(ErrorNotificationQueue errorQueue)
        {
            _errorQueue = errorQueue;
        }

        public void Emit(LogEvent logEvent)
        {
            // Only queue warnings and errors
            if (logEvent.Level < LogEventLevel.Warning)
                return;

            try
            {
                var message = logEvent.RenderMessage();
                var source = logEvent.Properties.ContainsKey("SourceContext")
                    ? logEvent.Properties["SourceContext"].ToString().Trim('"')
                    : "Unknown";

                Exception? exception = logEvent.Exception;

                _errorQueue.AddError(message, source, exception);
            }
            catch
            {
                // Don't throw from a logging sink
            }
        }
    }
}
