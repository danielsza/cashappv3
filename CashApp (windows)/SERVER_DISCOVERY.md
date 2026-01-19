# Server Discovery - Automatic Network Detection

## Overview

The Cash Drawer Client can automatically discover servers on your local network, eliminating the need to manually configure IP addresses. This makes deployment much easier, especially for multiple locations.

## How It Works

### Discovery Methods

The client uses **two methods** to find servers:

1. **UDP Broadcast Discovery** (Primary Method)
   - Client broadcasts a discovery request on port 5001
   - Servers respond with their information
   - Fast and reliable for local networks
   - Works across subnets in some network configurations

2. **Smart IP Scanning** (Fallback Method)
   - Scans common server IP addresses on your subnet
   - Checks addresses like .10, .11, .20, .21, .100, .101, .200, .201
   - Quick verification with ping command
   - Used when broadcast discovery doesn't find servers

### What Gets Discovered

For each server found, the client discovers:
- **IP Address** - Network address of the server
- **Port Number** - Server port (usually 5000)
- **Server ID** - Unique identifier (SERVER1, SERVER2, etc.)

## Using Discovery

### Method 1: Automatic on First Run

When you start the client for the first time with no configured servers:

1. Client automatically searches the network
2. Displays list of found servers
3. Asks if you want to use them
4. Saves configuration automatically

```
Found 2 server(s):

1. SERVER1
   192.168.1.10:5000

2. SERVER2
   192.168.1.11:5000

Would you like to use these servers?
[Yes] [No]
```

### Method 2: Manual Discovery Button

From the Settings tab:

1. Click **"🔍 Discover Servers"** button
2. Client searches for ~3 seconds
3. Shows list of found servers
4. Select "Yes" to populate server fields
5. Click "Save Settings" to keep them

### Method 3: Command Line Test

Test discovery from command line:

```python
python CashClient.py --discover
```

Or within the Python interpreter:

```python
from CashClient import CashClient
client = CashClient()
servers = client.scan_local_network()
for ip, port, server_id in servers:
    print(f"{server_id}: {ip}:{port}")
```

## Network Requirements

### Server Configuration

Servers must:
- Be running (UDP listener on port 5001)
- Be on the same network segment
- Have firewall rules allowing UDP port 5001 inbound
- Have firewall rules allowing TCP port 5000 inbound

### Client Configuration

Clients must:
- Be on the same network as servers
- Have broadcast permissions (usually automatic)
- Have firewall allowing UDP outbound
- Have firewall allowing TCP port 5000 outbound

### Network Configuration

Discovery works best with:
- ✅ Simple networks (single subnet)
- ✅ Standard WiFi networks
- ✅ Wired networks (Ethernet)
- ⚠️ May require configuration for VLANs
- ⚠️ May not work across routed networks
- ⚠️ May not work through some VPN connections

## Firewall Configuration

### Windows Firewall Rules

**On Server:**
```powershell
# Allow UDP discovery port
netsh advfirewall firewall add rule name="Cash Server Discovery" dir=in action=allow protocol=UDP localport=5001

# Allow TCP server port
netsh advfirewall firewall add rule name="Cash Server TCP" dir=in action=allow protocol=TCP localport=5000
```

**On Client:**
```powershell
# Usually no configuration needed, but if required:
netsh advfirewall firewall add rule name="Cash Client Outbound" dir=out action=allow protocol=ANY remoteport=5000,5001
```

### Testing Firewall

Test if discovery port is accessible:

```cmd
# From client computer, test server at 192.168.1.10
Test-NetConnection -ComputerName 192.168.1.10 -Port 5001
Test-NetConnection -ComputerName 192.168.1.10 -Port 5000
```

## Troubleshooting

### No Servers Found

**Problem**: Discovery finds no servers

**Solutions**:
1. Verify servers are running
   - Check server console shows "Discovery listener started"
   
2. Check network connectivity
   - Ping server IP: `ping 192.168.1.10`
   - Check same subnet: Client and server IPs should match pattern
   
3. Check firewall
   - Temporarily disable to test
   - Add proper firewall rules (see above)
   
4. Verify ports
   - Server should listen on TCP 5000 and UDP 5001
   - Use `netstat -an` to verify

5. Try manual configuration
   - If discovery fails, manually enter IP addresses in Settings

### Discovery Finds Wrong Server

**Problem**: Discovers servers from different location

**Solution**:
- Use manual configuration instead
- Set static server IPs in Settings tab
- Discovery works best on isolated networks

### Slow Discovery

**Problem**: Discovery takes too long

**Solutions**:
- Normal: 2-3 seconds is expected
- If longer: Check network congestion
- Configure servers manually for faster startup

### Discovery After Network Change

**Problem**: Need to rediscover after IP changes

