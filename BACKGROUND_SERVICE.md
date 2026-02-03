# Running as Background Service - Three Options

## Current: Console Window ❌
Right now the server runs in a console window. Let's fix that!

## 🎯 Three Ways to Run in Background

### Option 1: Windows Service (Best for Production) ⭐
- Runs as true Windows Service
- Auto-starts at boot
- No window at all
- Runs even when no one logged in

### Option 2: Hidden Console (Quick & Easy)
- Runs hidden in background
- No window shown
- Starts with user login
- Simple to setup

### Option 3: System Tray App (User-Friendly)
- Icon in system tray
- Right-click menu
- Easy start/stop
- User control

---

## 🚀 Option 1: Windows Service (Recommended)

The C# server already has Windows Service support built-in!

### Install as Windows Service:

**1. Build the EXE:**
```bash
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

**2. Install as Service:**
```bash
# Open CMD as Administrator
sc create CashDrawerServer binPath= "C:\CashDrawer\CashDrawer.Server.exe"
sc description CashDrawerServer "Cash Drawer Server - COM Port Controller"
sc config CashDrawerServer start= auto
```

**3. Start the Service:**
```bash
sc start CashDrawerServer
```

**Check Status:**
```bash
sc query CashDrawerServer
```

**Stop Service:**
```bash
sc stop CashDrawerServer
```

**Remove Service:**
```bash
sc delete CashDrawerServer
```

### Benefits:
✅ Runs at boot (before anyone logs in)
✅ No window at all
✅ Automatic restart on crash
✅ Runs with SYSTEM privileges (COM port access)
✅ Professional solution

---

## 🎯 Option 2: Hidden Background (Quick Setup)

Create a launcher that hides the window.

### Create: RunHidden.vbs
```vbscript
Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' Get script directory
ScriptDir = FSO.GetParentFolderName(WScript.ScriptFullName)

' Check if running as admin
Set objShell = CreateObject("Shell.Application")

If Not WScript.Arguments.Named.Exists("elevated") Then
    ' Re-launch with admin rights
    objShell.ShellExecute "wscript.exe", """" & WScript.ScriptFullName & """ /elevated", "", "runas", 0
    WScript.Quit
End If

' Run server completely hidden
WshShell.Run """" & ScriptDir & "\CashDrawer.Server.exe""", 0, False

' Show notification
WshShell.Popup "Cash Drawer Server started in background" & vbCrLf & vbCrLf & "Running as Administrator", 3, "Cash Server", 64
```

### Use It:
1. Save as `RunHidden.vbs` next to `CashDrawer.Server.exe`
2. Double-click `RunHidden.vbs`
3. Click "Yes" on UAC prompt
4. Server runs hidden in background!

### Auto-Start:
```
1. Create scheduled task:
   schtasks /Create /TN "CashDrawerServer" /TR "C:\CashDrawer\RunHidden.vbs" /SC ONLOGON /RL HIGHEST /F

2. Or: Put shortcut in Startup folder
   Win+R → shell:startup
   Create shortcut to RunHidden.vbs
```

---

## 🎯 Option 3: System Tray Application

Let me create a system tray wrapper application.

### Create: CashDrawer.Tray (New Project)

I'll add this as a WinForms app that:
- Shows icon in system tray
- Right-click menu: Start/Stop/Status/Exit
- Green icon = running, Red = stopped
- Starts/stops the server process
- Double-click to show status window

Would you like me to create this? It's about 100 lines of code.

---

## 📋 Comparison

| Method | Complexity | Auto-Start | No Window | Best For |
|--------|-----------|------------|-----------|----------|
| **Windows Service** | Medium | ✅ Boot | ✅ Yes | Production |
| **Hidden VBS** | Easy | ⚠️ Login | ✅ Yes | Quick setup |
| **System Tray** | Easy | ⚠️ Login | ✅ Yes | User control |

---

## 🎯 My Recommendation

**For Production: Use Windows Service**

It's already built into the C# server! Just install it:

```bash
# 1. Copy EXE to permanent location
xcopy CashDrawer.Server.exe C:\CashDrawer\ /Y
xcopy appsettings.json C:\CashDrawer\ /Y

# 2. Install service (as Administrator)
sc create CashDrawerServer binPath= "C:\CashDrawer\CashDrawer.Server.exe" start= auto
sc description CashDrawerServer "Cash Drawer Server"
sc start CashDrawerServer

# Done! Runs at boot, no window, automatic restart
```

---

## 🔧 Quick Fix Right Now

**To test hidden mode immediately:**

1. Create `RunHidden.vbs`:
```vbscript
CreateObject("WScript.Shell").Run "CashDrawer.Server.exe", 0, False
```

2. Put next to EXE
3. Double-click RunHidden.vbs
4. Server runs hidden!

---

## 📝 Which Do You Want?

1. **Windows Service** - I'll create installer scripts
2. **System Tray App** - I'll create the tray application
3. **Both** - I'll create everything

Let me know and I'll add it to the project! 🚀
