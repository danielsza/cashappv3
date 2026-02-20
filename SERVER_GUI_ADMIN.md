# Server GUI Administration - Complete Solution

## 🎯 What I'm Creating for You

### 1. **System Tray Server Manager** ⭐
- Runs in system tray (background)
- Right-click menu for quick access
- Requires admin authentication
- First-run setup wizard

### 2. **First-Run Setup**
- Automatic on first launch
- Forces admin account creation
- Can't skip (security!)
- Guided setup wizard

### 3. **Admin-Only Access**
- Every configuration change requires admin login
- Password authentication before opening manager
- Only admins can add/edit/delete users
- Regular users can't access settings

---

## 🎨 Features Included

### System Tray Icon
```
Right-click tray icon:
  ├─ Open Manager (requires admin auth)
  ├─ Service Status
  ├─ Exit
```

### Server Manager Tabs:

**Tab 1: Server Configuration**
- Server ID
- TCP Port
- COM Port (dropdown COM1-COM20)
- Relay Type (dropdown: DTR, RTS, etc.)
- Relay Duration
- Log Path (with browse button)
- Test COM Port button

**Tab 2: User Management**
- User list (shows username, level, locked status)
- Add User button
- Edit User button
- Delete User button
- Unlock Account button
- Only admins can access

**Tab 3: Transaction Logs**
- Shows recent transactions
- Refresh button
- Read-only view
- Today's log file

---

## 🚀 First-Run Experience

### When Server Starts for First Time:

```
1. Server detects no users exist
2. Shows dialog:
   ┌────────────────────────────────┐
   │  First Time Setup              │
   ├────────────────────────────────┤
   │  No admin account found!       │
   │                                │
   │  Create administrator account  │
   │  to manage the server.         │
   │                                │
   │  Username: [__________]        │
   │  Password: [__________]        │
   │  Name:     [__________]        │
   │                                │
   │  [ Create Admin ]  [ Cancel ]  │
   └────────────────────────────────┘

3. If Cancel: Server exits (can't run without admin)
4. If Create: Admin account created, server starts
5. Manager opens for initial configuration
```

---

## 🔐 Security Model

### Access Levels:

**Admin Users:**
- ✅ Open server manager
- ✅ Edit server settings
- ✅ Add/edit/delete users
- ✅ View logs
- ✅ Unlock accounts
- ✅ Open drawers (via client)

**Regular Users:**
- ❌ Cannot open server manager
- ❌ Cannot edit settings
- ❌ Cannot manage users
- ✅ Can open drawers (via client)

### Authentication Flow:

```
Manager Access:
1. Click system tray icon → "Open Manager"
2. If not authenticated:
   ┌────────────────────────┐
   │ Administrator Login    │
   ├────────────────────────┤
   │ Username: [_______]    │
   │ Password: [_______]    │
   │                        │
   │ [ Login ]  [ Cancel ]  │
   └────────────────────────┘
3. Verify user is admin level
4. Open manager
5. Session stays authenticated
```

---

## 💻 How It Works

### File: `ServerManagerForm.cs`
- Main management GUI
- System tray integration
- Three tabs (Server, Users, Logs)
- Admin authentication required

### File: `FirstRunSetupDialog.cs`
- Shown on first run
- Creates initial admin account
- Cannot be skipped
- Validates password strength

### File: `AdminAuthDialog.cs`
- Authentication dialog
- Verifies admin level
- Session management
- Failed attempt tracking

### File: `UserEditorDialog.cs`
- Add/Edit user form
- Username, password, name, level
- Password confirmation
- Validation

---

## 🎯 Usage Examples

### First Time Running Server:

```bash
# Run server
CashDrawer.Server.exe

# Automatic first-run dialog appears:
"Create administrator account"

# Fill in:
Username: admin
Password: Admin123!
Name: Administrator

# Click "Create Admin"
# Server starts with tray icon
# Manager opens for configuration
```

### Daily Operations:

```bash
# Server runs in background (tray)

# To configure:
1. Right-click tray icon
2. Click "Open Manager"
3. Enter admin credentials
4. Make changes
5. Click "Save Changes"

# To manage users:
1. Open Manager (authenticate)
2. Go to "User Management" tab
3. Click "Add User"
4. Fill in details
5. Save

# To view logs:
1. Open Manager
2. Go to "Transaction Logs" tab
3. Click "Refresh Logs"
```