**Solution**:
1. Go to Settings tab
2. Click "🔍 Discover Servers"
3. Approve new server addresses
4. Click "Save Settings"

## Advanced Configuration

### Customize Scanned IPs

Edit `CashClient.py` to add your preferred server IPs:

```python
def scan_local_network(self):
    # ...
    common_ips = [
        f"{subnet}.10",    # Your server IPs here
        f"{subnet}.11",
        f"{subnet}.50",    # Add custom IPs
        f"{subnet}.51",
        # Add more as needed
    ]
```

### Change Discovery Timeout

Adjust discovery timeout in `discover_servers()`:

```python
def discover_servers(self, timeout=3):  # Change from 3 to 5 for slower networks
```

### Disable Auto-Discovery

To disable automatic discovery on first run, edit config:

```ini
[Client]
AutoDiscover = false
```

Or manually create `client_config.ini` before first run.

## Technical Details

### UDP Broadcast Protocol

**Discovery Request** (from client):
```json
{
  "command": "discover",
  "type": "cash_client"
}
```

**Discovery Response** (from server):
```json
{
  "type": "cash_server",
  "server_id": "SERVER1",
  "port": 5000,
  "version": "3.0"
}
```

### Ports Used

| Port | Protocol | Purpose |
|------|----------|---------|
| 5000 | TCP | Main server communication |
| 5001 | UDP | Discovery broadcasts |

### Discovery Process Flow

```
Client                          Server
  |                               |
  |--[UDP Broadcast on 5001]---->|
  |  {"command": "discover"}      |
  |                               |
  |<--[UDP Response]-------------|
  |   {"server_id": "SERVER1"}   |
  |                               |
  |--[TCP Connect on 5000]------>|
  |   Verify with ping            |
  |<--[Success Response]---------|
  |                               |
  [Server Added to List]
```

## Security Considerations

### Discovery Security

- Discovery uses **UDP broadcast** (not encrypted)
- Anyone on network can see discovery requests
- Server information is public on local network
- Authentication still required to use server

### Recommendations

- Use discovery only on trusted networks
- Manually configure servers for sensitive environments
- Consider network segmentation for security
- Regular server access is still authenticated

## Examples

### Example 1: First Time Setup

```
User starts CashClient.py

Client: "No servers configured, searching network..."
Client: [Broadcasts UDP discovery]
Server1: [Responds with info]
Server2: [Responds with info]

Dialog: "Found 2 servers, would you like to use them?"
User: [Clicks Yes]

Client: "Server addresses updated! Click Save Settings"
User: [Clicks Save Settings]
Client: "Settings saved"

User: [Logs in and uses system]
```

### Example 2: Manual Discovery

```
User opens Settings tab
User clicks "🔍 Discover Servers"

Client: "Searching for servers on network..."
[3 second search]

Dialog: "Found 2 servers:
  1. SERVER1 at 192.168.1.10:5000
  2. SERVER2 at 192.168.1.11:5000
  
  Would you like to use these servers?"

User: [Clicks Yes]

Client fills in:
  Primary Server: 192.168.1.10
  Primary Port: 5000
  Secondary Server: 192.168.1.11
  Secondary Port: 5000

Status: "Found 2 servers - click Save Settings"

User: [Clicks Save Settings]
Client: "Settings saved successfully"
```

### Example 3: IP Changed, Rediscover

```
Server IP changed from .10 to .20

Client tries to connect: "Connection Failed"
User opens Settings tab
User clicks "🔍 Discover Servers"

Client finds server at new IP: 192.168.1.20
Dialog shows: "SERVER1 at 192.168.1.20:5000"

User: [Clicks Yes]
User: [Clicks Save Settings]

Client: "Connected to 192.168.1.20:5000"
```

## FAQ

**Q: Is discovery automatic?**  
A: Yes, on first run if no servers are configured. You can also manually trigger it.

**Q: Do I need to configure anything?**  
A: No, if servers are on the same network. Just run the client and it finds them.

**Q: What if discovery doesn't work?**  
A: Manually enter server IPs in Settings tab. Discovery is a convenience feature.

**Q: Can I disable discovery?**  
A: Configure servers manually in Settings, then discovery won't run automatically.

**Q: Does it work over the internet?**  
A: No, only on local networks. Discovery uses broadcast which is local only.

**Q: Is it secure?**  
A: Discovery is unencrypted but only reveals server presence. Authentication is still required to use servers.

**Q: How often does it search?**  
A: Only when you click Discover, or on first run with no configuration.

---

**Feature**: Auto-Discovery  
**Status**: ✅ Active  
**Ports**: UDP 5001 (discovery), TCP 5000 (server)  
**Network**: Local network only  
**Security**: Server authentication still required
