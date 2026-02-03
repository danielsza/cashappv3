namespace CashDrawer.ServerControl.Models
{
    public class ControlServiceConfig
    {
        public int Port { get; set; } = 5002;
        public string ServiceName { get; set; } = "CashDrawerServer";
        public string AuthToken { get; set; } = "";
        public List<string> AllowedIPs { get; set; } = new();
    }
}
