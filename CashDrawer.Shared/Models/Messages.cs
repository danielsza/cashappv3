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
        public string? NewPassword { get; set; }  // Used by change_own_password
        public string? Reason { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public decimal Total { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
        public object? Data { get; set; }  // For generic data passing (e.g., user sync)
        public string? ClientIP { get; set; }  // Client IP address (set by server from connection)

        /// <summary>
        /// Idempotency key for open_drawer. The client mints one per user-initiated
        /// submission and reuses it for every automatic retry of that submission, so a
        /// retry after a lost response cannot log the transaction a second time.
        /// The server adopts it as the TransactionId, which also makes the dedupe work
        /// across servers (a retry that fails over to the peer produces the same ID,
        /// so peer sync collapses the two copies instead of keeping both).
        /// Null from pre-3.11.5 clients - the server then generates an ID as before.
        /// </summary>
        public string? ClientTransactionId { get; set; }

        /// <summary>
        /// Set on an open_drawer that should only record the transaction, because the
        /// caller already opened the drawer for this operation. BOD and EOD open it
        /// up front (via open_drawer_only) so the cash can be counted, and would
        /// otherwise pop it a second time when the count is submitted.
        /// Ignored by pre-3.11.6 servers, which just open it again as they do today.
        /// </summary>
        public bool SkipDrawerOpen { get; set; }
    }
    
    /// <summary>
    /// Base response from server to client
    /// </summary>
    public class ServerResponse
    {
        public string Status { get; set; } = "error";
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Machine-readable reason for a failure, so the client can react to a
        /// specific one without matching on message text. Currently only
        /// <see cref="DrawerOpenFailed"/>. Null from pre-3.11.8 servers, and the
        /// client treats "no code" as "don't retry anywhere else" - a failure it
        /// can't identify must never be retried blindly against another server.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// The relay would not fire, so the drawer never opened and no transaction
        /// was recorded. Safe for the client to retry on another server driving the
        /// same till - and safe only because nothing was logged here.
        /// </summary>
        public const string DrawerOpenFailed = "DRAWER_OPEN_FAILED";
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
