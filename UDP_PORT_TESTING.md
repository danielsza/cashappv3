# UDP Port 5001 - Testing Guide

## 🔍 Tests to Check if Port is Blocked

### Test 1: Check Server is Listening (EASIEST)

**On the server machine:**

```bash
# Option A: Using netstat (Windows)
netstat -an | findstr 5001

# Should show:
UDP    0.0.0.0:5001    *:*

# Option B: Using PowerShell
Get-NetUDPEndpoint | Where-Object LocalPort -eq 5001

# Should show listening on port 5001
```

**What to look for:**
- ✅ If you see `:5001` listed → Server IS listening
- ❌ If nothing shows → Server NOT listening on port 5001

---

### Test 2: Simple UDP Test with PowerShell (BEST)

**On the CLIENT machine, run this PowerShell script:**

```powershell
# Replace with your actual server IP
$serverIP = "192.168.1.100"
$port = 5001

$udpClient = New-Object System.Net.Sockets.UdpClient
$udpClient.Client.ReceiveTimeout = 5000

try {
    # Send test message
    $message = [System.Text.Encoding]::UTF8.GetBytes('{"command":"discover","type":"cash_client"}')
    $endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse($serverIP), $port)
    
    Write-Host "Sending UDP packet to $serverIP:$port..."
    $udpClient.Send($message, $message.Length, $endpoint)
    
    Write-Host "Listening for response..."
    $remoteEP = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
    $response = $udpClient.Receive([ref]$remoteEP)
    
    Write-Host "✅ SUCCESS! Received response from $($remoteEP.Address):$($remoteEP.Port)"
    Write-Host "Response: $([System.Text.Encoding]::UTF8.GetString($response))"
} catch {
    Write-Host "❌ FAILED: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Possible causes:"
    Write-Host "  - Firewall blocking UDP port 5001"
    Write-Host "  - Server not running"
    Write-Host "  - Wrong IP address"
} finally {
    $udpClient.Close()
}
```

**Expected Results:**
- ✅ **Success:** You'll see the server's JSON response
- ❌ **Timeout:** Firewall is blocking OR server not responding

---

### Test 3: Check Windows Firewall Rules

**On BOTH machines (server AND client):**

```powershell
# Check if port 5001 is allowed
Get-NetFirewallRule | Where-Object {$_.LocalPort -eq 5001} | Format-Table Name, Enabled, Direction, Action

# Check for blocking rules
Get-NetFirewallRule | Where-Object {$_.LocalPort -eq 5001 -and $_.Action -eq 'Block'}
```

**What to look for:**
- ✅ If you see "Allow" rules → Port is allowed
- ❌ If you see "Block" rules → Port is blocked
- ⚠️ If nothing shows → No specific rule (default policy applies)

---

### Test 4: Temporarily Disable Firewall (TESTING ONLY!)

**⚠️ WARNING: Only for testing! Re-enable after test!**

**On the SERVER machine:**

```powershell
# Disable firewall (as Administrator)
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False

# Now try discovery from client

# Re-enable firewall IMMEDIATELY after test
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
```

**Results:**
- ✅ If discovery works with firewall off → Firewall is blocking
- ❌ If discovery still fails → Not a firewall issue

---

### Test 5: Check Server Logs

**While running Test 2 above, check the server console:**

**What to look for:**
```
✅ GOOD - Server receives request:
"Discovery request from 192.168.1.101:xxxxx"

❌ BAD - No message:
Server never receives the packet = firewall blocking
```

---

### Test 6: Use Test-NetConnection (TCP Only - Won't Work for UDP)

**Note:** Test-NetConnection only works for TCP, NOT UDP. But you can test if you can reach the server at all:

```powershell
# Test if server is reachable
Test-NetConnection -ComputerName 192.168.1.100 -Port 5000

# This tests TCP port 5000 (your main server port)
```

---

## 🔧 If Port is Blocked - How to Fix

### Create Firewall Rule (Windows)

**On the SERVER machine (as Administrator):**

```powershell
# Create inbound rule for UDP port 5001
New-NetFirewallRule -DisplayName "Cash Drawer Discovery (UDP In)" `
    -Direction Inbound `
    -Protocol UDP `
    -LocalPort 5001 `
    -Action Allow `
    -Profile Any

