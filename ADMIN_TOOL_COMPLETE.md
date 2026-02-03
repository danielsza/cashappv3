# 🔧 Admin Tool - Complete! ✅

## ✅ What's Been Created

**CashDrawer.AdminTool** - Full-featured GUI administration application!

### Features:
- ✅ **First-Run Setup Wizard** - Auto-detects no users, forces admin creation
- ✅ **Server Configuration** - Edit all settings (ports, COM, relay, paths)
- ✅ **User Management** - Add/Edit/Delete users
- ✅ **Password Management** - Change passwords without editing user
- ✅ **Account Unlocking** - Unlock locked accounts
- ✅ **Transaction Logs** - View recent transactions
- ✅ **Clean GUI** - Professional tabs and layout
- ✅ **Validation** - Prevents mistakes (can't delete last admin, etc.)

---

## 🚀 Build the Admin Tool

### Build:
```bash
cd CashDrawerCS/CashDrawer.AdminTool
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

**Output:**  
`bin/Release/net8.0-windows/win-x64/publish/CashDrawer.AdminTool.exe`

### Or Build All Three:
```bash
cd CashDrawerCS
dotnet build
```

---

## 🎯 How to Use

### 1. Run Admin Tool:
```bash
CashDrawer.AdminTool.exe
```

### 2. Select Server Folder:
```
Dialog appears:
"Select Cash Drawer Server folder"

Browse to folder containing:
- CashDrawer.Server.exe
- appsettings.json
- users.json (or will be created)

Click: Select Folder
```

### 3. First-Run Setup (If No Users):
```
If users.json doesn't exist or is empty:

Dialog:
┌──────────────────────────────┐
│ No users found!              │
│                              │
│ Create admin account now?    │
│                              │
│  [ Yes ]        [ No ]       │
└──────────────────────────────┘

Click: Yes

Create Administrator Account:
┌──────────────────────────────┐
│ Username:  [admin___]        │
│ Full Name: [Administrator_]  │
│ Password:  [••••••••]        │
│ Confirm:   [••••••••]        │
│ Level:     [Admin ▼]         │
│                              │
│  [ Save ]      [ Cancel ]    │
└──────────────────────────────┘

Fill in and click Save
Admin account created!
```

### 4. Main Window Opens:
```
┌─────────────────────────────────────────┐
│ 🔧 Server Administration                │
│ Server: C:\CashDrawer                   │
├─────────────────────────────────────────┤
│ [⚙ Server Config] [👥 Users] [📋 Logs] │
├─────────────────────────────────────────┤
│                                          │
│  (Tab content here)                      │
│                                          │
│                                          │
│                                          │
│ [📁 Change Folder] [💾 Save] [Cancel]   │
└─────────────────────────────────────────┘
```

---

## 📋 Features by Tab

### ⚙ Server Configuration Tab:

**Edit:**
- Server ID
- TCP Port
- COM Port (dropdown)
- Relay Type (DTR/RTS/etc.)
- Relay Duration
- Network Log Path
- Local Log Path

**Includes:**
- Browse buttons for paths
- Info panel with notes
- Validation

### 👥 User Management Tab:

**Left Panel - User List:**
```
✓ admin    [ADMIN]    Administrator
✓ 709      [USER]     Cashier
🔒 temp     [USER]     Temp Worker
```

**Right Panel - Actions:**
- ➕ **Add User** - Create new user/admin
- ✏ **Edit User** - Change name/level
- 🔑 **Change Password** - Update password
- 🔓 **Unlock Account** - Unlock after failed attempts
- 🗑 **Delete User** - Remove user (can't delete last admin!)

**User Details Box:**
Shows selected user info:
- Username
- Full name
- Level
- Created date
- Failed attempts
- Locked status

### 📋 Transaction Logs Tab:

- View today's transactions
- Refresh button
- Read-only display
- Last 100 entries

### ℹ About Tab:

- Version information
- Feature list
- Instructions

---

## 🎨 Usage Examples

### Add a User:
```
1. Click: 👥 User Management tab
2. Click: ➕ Add User
3. Fill in:
   Username: cashier1
   Name: John Smith
   Password: ••••
   Confirm: ••••
   Level: User
4. Click: Save
5. User appears in list!
```

### Change Password:
```
1. Select user in list
2. Click: 🔑 Change Password
3. Enter new password (twice)
4. Click: Change Password
5. Done!
```

### Edit Server Settings:
```
1. Click: ⚙ Server Configuration tab
2. Change settings:
   COM Port: COM10 ▼
   Relay Type: DTR ▼
   Duration: 0.5
3. Click: 💾 Save All Changes
4. Restart server for changes to take effect
```

### Unlock Account:
```
1. Select locked user (🔒 icon)
2. Click: 🔓 Unlock Account
3. Confirmed!
4. Icon changes to ✓
```

---

## 🔐 Security Features

### Prevents Mistakes:
- ✅ Can't delete last admin
- ✅ Password confirmation required
- ✅ Minimum 4-character passwords
- ✅ Username validation
- ✅ Can't change username (prevents issues)

### First-Run Enforcement:
- ✅ Detects no users
- ✅ Forces admin creation
- ✅ Can't skip or cancel
- ✅ Admin-only level forced

---

## 💾 What Gets Saved

### When You Click "Save All Changes":

**appsettings.json:**
```json
{
  "Server": {
    "ServerID": "SERVER1",
    "Port": 5000,
    "COMPort": "COM10",
    "RelayPin": "DTR",
    "RelayDuration": 0.5,
    "LogPath": "\\\\server\\logs",
    "LocalLogPath": "./Logs"
  }
}
```

**users.json:**
```json
{
  "admin": {
    "Username": "admin",
    "Name": "Administrator",
    "PasswordHash": "$2a$11$...",
    "Level": 1,
    "FailedAttempts": 0,
    "Created": "2025-01-20T00:00:00"
  },
  "709": {
    "Username": "709",
    "Name": "Cashier",
    "PasswordHash": "$2a$11$...",
    "Level": 0
  }
}
```

---

## 📦 Complete Deployment Package

### Build All Three:

**Server:**
```bash
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Deploy/Server
```

**Client:**
```bash
cd ../CashDrawer.Client
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Deploy/Client
```

**Admin Tool:**
```bash
cd ../CashDrawer.AdminTool
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Deploy/AdminTool
```

**Result:**
```
Deploy/
├── Server/
│   └── CashDrawer.Server.exe
├── Client/
│   └── CashDrawer.Client.exe
└── AdminTool/
    └── CashDrawer.AdminTool.exe
```

---

## 🎯 Typical Workflow

### Initial Setup:
```
1. Copy CashDrawer.Server.exe to server folder
2. Run CashDrawer.AdminTool.exe
3. Point to server folder
4. Create admin account (forced)
5. Configure server settings
6. Add users (cashiers)
7. Click Save
8. Start server
```

### Daily Operations:
```
Staff:
- Use CashDrawer.Client.exe
- Enter password per transaction
- Drawer opens

Admin:
- Use CashDrawer.AdminTool.exe when needed
- Add/edit users
- View logs
- Change settings
```

---

## 🎊 What You Have Now

✅ **Server** - Runs in background, controls drawer
✅ **Client** - Staff use to open drawer
✅ **Admin Tool** - Manage everything with GUI

**Three separate executables:**
- Clean separation of concerns
- No build conflicts
- Professional structure
- Easy to maintain

---

## 🚀 Next Steps

1. **Build everything:**
   ```bash
   dotnet build
   ```

2. **Test the Admin Tool:**
   ```bash
   cd CashDrawer.AdminTool/bin/Debug/net8.0-windows
   ./CashDrawer.AdminTool.exe
   ```

3. **Point to server folder**

4. **Create first admin account**

5. **Add users**

6. **Configure settings**

7. **Test with server & client**

---

## 💡 Tips

- **Admin Tool can run anywhere** - Just point it to server folder (even network path!)
- **Multiple admins** - Can edit same server (save when done)
- **Safe deletes** - Can't delete last admin
- **Password hashing** - BCrypt automatically
- **First run** - Auto-detects and guides setup

---

**Everything is ready! Build and test the Admin Tool!** 🎉
