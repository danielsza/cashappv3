using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm : Form
    {
        private ListBox _serversList = null!;
        private Button _discoverButton = null!;
        private Button _refreshButton = null!;
        private TabControl _tabControl = null!;
        private Button _saveButton = null!;
        private Label _statusLabel = null!;
        
        private List<DiscoveredServer> _discoveredServers = new();
        private DiscoveredServer? _selectedServer;
        private User? _currentUser; // Track authenticated user

        // Server Config Controls
        private TextBox _serverIdText = null!;
        private NumericUpDown _portNumber = null!;
        private ComboBox _comPortCombo = null!;
        private ComboBox _relayTypeCombo = null!;
        private NumericUpDown _relayDurationNumber = null!;
        private TextBox _logPathText = null!;
        private TextBox _localLogPathText = null!;
        private CheckBox _testModeCheck = null!;
        private Button _testRelayButton = null!;
        private Button _startServerButton = null!;
        private Button _restartServerButton = null!;
        private Button _stopServerButton = null!;

        // Users Controls
        private ListBox _usersList = null!;
        private Button _addUserButton = null!;
        private Button _editUserButton = null!;
        private Button _deleteUserButton = null!;
        private Button _unlockUserButton = null!;
        private Button _resetPasswordButton = null!;
        private Label _userDetailsLabel = null!;

        public MainForm()
        {
            InitializeComponent();
            _ = DiscoverServersAsync(); // Auto-discover on startup
        }

        private class DiscoveredServer
        {
            public string ServerID { get; set; } = "";
            public string Host { get; set; } = "";
            public int Port { get; set; }           // Main server port (5000)
            public int ControlPort { get; set; }    // Control service port (5002)
            public bool IsConnected { get; set; }   // Is main server running?
            public ServerConfig? Config { get; set; }
            public List<User>? Users { get; set; }

            public override string ToString() => $"{ServerID} ({Host}:{Port}) - {(IsConnected ? "🟢 online" : "🔴 offline")}";
        }
    }
}
