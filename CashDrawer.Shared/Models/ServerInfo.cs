namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// Information about a discovered server
    /// </summary>
    public class ServerInfo
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ServerID { get; set; } = string.Empty;
    }
}
