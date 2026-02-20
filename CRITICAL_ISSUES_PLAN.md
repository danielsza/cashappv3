# Critical Issues - Fix Plan

## 🚨 Issues Reported

### 1. ✅ Discovery Still Doesn't Work
**Status:** Need to fix
**Problem:** Client keeps showing "Discovering..." forever
**Root cause:** UDP discovery implementation issue

### 2. ✅ No Secondary Server Config
**Status:** Need to add
**Problem:** Settings has no backup server option (despite code existing)
**Solution:** Enable and wire up backup server UI

### 3. ✅ Manual Connection Doesn't Persist
**Status:** Need to fix
**Problem:** Test works but doesn't stay connected
**Root cause:** Connection not persisted after settings dialog closes

### 4. ✅ First Line Cut Off (Admin Tool)
**Status:** Partially fixed, needs more
**Current:** Server ID at y=15 with padding=30
**Need:** More space or scroll position adjustment

### 5. ✅ Cancel Button Too Far Right
**Status:** Fixed
**New position:** x=840 (was x=830)

### 6. ✅ Cannot Manage Users After Initial Admin
**Status:** Need to investigate
**Problem:** Users tab exists but not accessible?
**Possible cause:** Tab not visible or event handlers not firing

### 7. ❓ Server User Syncing
**Question:** Is peer server sync implemented?
**Python version:** Synced users between servers
**C# version:** Need to verify if implemented

### 8. ❓ Server IP Announcement
**Question:** Does server announce its IP in discovery response?
**Python version:** Sent IP address in UDP response
**C# version:** Need to verify

---

## 🔧 Fix Priority Order

1. **CRITICAL: Client Discovery** - Can't connect = unusable
2. **HIGH: Manual Connection Persistence** - Workaround exists but annoying
3. **HIGH: User Management Access** - Can't add cashiers = blocker
4. **MEDIUM: Backup Server UI** - Important for failover
5. **MEDIUM: Admin Tool UI** - Usability issues
6. **LOW: Server Sync Verification** - May already work

---

## 📋 Detailed Fix Plan

### Fix 1: Client Discovery
**Files to check:**
- `NetworkClient.cs` - DiscoverServersAsync()
- UDP broadcast on port 5001
- Server responds with ServerID, IP, Port

**Test:**
```
1. Server running on 192.168.1.100:5000
2. Client starts
3. Sends UDP broadcast to 255.255.255.255:5001
4. Server responds with JSON
5. Client parses and shows in list
```

### Fix 2: Backup Server Config
**Files to modify:**
- `SettingsDialog.cs` - Already has backup fields!
- Just need to ensure they're visible and working
- Save to ClientSettings.json
- Use in connection logic

### Fix 3: Connection Persistence  
**Files to modify:**
- `MainForm.cs` - DiscoverAndConnectAsync()
- After successful connection, don't keep retrying
- Show "Connected" and stay connected

### Fix 4: User Management Tab
**Verify:**
- Tab is created ✓
- Tab is added to TabControl ✓
- Event handlers exist ✓
- **Need to check:** Why can't user access it?

### Fix 5: Admin Tool UI
**Changes:**
- Server ID: More top padding ✓
- Cancel button: Closer to Save ✓

### Fix 6: Server Peer Communication
**Python version had:**
- Peer server host/port config
- Connected to peer on startup
- Synced user changes
- Announced IP in discovery

**C# version needs:**
- Verify UdpServerService announces IP
- Verify peer connection code exists
- Add user sync over TCP

---

## 🧪 Testing Plan

### Test Discovery:
```bash
# Terminal 1: Server
cd CashDrawer.Server/bin/Debug
./CashDrawer.Server.exe

# Terminal 2: Client
cd CashDrawer.Client/bin/Debug  
./CashDrawer.Client.exe

Expected:
- Client shows "Discovering..."
- After 1-2 seconds: "Connected to SERVER1"
- Server shows: "Client connected from 192.168.1.101"
```

### Test User Management:
```bash
1. Admin Tool → Select server folder
2. Create admin (if needed)
3. Look for tabs: [⚙ Server Config] [👥 Users] [📋 Logs]
4. Click "👥 Users" tab
5. Should see user list and buttons
```

### Test Backup Server:
```bash
1. Client → Settings
2. Fill Primary: 192.168.1.100:5000
3. Check "Enable Backup"
4. Fill Backup: 192.168.1.101:5000
5. Save
6. Stop primary server
7. Client should failover to backup
```

---

## 🎯 Quick Wins (Do First)

1. **Fix Cancel button** ✅ DONE
2. **Fix Server ID padding** ✅ DONE  
3. **Verify User Management tab visible** - CHECK NEXT
4. **Fix discovery timeout** - CRITICAL
5. **Wire up backup server** - HIGH PRIORITY

---

## 📝 Code Locations

### Discovery:
- `CashDrawer.Client/NetworkClient.cs` - DiscoverServersAsync()
- `CashDrawer.Server/Services/UdpServerService.cs` - HandleDiscoveryRequest()

### Connection:
- `CashDrawer.Client/MainForm.cs` - DiscoverAndConnectAsync()
- `CashDrawer.Client/NetworkClient.cs` - Connect()

### Settings:
- `CashDrawer.Client/SettingsDialog.cs` - Backup server fields
- `CashDrawer.Client/ClientSettings.cs` - Backup properties

### User Sync:
- `CashDrawer.Server/Services/UserService.cs` - SaveUsers()
- Need to add peer notification

---

## ⚠️ Known Issues from Python Version

**Python v3 had:**
1. Dual failover (primary + backup)
2. User sync between servers
3. UDP discovery with IP announcement
4. Persistent connections
5. Canadian penny rounding
6. Password-per-transaction
7. Document type validation
8. Comprehensive logging

**C# version status:**
- ✅ Penny rounding
- ✅ Password-per-transaction  
- ✅ Document validation
- ✅ Comprehensive logging
- ❓ Discovery (broken?)
- ❓ Failover (UI exists, wired up?)
- ❓ User sync (not implemented?)
- ❓ IP announcement (need to verify)

---

**Next: Fix discovery and connection persistence!**
