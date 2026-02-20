using System;
using System.Collections.Generic;
using System.IO;

namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// Server configuration
    /// </summary>
    public class ServerConfig
    {
        public string ServerID { get; set; } = "SERVER1";
        public int Port { get; set; } = 5000;
        public int DiscoveryPort { get; set; } = 5001;
        public string COMPort { get; set; } = "COM10";
        public RelayType RelayPin { get; set; } = RelayType.DTR;
        public double RelayDuration { get; set; } = 0.5;
        public string LogPath { get; set; } = @"\\PARTSRV2\Parts\Cash";
        public string LocalLogPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CashDrawer",
            "Logs"
        );
        public string? PeerServerHost { get; set; }
        public int PeerServerPort { get; set; } = 5000;
        public List<string> PeerServers { get; set; } = new();
        public bool TestMode { get; set; } = false;  // When true, simulates drawer without opening relay
    }
    
    /// <summary>
    /// Security settings
    /// </summary>
    public class SecurityConfig
    {
        public string AuthToken { get; set; } = "default-token-change-me";
        public bool RequireAuth { get; set; } = true;
        public int MaxFailedAttempts { get; set; } = 3;
        public int LockoutDurationSeconds { get; set; } = 300;
        public int SessionTimeoutSeconds { get; set; } = 3600;
        public int SessionTimeoutMinutes { get; set; } = 480;
    }
    
    /// <summary>
    /// Petty cash configuration
    /// </summary>
    public class PettyCashConfig
    {
        public List<string> Recipients { get; set; } = new()
        {
            "Store Supplies",
            "Office Supplies",
            "Employee Reimbursement",
            "Postage",
            "Cleaning Supplies",
            "Misc Expense"
        };
        
        public List<string> Reasons { get; set; } = new()
        {
            "Office Supplies",
            "Postage",
            "Employee Lunch",
            "Cleaning Supplies",
            "Emergency Purchase",
            "Store Maintenance",
            "Other"
        };
    }
    
    /// <summary>
    /// Relay control types
    /// </summary>
    public enum RelayType
    {
        DTR,
        DTR_INVERTED,
        RTS,
        RTS_INVERTED,
        BYTES_ESC,
        BYTES_DLE,
        RELAY_COMMANDS  // For relay controllers that accept "relay on/off" text commands
    }
}