---

## 📋 Implementation Plan

I'll create these files:

### Core Files:
1. ✅ `ServerManagerForm.cs` - Main manager (started)
2. `ServerManagerForm_Events.cs` - Event handlers
3. `FirstRunSetupDialog.cs` - First-run wizard
4. `AdminAuthDialog.cs` - Admin authentication
5. `UserEditorDialog.cs` - Add/edit users
6. `SettingsManager.cs` - Save/load config

### Integration:
7. Update `Program.cs` - Add tray icon mode
8. Add command line args: `--console` or `--tray`

---

## 🎨 UI Screenshots (Concept)

### System Tray:
```
[💰 Cash Drawer Server]
├─ Open Manager
├─ Service Status: Running
├─ Users: 5 active
└─ Exit
```

### Manager Window:
```
┌──────────────────────────────────────────┐
│ Cash Drawer Server Manager               │
├──────────────────────────────────────────┤
│ [Server Config] [Users] [Logs]           │
├──────────────────────────────────────────┤
│ Server Configuration                      │
│                                           │
│ Server ID:    [SERVER1_____]             │
│ TCP Port:     [5000]                     │
│ COM Port:     [COM10 ▼]                  │
│ Relay Type:   [DTR ▼]                    │
│ Duration:     [0.5] seconds              │
│ Log Path:     [\\server\logs] [Browse]   │
│                                           │
│ [Test COM Port]                          │
│                                           │
│         [Save Changes]  [Cancel]         │
└──────────────────────────────────────────┘
```

### User Management:
```
┌────────────────────────────────────┐
│ Users                   │ Actions  │
├─────────────────────────┼──────────┤
│ ✓ admin    [ADMIN]      │          │
│ ✓ 709      [USER]       │ ➕ Add   │
│ ✓ cashier1 [USER]       │          │
│ 🔒 temp     [USER]       │ ✏ Edit   │
│                         │          │
│                         │ 🗑 Delete │
│                         │          │
│                         │ 🔓 Unlock │
└─────────────────────────┴──────────┘
```

---

## 🚀 Command Line Options

### Console Mode (Current):
```bash
CashDrawer.Server.exe
# Shows console window with logs
```

### Tray Mode (New):
```bash
CashDrawer.Server.exe --tray
# Runs in system tray, no console
```

### Service Mode:
```bash
# Installed as Windows Service
# No UI, background only
```

---

## ✅ What Gets Saved

When you click "Save Changes":

### Server Settings → appsettings.json:
```json
{
  "Server": {
    "ServerID": "SERVER1",
    "Port": 5000,
    "COMPort": "COM10",
    "RelayPin": "DTR",
    "RelayDuration": 0.5,
    "LogPath": "\\\\server\\logs"
  }
}
```

### User Changes → users.json:
```json
{
  "admin": {
    "Username": "admin",
    "Name": "Administrator",
    "PasswordHash": "$2a$11$...",
    "Level": 1,
    "FailedAttempts": 0
  },
  "709": {
    "Username": "709",
    "Name": "Cashier",
    "PasswordHash": "$2a$11$...",
    "Level": 0
  }
}
```

### Changes Apply:
- Some changes require restart (ports)
- User changes: immediate
- Settings reload on save

---

## 🎯 Would You Like Me To:

1. **Complete the full implementation?**
   - All dialog classes
   - Event handlers
   - Settings persistence
   - Full integration

2. **Create standalone admin tool instead?**
   - Separate EXE for administration
   - Server stays console-only
   - Connects to running server

3. **Both?**
   - Tray icon in server
   - Separate admin tool
   - Choose which to use

**Which approach do you prefer?**

---

## 💡 My Recommendation

**Option: Integrated System Tray Manager**

**Pros:**
- ✅ Everything in one place
- ✅ Always accessible (tray icon)
- ✅ No separate tools needed
- ✅ First-run setup built-in
- ✅ Admin auth enforced

**Cons:**
- Server EXE slightly larger (~2 MB more)
- Requires WinForms for server

**This is the most user-friendly approach!**

Let me know and I'll complete the full implementation! 🚀
