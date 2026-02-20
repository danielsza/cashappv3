using System;

namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// Safe drop record for tracking cash deposits to safe
    /// </summary>
    public class SafeDropEntry
    {
        /// <summary>
        /// Unique ID for sync deduplication (ServerID-Timestamp)
        /// </summary>
        public string Id { get; set; } = "";
        
        /// <summary>
        /// Server where the safe drop was recorded
        /// </summary>
        public string ServerID { get; set; } = "";
        
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
        public string Username { get; set; } = "";
        public string Invoice { get; set; } = "";
        public bool Confirmed { get; set; }
        
        /// <summary>
        /// Generate unique ID if not set
        /// </summary>
        public void GenerateId(string serverID)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = $"{serverID}-SD-{Timestamp:yyyyMMddHHmmssfff}";
                ServerID = serverID;
            }
        }
    }
}
