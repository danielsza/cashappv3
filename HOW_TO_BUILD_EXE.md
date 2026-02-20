# How to Build the EXE

## 🎯 Two Options

### Option 1: Framework-Dependent (RECOMMENDED)
- **Size:** ~300 KB (tiny!)
- **Requirement:** .NET 8.0 Runtime on target PC
- **Best for:** Internal use where .NET can be installed once

### Option 2: Self-Contained (STANDALONE)
- **Size:** ~50 MB
- **Requirement:** Nothing! Runs anywhere
- **Best for:** Distribution, no installation needed

---

## 🚀 Option 1: Framework-Dependent (Small & Fast)

### Build Server:
```bash
cd CashDrawerCS/CashDrawer.Server
dotnet publish -c Release -o ../../Published/Server
```

**Output:**
```
Published/Server/
  ├── CashDrawer.Server.exe          ← Main EXE (~300 KB)
  ├── CashDrawer.Shared.dll
  ├── appsettings.json
  └── [dependencies]
```

### Build Client (when ready):
```bash
cd CashDrawerCS/CashDrawer.Client
dotnet publish -c Release -o ../../Published/Client
```

### Requirements:
Target PC needs .NET 8.0 Runtime:
- Download: https://dotnet.microsoft.com/download/dotnet/8.0
- Click: "Download .NET Runtime" (not SDK)
- Install once, works forever

---

## 🎯 Option 2: Self-Contained (Standalone EXE)

### Single File EXE (Recommended):
```bash
cd CashDrawerCS/CashDrawer.Server

dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  -o ../../Published/Standalone
```

**Output:**
```
Published/Standalone/
  └── CashDrawer.Server.exe    ← Single EXE (~50 MB), runs anywhere!
```

### Smaller Self-Contained (Multiple Files):
```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishTrimmed=true ^
  -o ../../Published/Trimmed
```

**Output:** ~30 MB with DLLs (trimmed unused code)

---

## 📋 Step-by-Step Guide

### Step 1: Open Command Prompt
```
Win+R → cmd → Enter
```

### Step 2: Navigate to Project
```bash
cd C:\Path\To\CashDrawerCS
```

### Step 3: Choose Your Build Type

**For Small EXE (needs .NET 8 Runtime):**
```bash
cd CashDrawer.Server
dotnet publish -c Release -o ../../Published/Server
```

**For Standalone EXE (runs anywhere):**
```bash
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ../../Published/Standalone
```

### Step 4: Find Your EXE
```
CashDrawerCS/Published/Server/CashDrawer.Server.exe
or
CashDrawerCS/Published/Standalone/CashDrawer.Server.exe
```

### Step 5: Copy & Deploy
```
Copy the Published folder to target PC
Run CashDrawer.Server.exe
Done!
```

---

## 🎨 Visual Studio Method (If You Have It)

### Method 1: Right-Click Publish
```
1. Open CashDrawer.sln in Visual Studio
2. Right-click CashDrawer.Server project
3. Click "Publish..."
4. Choose: Folder
5. Click: Publish
6. Find EXE in: bin\Release\net8.0\publish\
```

### Method 2: Build Menu
```
1. Build → Configuration Manager
2. Set to: Release
3. Build → Publish CashDrawer.Server
4. Or: Build → Build Solution (creates EXE in bin\Release\net8.0\)
```

---

## ⚙️ Advanced Build Options

### Optimize for Size:
```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:PublishTrimmed=true ^
  /p:EnableCompressionInSingleFile=true ^
  -o ../../Published/Optimized
```

### Optimize for Speed:
```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:PublishReadyToRun=true ^
  -o ../../Published/Fast
```

### Debug Build (for testing):
```bash
dotnet publish -c Debug -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  -o ../../Published/Debug
```

---

## 📊 Build Comparison

| Type | Size | Speed | Requirement | Best For |
|------|------|-------|-------------|----------|
| **Framework-Dependent** | ~300 KB | Fast | .NET 8 Runtime | Internal use |
| **Self-Contained** | ~50 MB | Fast | None | Distribution |
| **Trimmed** | ~30 MB | Fast | None | Balanced |
| **Optimized** | ~25 MB | Fast | None | Small + Fast |

---

## 🎯 Recommended Deployment

### For Your Store Locations:

**Option A: Install .NET Once**
```
1. Install .NET 8 Runtime on each PC (once)
2. Use framework-dependent builds (300 KB)
3. Easy updates (just copy new EXE)
```

**Option B: Standalone**
```
1. No installation needed
2. Use self-contained builds (50 MB)
3. Copy and run anywhere
```

**My Recommendation:** Option A (install .NET once, use small EXEs)

---

## 🔧 Troubleshooting

### "dotnet not found"
Install .NET 8.0 SDK:
- https://dotnet.microsoft.com/download/dotnet/8.0
- Choose: ".NET SDK" (includes build tools)

### "Could not find project"
Make sure you're in the right folder:
```bash
cd CashDrawerCS/CashDrawer.Server
# Then run publish command
```

### "Access denied" when building
Close any running instances:
```bash
taskkill /F /IM CashDrawer.Server.exe
# Then rebuild
```

### Build succeeds but EXE doesn't run
**For framework-dependent:**
- Install .NET 8.0 Runtime on target PC

**For self-contained:**
- Make sure you used `--self-contained true`

---

## 📦 Complete Deployment Package

### Create Installer Folder:
```bash
# Framework-dependent
dotnet publish -c Release -o Deploy/Server
xcopy appsettings.json Deploy/Server\ /Y
xcopy README.md Deploy\ /Y

# Result:
Deploy/
  ├── Server/
  │   ├── CashDrawer.Server.exe   ← 300 KB
  │   ├── appsettings.json
  │   └── *.dll
  └── README.md
```

### Or Standalone:
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o Deploy/Standalone
xcopy appsettings.json Deploy/Standalone\ /Y

# Result:
Deploy/Standalone/
  ├── CashDrawer.Server.exe   ← 50 MB, runs anywhere!
  └── appsettings.json
```

---

## 🎊 Quick Build Commands

### Copy-Paste Ready:

**Small EXE (needs .NET):**
```bash
cd CashDrawer.Server
dotnet publish -c Release -o ../../Published
```

**Standalone EXE (no dependencies):**
```bash
cd CashDrawer.Server
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true -o ../../Published
```

**Find your EXE:**
```
CashDrawerCS/Published/CashDrawer.Server.exe
```

**Test it:**
```bash
cd Published
CashDrawer.Server.exe
```

---

## ✅ Success Checklist

After building:
- [ ] EXE file exists in Published folder
- [ ] appsettings.json is copied
- [ ] Run EXE (as admin for COM port)
- [ ] Server starts without errors
- [ ] COM port initializes
- [ ] Ready to deploy!

---

## 🚀 Next: Once EXE Works

1. ✅ Test COM port opens drawer
2. ✅ Create users with UserManager
3. ✅ Deploy to all locations
4. ✅ Tell me what features to add!

**What features do you want next?** 🎯
