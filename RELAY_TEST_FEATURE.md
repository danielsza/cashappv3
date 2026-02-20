# 🔌 Relay Test Button Added! ✅

## ✅ What's New

Added a **Test Relay** button to the Admin Tool's Server Configuration tab!

### Features:
- ✅ Test COM port and relay settings before saving
- ✅ Actually opens the drawer to verify it works
- ✅ Shows helpful error messages
- ✅ Detects admin rights issues
- ✅ Just like the Python version!

---

## 🎯 How to Use

### 1. Open Admin Tool:
```bash
CashDrawer.AdminTool.exe
```
*(Right-click → Run as administrator for COM port access)*

### 2. Go to Server Configuration Tab

### 3. Configure Settings:
```
COM Port:     [COM10 ▼]
Relay Type:   [DTR ▼]
Duration:     [0.5] seconds
```

### 4. Click Test Button:
```
[🔌 Test Relay / Open Drawer]
```

### 5. Confirmation Dialog:
```
┌─────────────────────────────────┐
│ Test Relay Settings:            │
│                                  │
│ COM Port: COM10                  │
│ Relay Type: DTR                  │
│ Duration: 0.5 seconds            │
│                                  │
│ This will attempt to open the   │
│ cash drawer.                     │
│                                  │
│ Continue with test?              │
│                                  │
│    [ Yes ]        [ No ]         │
└─────────────────────────────────┘
```

### 6. Results:

**Success:**
```
✓ Test successful! Drawer should have opened.

[Dialog]
Relay test successful!

If the drawer opened, your settings are correct.
If it didn't open, try:
• Different COM port
• Different relay type  
• Check physical connections
```

**Failed - Access Denied:**
```
✗ Access denied - run as administrator

[Dialog]
Access Denied!

COM ports require administrator rights.

Please:
1. Close this application
2. Right-click CashDrawer.AdminTool.exe
3. Choose 'Run as administrator'
4. Try the test again
```

**Failed - Other Error:**
```
✗ Error: The port 'COM10' does not exist.

[Dialog]
Relay test failed: The port 'COM10' does not exist.

Common issues:
• Wrong COM port selected
• COM port in use by another program
• Drawer not connected
• Need administrator rights
```

---

## 🎨 UI Layout

**Server Configuration Tab:**
```
┌────────────────────────────────────────┐
│ Server ID:    [SERVER1____________]    │
│ TCP Port:     [5000]                   │
│ COM Port:     [COM10 ▼]                │
│ Relay Type:   [DTR ▼]                  │
│ Duration:     [0.5] seconds            │
│ Log Path:     [...........] [Browse]   │
│                                         │
│ [🔌 Test Relay / Open Drawer]          │
│ Click to test if drawer opens...       │
│                                         │
│ ℹ Configuration Notes:                 │
│ • Changes to ports require restart     │
│ • DTR is recommended (confirmed)       │
│ • Test before saving!                  │
└────────────────────────────────────────┘
```

---

## 💡 Testing Workflow

### Finding the Right Settings:

```
1. Open Admin Tool (as admin)
2. Go to Server Configuration
3. Try settings:
   
   Test 1:
   COM Port: COM10
   Relay Type: DTR
   [Test] → Did drawer open?
   
   If NO, try:
   
   Test 2:
   COM Port: COM10
   Relay Type: DTR_INVERTED
   [Test] → Did drawer open?
   
   Test 3:
   COM Port: COM10
   Relay Type: RTS
   [Test] → Did drawer open?
   
   Test 4:
   COM Port: COM11
   Relay Type: DTR
   [Test] → Did drawer open?
   
4. Found working settings? Save!
5. Start server with those settings
```

---

## 🔧 Relay Types Tested

The button tests all 6 relay types:

| Type | Description |
|------|-------------|
| **DTR** | Standard DTR (HIGH→LOW) ⭐ Recommended |
| **DTR_INVERTED** | Inverted DTR (LOW→HIGH) |
| **RTS** | Standard RTS (HIGH→LOW) |
| **RTS_INVERTED** | Inverted RTS (LOW→HIGH) |
| **BYTES_ESC** | ESC p command bytes |
| **BYTES_DLE** | DLE command bytes |

---

## ⚙️ Technical Details

### What the Test Does:

```csharp
1. Opens COM port
2. Sends relay signal (based on type)
3. Waits for duration
4. Closes relay signal
5. Closes COM port
6. Reports success/failure
```

### Same Code as Server:

The test uses **identical code** to the server's relay control, so:
- ✅ If test works, server will work
- ✅ Same timing, same signals
- ✅ Accurate test

---

## 🎯 Best Practices

### Before Testing:
1. ✅ Make sure drawer is connected
2. ✅ Make sure drawer has power
3. ✅ Run Admin Tool as administrator
4. ✅ Close any other programs using COM port

### During Testing:
1. ✅ Start with COM10, DTR
2. ✅ Listen/watch for drawer to open
3. ✅ If it doesn't work, try other settings
4. ✅ Test each change before moving on

### After Finding Working Settings:
1. ✅ Write them down
2. ✅ Click "Save All Changes"
3. ✅ Start the server
4. ✅ Test with client

---

## 📊 Troubleshooting

### Drawer Doesn't Open

**Check:**
- ✅ Drawer connected to computer?
- ✅ Drawer has power?
- ✅ Running as administrator?
- ✅ Correct COM port?
- ✅ Try all 6 relay types

### "Access Denied" Error

**Solution:**
```
Right-click AdminTool → Run as administrator
```

### "Port does not exist"

**Solution:**
```
1. Open Device Manager
2. Expand "Ports (COM & LPT)"
3. Find your COM port number
4. Use that in Admin Tool
```

### "Port already in use"

**Solution:**
```
1. Close Admin Tool
2. Close Server (if running)
3. Check Task Manager for other programs
4. Reopen Admin Tool
5. Try test again
```

---

## 🎊 Complete Testing Flow

```
1. Install server
2. Open Admin Tool (as admin)
3. Point to server folder
4. Go to Server Configuration
5. Test different COM ports/relay types
6. Find working combination
7. Save settings
8. Start server
9. Test with client
10. Done! ✅
```

---

## 💾 What Gets Saved

When you find working settings and click Save:

**appsettings.json:**
```json
{
  "Server": {
    "COMPort": "COM10",
    "RelayPin": "DTR",
    "RelayDuration": 0.5
  }
}
```

Server will use these exact settings!

---

## ✅ Summary

**New Feature:**
- 🔌 Test Relay button in Admin Tool
- Tests COM port and relay settings
- Opens drawer to verify it works
- Same as Python version's test functionality
- Helpful error messages
- Admin rights detection

**Benefits:**
- ✅ Find correct settings quickly
- ✅ No guessing
- ✅ Test before deploying
- ✅ Saves time
- ✅ Prevents issues

**Ready to use!** Build and test it! 🚀
