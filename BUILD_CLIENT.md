# Building the Client Application

## ✅ Client Now Included!

The WinForms client has been created with:
- ✅ Modern UI with document types
- ✅ Document number field
- ✅ Transaction amounts
- ✅ Password-per-transaction dialog
- ✅ Auto-discovery of servers
- ✅ Clean, professional interface

---

## 🚀 Build Client EXE

### Quick Build:
```bash
cd CashDrawerCS/CashDrawer.Client
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

**Output:**  
`bin/Release/net8.0-windows/win-x64/publish/CashDrawer.Client.exe`

### Small Build (needs .NET 8):
```bash
cd CashDrawerCS/CashDrawer.Client
dotnet publish -c Release
```

**Output:**  
`bin/Release/net8.0-windows/publish/CashDrawer.Client.exe` (~500 KB)

---

## 📦 Build Both Server and Client

### Build Everything:
```bash
cd CashDrawerCS
dotnet build
```

### Publish Both (Standalone):
```bash
# Server
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Deploy/Server

# Client
cd ../CashDrawer.Client
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Deploy/Client
```

**Result:**
```
Deploy/
  ├── Server/
  │   └── CashDrawer.Server.exe  (~50 MB)
  └── Client/
      └── CashDrawer.Client.exe  (~50 MB)
```

---

## 🎯 What the Client Has

### Features:
- ✅ **Auto-Discovery** - Finds servers automatically
- ✅ **Password Dialog** - Per-transaction authentication
- ✅ **Document Types** - Invoice, Petty Cash, Change, Refund, BOD, EOD
- ✅ **Document Number** - Field for invoice/document #
- ✅ **Transaction Amounts** - Total, IN, OUT
- ✅ **Quick Open** - Fast open without filling form
- ✅ **Status Display** - Shows last action and user
- ✅ **Connection Status** - Green/Red indicator

### UI Layout:
```
┌─────────────────────────────────────┐
│ ● Connected      SERVER1 (10.0.0.1) │
├─────────────────────────────────────┤
│ Document Type                       │
│  □ Invoice      □ Refund            │
│  □ Petty Cash   □ BOD               │
│  □ Change       □ EOD               │
├─────────────────────────────────────┤
│ Transaction Details                 │
│  Document #:  [____________]        │
│  Total:       [0.00    ]            │
│  IN:          [0.00    ]            │
│  Out:         [0.00    ]            │
├─────────────────────────────────────┤
│  [  Open Drawer  ]  [ Quick Open ]  │
│                                     │
│  ✓ Drawer opened by 709 at 2:30 PM │
└─────────────────────────────────────┘
```

---

## 🧪 Test the Client

### 1. Start Server First:
```bash
cd Deploy/Server
CashDrawer.Server.exe
```

Server should show:
```
TCP server started on port 5000
Discovery service started on UDP port 5001
```

### 2. Start Client:
```bash
cd Deploy/Client
CashDrawer.Client.exe
```

### 3. Test Workflow:
```
1. Client opens
2. Auto-discovers server
3. Status shows: "● Connected"
4. Fill in transaction:
   - Check "Invoice"
   - Document #: INV12345
   - Total: 100.00
5. Click "Open Drawer"
6. Password dialog appears
7. Enter: 1234 (or your password)
8. Press Enter
9. Drawer opens!
10. Status shows: "✓ Drawer opened by 709"
```

---

## 📝 Create Users First

Before testing, create a user:

### Option 1: Using Server Directly
```bash
cd Deploy/Server
# Run server
# Use UserManager (TODO - create this next)
```

### Option 2: Manual Creation
Add to `users.json` next to server EXE:
```json
{
  "709": {
    "Username": "709",
    "Name": "Test User",
    "PasswordHash": "$2a$11$...",
    "Level": 0,
    "FailedAttempts": 0,
    "Created": "2025-01-20T00:00:00"
  }
}
```

(I'll create UserManager app next to make this easier!)

---

## 🎯 Deployment Package

### Create Complete Package:
```bash
# Build both
cd CashDrawerCS

# Server
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Package/Server

# Client  
cd ../CashDrawer.Client
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Package/Client

# Copy configs
cd ../..
xcopy CashDrawer.Server\appsettings.json Package\Server\ /Y
xcopy RunHidden.vbs Package\Server\ /Y
xcopy InstallService.bat Package\Server\ /Y
```

**Result:**
```
Package/
  ├── Server/
  │   ├── CashDrawer.Server.exe
  │   ├── appsettings.json
  │   ├── RunHidden.vbs
  │   └── InstallService.bat
  └── Client/
      └── CashDrawer.Client.exe
```

---

## 🎨 Visual Studio Build

If using Visual Studio:

1. **Open Solution:**
   - Double-click `CashDrawer.sln`

2. **Set Startup Projects:**
   - Right-click solution
   - Properties → Startup Project
   - Choose "Multiple startup projects"
   - Set Server and Client to "Start"

3. **Build:**
   - Build → Build Solution (Ctrl+Shift+B)

4. **Run:**
   - Press F5
   - Both server and client start

---

## ⚠️ Important Notes

### Client EXE Location:
The client EXE path depends on your build type:

**Debug:**
```
CashDrawer.Client/bin/Debug/net8.0-windows/CashDrawer.Client.exe
```

**Release:**
```
CashDrawer.Client/bin/Release/net8.0-windows/CashDrawer.Client.exe
```

**Published (win-x64):**
```
CashDrawer.Client/bin/Release/net8.0-windows/win-x64/publish/CashDrawer.Client.exe
```

### Look in the Right Folder:
- Not `win-x86` (32-bit)
- Use `win-x64` (64-bit) ✅

---

## 🚀 Ready to Test!

1. **Build server and client** ✅
2. **Run server** (as admin for COM port)
3. **Run client**
4. **Client auto-discovers server**
5. **Test opening drawer**

Once this works, tell me what features you want to add! 🎯

**Next: UserManager app for easy user creation!**
