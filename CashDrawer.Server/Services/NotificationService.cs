using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashDrawer.Server.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationConfig _config;

        public NotificationService(
            ILogger<NotificationService> logger,
            IOptions<NotificationConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task SendErrorNotificationAsync(string errorMessage, Exception? ex = null)
        {
            try
            {
                var subject = $"Cash Drawer Error - {DateTime.Now:yyyy-MM-dd HH:mm}";
                var body = $@"
Cash Drawer Server Error Report
================================
Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Server: {Environment.MachineName}

Error Message:
{errorMessage}

{(ex != null ? $@"
Exception Details:
Type: {ex.GetType().Name}
Message: {ex.Message}
Stack Trace:
{ex.StackTrace}
" : "")}

Please investigate this issue immediately.
";

                if (_config.EmailEnabled)
                {
                    await SendEmailAsync(subject, body);
                }

                if (_config.PopupEnabled)
                {
                    // Log for popup display (will be picked up by admin tool)
                    _logger.LogCritical($"ADMIN_POPUP: {errorMessage}");
                }
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Failed to send error notification");
            }
        }

        private async Task SendEmailAsync(string subject, string body)
        {
            if (!_config.EmailEnabled || string.IsNullOrWhiteSpace(_config.SmtpServer))
            {
                _logger.LogWarning("Email notifications not configured");
                return;
            }

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_config.FromEmail, "Cash Drawer System");
                
                foreach (var email in _config.AdminEmails)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        message.To.Add(email);
                    }
                }

                if (message.To.Count == 0)
                {
                    _logger.LogWarning("No admin emails configured");
                    return;
                }

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                using var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort);
                client.EnableSsl = _config.UseSsl;
                
                if (!string.IsNullOrWhiteSpace(_config.SmtpUsername))
                {
                    client.Credentials = new NetworkCredential(_config.SmtpUsername, _config.SmtpPassword);
                }

                await client.SendMailAsync(message);
                _logger.LogInformation($"Error notification sent to {message.To.Count} recipient(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification");
            }
        }

        public async Task SendInfoNotificationAsync(string subject, string message)
        {
            try
            {
                if (_config.EmailEnabled)
                {
                    await SendEmailAsync($"Cash Drawer Info - {subject}", message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send info notification");
            }
        }
    }

    public class NotificationConfig
    {
        public bool EmailEnabled { get; set; }
        public bool PopupEnabled { get; set; }
        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string FromEmail { get; set; } = "cashdrawer@company.com";
        public List<string> AdminEmails { get; set; } = new();
    }
}
