# CashDrawer v3 — Dev Journal

This document captures architecture decisions, bug investigations, design rationale, and
context that doesn't belong in the changelog. Read this first when resuming work.

---

## Project Overview

**Repo:** `https://github.com/danielsza/cashappv3` (private)  
**Local path:** `C:\cashappv3\CashDrawerCS`  
**Current version:** 3.10.26  
**Locations:** Hamilton (JBNH), Hamilton South (JBSC) — John Bear GM dealership  

Multi-location cash drawer management system. A Windows service (`CashDrawer.Server`)
runs at each location and handles all business logic. A WinForms client (`CashDrawer.Client`)
connects over TCP and acts as a thin UI. A control service (`CashDrawer.ServerControl`)
handles service lifecycle management.

### Solution Projects

| Project | Purpose |
|---|---|
| `CashDrawer.Server` | Windows service, all business logic, TCP server |
| `CashDrawer.Client` | WinForms client app (thin UI) |
| `CashDrawer.ServerControl` | Service lifecycle control (start/stop/restart) |
| `CashDrawer.AdminTool` | Admin UI for user management, config |
| `CashDrawer.NetworkAdmin` | Network-level admin tool |
| `CashDrawer.Shared` | Shared models (ServerRequest, ServerResponse, etc.) |

### Key Files

| File | Purpose |
|---|---|
| `CashDrawer.Server/Services/TcpServerService.cs` | Main server logic, all command handlers |
| `CashDrawer.Client/MainForm.cs` | Main client UI, transaction entry |
| `CashDrawer.Client/EODCountForm.cs` | End-of-day count form |
| `CashDrawer.Server/Services/PeerSyncService.cs` | Multi-server sync |

---

## Architecture

### Communication
- Client → Server: TCP on port **5001**
- Server ↔ Server (sync): TCP on port **5001**, discovery via UDP broadcast on port **5003**
- Control service: TCP on port **5003**
- Protocol: JSON-serialized `ServerRequest` / `ServerResponse` objects, newline-delimited