# Verify rule was created
Get-NetFirewallRule -DisplayName "Cash Drawer Discovery (UDP In)"
```

**On the CLIENT machine (as Administrator):**

```powershell
# Create outbound rule for UDP port 5001
New-NetFirewallRule -DisplayName "Cash Drawer Discovery (UDP Out)" `
    -Direction Outbound `
    -Protocol UDP `
    -LocalPort Any `
    -RemotePort 5001 `
    -Action Allow `
    -Profile Any
```

### Or Use GUI:

1. **Windows Defender Firewall with Advanced Security**
2. **Inbound Rules** → **New Rule**
3. **Port** → **UDP** → **5001**
4. **Allow the connection**
5. Apply to all profiles (Domain, Private, Public)
6. Name: "Cash Drawer Discovery"

---

## 🎯 Quick Diagnosis

Run this on CLIENT machine:

```powershell
Write-Host "=== Cash Drawer Discovery Test ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if you can ping server
$serverIP = "192.168.1.100"  # Change this!
Write-Host "1. Testing network connectivity..." -ForegroundColor Yellow
if (Test-Connection -ComputerName $serverIP -Count 1 -Quiet) {
    Write-Host "   ✅ Server is reachable" -ForegroundColor Green
} else {
    Write-Host "   ❌ Cannot reach server - check network/IP" -ForegroundColor Red
    exit
}

# 2. Check if TCP port 5000 is open
Write-Host "2. Testing TCP port 5000..." -ForegroundColor Yellow
$tcpTest = Test-NetConnection -ComputerName $serverIP -Port 5000 -WarningAction SilentlyContinue
if ($tcpTest.TcpTestSucceeded) {
    Write-Host "   ✅ TCP port 5000 is open" -ForegroundColor Green
} else {
    Write-Host "   ❌ TCP port 5000 is blocked" -ForegroundColor Red
}

# 3. Test UDP discovery
Write-Host "3. Testing UDP discovery on port 5001..." -ForegroundColor Yellow
$udpClient = New-Object System.Net.Sockets.UdpClient
$udpClient.Client.ReceiveTimeout = 3000

try {
    $message = [System.Text.Encoding]::UTF8.GetBytes('{"command":"discover","type":"cash_client"}')
    $endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse($serverIP), 5001)
    
    $udpClient.Send($message, $message.Length, $endpoint) | Out-Null
    
    $remoteEP = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
    $response = $udpClient.Receive([ref]$remoteEP)
    
    Write-Host "   ✅ UDP discovery WORKS!" -ForegroundColor Green
    Write-Host "   Response: $([System.Text.Encoding]::UTF8.GetString($response))" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ UDP discovery FAILED" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Likely cause: Firewall blocking UDP port 5001" -ForegroundColor Yellow
} finally {
    $udpClient.Close()
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
```

**Save this as `test-discovery.ps1` and run:**
```powershell
powershell -ExecutionPolicy Bypass -File test-discovery.ps1
```

---

## 🎯 Expected Results:

### If Everything Works:
```
=== Cash Drawer Discovery Test ===

1. Testing network connectivity...
   ✅ Server is reachable
2. Testing TCP port 5000...
   ✅ TCP port 5000 is open
3. Testing UDP discovery on port 5001...
   ✅ UDP discovery WORKS!
   Response: {"Type":"cash_server","ServerID":"SERVER1","Port":5000}

=== Test Complete ===
```

### If Firewall Blocks UDP:
```
=== Cash Drawer Discovery Test ===

1. Testing network connectivity...
   ✅ Server is reachable
2. Testing TCP port 5000...
   ✅ TCP port 5000 is open
3. Testing UDP discovery on port 5001...
   ❌ UDP discovery FAILED
   Error: A connection attempt failed...
   
   Likely cause: Firewall blocking UDP port 5001

=== Test Complete ===
```

---

## 💡 Summary:

**Best test:** Run the PowerShell script above. It will tell you exactly what's wrong!

**Quick fix if blocked:**
```powershell
# Run as Administrator on SERVER
New-NetFirewallRule -DisplayName "Cash Drawer Discovery" `
    -Direction Inbound -Protocol UDP -LocalPort 5001 -Action Allow
```

**Alternative:** Just use manual connection - works great without discovery!

---

Let me know what the test shows!
