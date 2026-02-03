using System;

namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// Base request from client to server
    /// </summary>
    public class ServerRequest
    {
        public string Command { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Reason { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public decimal Total { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
        public object? Data { get; set; }  // For generic data passing (e.g., user sync)
        public string? ClientIP { get; set; }  // Client IP address (set by server from connection)
    }
    
    /// <summary>
    /// Base response from server to client
    /// </summary>
    public class ServerResponse
    {
        public string Status { get; set; } = "error";
        public string Message { get; set; } = string.Empty;
        public string? ServerID { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }  // Full display name
        public object? Data { get; set; }
    }
    
    /// <summary>
    /// Authentication response
    /// </summary>
    public class AuthResponse
    {
        public string SessionToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public UserLevel Level { get; set; }
    }
    
    /// <summary>
    /// Server discovery broadcast
    /// </summary>
    public class DiscoveryMessage
    {
        public string Type { get; set; } = "cash_server";
        public string ServerID { get; set; } = string.Empty;
        public int Port { get; set; } = 5000;
        
        // Control server fields (optional)
        public bool? MainServerRunning { get; set; }  // Is main server running?
        public int? MainServerPort { get; set; }      // Main server port (usually 5000)
    }
}