### Data Storage (all JSON files)
All data lives under `C:\ProgramData\CashDrawer\`:

| File | Contents |
|---|---|
| `users.json` | User accounts (BCrypt hashed passwords) |
| `config.json` | Server config (server ID, drawer port, etc.) |
| `petty_cash_config.json` | Petty cash recipients and reasons |
| `bod_float.json` | BOD float by date (`{ "yyyy-MM-dd": amount }`) |
| `safe_drops.json` | Safe drop entries |
| `Logs/CashDrawer_yyyy-MM-dd.log` | Daily transaction logs |

### Log Line Format

**New format** (v3.10.20+, with TransactionId):
```
TransactionId | DateTime | ServerID | UserID | Transaction | DocType | DocNumber | Total: X | IN: X | OUT: X
parts[0]        parts[1]   parts[2]   parts[3]  parts[4]     parts[5]  parts[6]   parts[7]   parts[8]  parts[9]
```

**Old format** (pre-v3.10.20, no TransactionId):
```
DateTime | ServerID | UserID | Transaction | DocType | DocNumber | Total: X | IN: X | OUT: X
parts[0]   parts[1]   parts[2]  parts[3]     parts[4]  parts[5]   parts[6]   parts[7]  parts[8]
```

Parser detects format by `parts.Length >= 10` (new) vs `>= 9` (old).  
Both formats supported for backwards compatibility in all log-reading code.

### Transaction Types (DocType)
- `BOD` — Beginning of Day float entry
- `EOD` — End of Day summary entry
- `Invoice` — Normal cash sale
- `PettyCash` — Petty cash disbursement

---

## EOD Calculation Logic

### Expected Total
`ExpectedTotal = BODFloat + totalTransactions`

Where `totalTransactions` is the **sum of `Total:` fields** from all non-BOD/EOD log lines.
This was fixed in v3.10.26.

**Why not IN+OUT?**  
For a sale where customer overpays (e.g. $10 sale, customer pays $20, $10 change):
- `IN: 20.00`, `OUT: -10.00` → IN+OUT = $10.00 ✓ (happens to be right in this case)  
- But: `IN: 20.00`, `OUT: 10.00` (if OUT stored as positive) → IN+OUT = $30.00 ✗

The `Total:` field is always the actual transaction value, sign-agnostic, making it
the authoritative source for expected cash calculation.

**Safe drops** are NOT subtracted in `HandleGetDaySummary()` — the client's
`EODCountForm` handles that separately when building the final EOD variance report.

### Server Breakdown
`HandleGetDaySummary()` also builds a per-server breakdown (second pass through the log)
for multi-location reporting. This reads `parts[2]` (new) / `parts[1]` (old) for ServerID.

---

## Multi-Server Sync

Peer discovery: each server UDP-broadcasts every 2 minutes to port 5003.  
On discovery, a full `sync_all` is attempted via TCP.

Sync includes:
- Transaction logs (deduplication by TransactionId)
- Safe drops (deduplication by unique ID)
- BOD float (local wins if both have one for the same date)
- Users (two-way, `LastModified` conflict resolution — newest wins)
- Petty cash config (two-way, `LastModified` conflict resolution)

Buffer size for sync: **256KB** (set explicitly — default was too small for large logs).

---

## Bug History

### v3.10.26 — EOD Expected Total Double-Counting (2026-03-09)
**Symptom:** EOD expected total was wrong for transactions with change given. A $10 sale
where customer pays $20 and receives $10 change was reporting $30 expected instead of $10.

**Root cause:** `expectedTotal = bodFloat + totalIn + totalOut` was summing cash flow
fields (IN/OUT) rather than the actual transaction value.

**Fix:** Added `totalTransactions` accumulator that parses `Total:` from `parts[7]`
(new format) or `parts[6]` (old format). Changed to `expectedTotal = bodFloat + totalTransactions`.
`totalIn`/`totalOut` retained for display in EOD summary response.

**File:** `CashDrawer.Server/Services/TcpServerService.cs` → `HandleGetDaySummary()`

---

### v3.10.22 — BCrypt "Invalid Salt Version" on Password Reset (2026-01-30)
**Symptom:** After admin resets a password, user gets "invalid salt version" error on login.

**Root cause:** Password reset was storing plaintext instead of BCrypt hash.

**Fix:** Password reset path now calls `BCrypt.HashPassword()` before saving.

---

### v3.10.18 — Peer Discovery Not Working (2026-01-30)
**Symptom:** Servers not finding each other on the network.

**Root cause:** Discovery was broadcasting to wrong port (5001 instead of 5003).

**Fix:** Changed broadcast target to port 5003 (control service port).
Also added 10-second startup delay so discovery doesn't run before services initialize.

---

## Pending Items

### 1. Petty Cash Config Loading (client-side)
**Current state:** `MainForm.cs` has hardcoded recipient and reason lists.  
**Desired state:** Client calls `get_petty_cash_config` on startup and populates dropdowns dynamically.  
**Why:** Config lives in `petty_cash_config.json` on the server and syncs between locations.
Hardcoding bypasses this and means both locations need code changes to update the lists.

**Approach:**
- On client startup (or before opening petty cash dialog), send `get_petty_cash_config` request
- Populate recipient/reason dropdowns from response
- Fall back to hardcoded defaults if server unavailable

### 2. Petty Cash Detail Logging
**Current state:** Petty cash log lines show the invoice/doc number but not recipient or reason.  
**Desired state:** Log line includes e.g. `Store Supplies - Cleaning products` in the DocNumber field
(or an additional field).  
**Why:** EOD reports and audit trail are meaningless without knowing what the petty cash was for.

**Approach:**
- When logging a petty cash transaction, append `{recipient} - {reason}` to the DocNumber/description field
- Ensure this flows through to receipts and EOD breakdown

---

## Build & Deploy

```cmd
set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd
cd C:\cashappv3\CashDrawerCS
git fetch origin
git reset --hard origin/main
cd Installer
build.bat
```

Output: `Installer\Output\CashDrawerSetup.msi`

Deploy to store PCs:
```cmd
msiexec /i CashDrawerSetup.msi
```

**BEFORE EVERY COMMIT — bump version in all three places:**

| File | Field |
|---|---|
| `Installer/Product.wxs` | `<?define ProductVersion = "3.10.XX.0" ?>` |
| `CashDrawer.Client/CashDrawer.Client.csproj` | `<Version>3.10.XX</Version>` |
| `CashDrawer.Server/CashDrawer.Server.csproj` | `<Version>3.10.XX</Version>` |

Failing to bump the version means Windows Installer won't upgrade existing installs — it will silently skip file replacement.

Commit pattern (after Claude pushes code changes directly via API):
```cmd
git fetch origin
git reset --hard origin/main
cd Installer
build.bat
```

To push local changes:
```cmd
git add -A
git commit -m "v3.10.XX - Description of change"
git push --set-upstream origin main
```

Git PATH (required in each new cmd session):
```cmd
set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd
```
