# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.11.4] - 2026-07-16

### Added
- **Transaction log on the EOD print summary** - The printed End-of-Day summary now
  includes a compact one-line-per-transaction table for the business day, with a
  count and total footer, so the printout reconciles on its own without opening the
  Admin Tool. Rows are scoped with the same `DaySummaryCalculator` logic the totals
  use, so the list always ties out to the printed Expected Total. Fetched via the
  existing `get_transaction_logs` command, so **no server update is required**; if
  the fetch fails the summary still prints, just without the log.
- **Print button on the Admin Tool log viewer** - Prints the currently displayed log
  tab exactly as filtered. Transactions render as a fixed-width table (with a date
  column); errors print verbatim. Log lines in the legacy 9-field format that can't
  be parsed are printed as stored under an "Unrecognized lines" heading rather than
  being silently dropped.

### Fixed
- **EOD summary printed the wrong date** - The date line used the format string
  `yyyy-MM-DD`; `DD` is not a valid .NET format specifier, so every printed EOD
  summary rendered an incorrect date. Now `yyyy-MM-dd`.
- **Printed reports silently clipped at one page** - Printing drew the whole report
  into the page rectangle in a single `DrawString` and never set `HasMorePages`, so
  any content past the bottom of page 1 was dropped with no error. Reports now
  paginate line-by-line and repeat column headers on continuation pages.

## [3.11.0] - 2026-07-06

### Added
- **Single-instance enforcement** - The client now uses a machine-wide mutex so
  only one instance can run per computer. A second launch surfaces the existing
  window and exits, preventing duplicate transaction logging.
- **Automatic failover + rescan** - All transactions now route through a single
  recovery path: live connection → primary → backup → fresh UDP discovery. If a
  server goes offline and returns on a **new IP address**, the client rediscovers
  it automatically and saves the new address. A 15-second background monitor
  reconnects even while idle.
- **Self-service password change** - Users can change their own password from the
  client (verifies the current password first). New server command
  `change_own_password`; changes propagate to peer servers via sync.
- **Full auto-update** - On startup the client checks an update manifest
  (`update/version.json`), and if a newer version exists, downloads the installer
  (with optional SHA-256 verification), launches it, and exits. Manifest URL is
  overridable per-site via `client_settings.json` (`UpdateManifestUrl`).

### Fixed
- **Log/tally corruption** - Free-text fields (username, invoice #, petty-cash
  reason) are now sanitized of the `|` delimiter and line breaks before logging,
  so a stray `|` can no longer shift columns and corrupt a log line.
- **Culture-safe money formatting** - Transaction amounts are written and parsed
  with `InvariantCulture`, so a machine using a comma decimal separator can't
  write `50,00` and then fail to parse it.
- **Log viewer error on large logs** - The admin, client, and server all read the
  full TCP payload now (accumulate until the JSON parses) instead of a single
  fixed-size read. Large transaction-log dumps and large requests (peer sync,
  logo uploads) were previously truncated, breaking JSON parsing and silently
  failing sync.
- **Log-viewer date filter** - Date filtering now reads the timestamp from the
  correct column for the new `TransactionId`-first log format (previously it read
  the ID and skipped filtering).
- **Connect timeout** - Client connections now time out after 3s instead of
  blocking ~20s on an unreachable host, so failover is responsive.

- **EOD "short the BOD balance" fix (forgotten EOD)** - End-of-Day now takes the
  BOD float from the BOD entry for the business day (the most recent BOD's date),
  instead of looking the float up by *today's* calendar date. Previously, if an EOD
  was done on a different day than the BOD (e.g. a forgotten EOD reconciled the next
  day), the float lookup hit a date with no BOD, returned $0, and the count came up
  short by the entire float. New reusable `DaySummaryCalculator` in Shared
  (unit-tested). The reconciliation is **per calendar day** - this shop removes the
  drawer cash nightly whether or not EOD was run, so each day stands alone and an
  earlier day's sales never bleed into a later day's total.
- **Duplicate BOD (same-day correction) safety** - If a cashier does Beginning-of-Day
  twice on the same day, it's treated as a correction: the earliest BOD anchors the
  start (no sales dropped) and the latest BOD supplies the float (the corrected
  amount). Re-running End-of-Day yields the same totals. Summary returns `BodCount`
  so a double-BOD is visible.
- **Client overnight FYI** - One-time informational message if the client is left
  open past midnight (cash totals are tracked per day).

## [3.10.26] - 2026-03-09

### Fixed
- **EOD expected total calculation** - Was double-counting cash for transactions where change was given
  - Example: $10 sale, customer pays $20, $10 change → was counting $30, now correctly counts $10
  - Fix: parse `Total:` field from log line instead of summing `IN:` + `OUT:`
  - New log format: `parts[7]` = `"Total: X"`, old format: `parts[6]` = `"Total: X"`
  - `totalIn` / `totalOut` retained for display fields in EOD summary response

## [3.10.25] - 2026-02-20

### Changed
- **Enter key navigation** - Press Enter to move to next field (instead of Tab)
  - Doc # → Total → IN → Submit
- AuthenticationDialog: Enter on username moves to password, Enter on password submits

## [3.10.24] - 2026-02-20

### Changed
- **BOD flow improved** - Drawer now opens immediately after authentication so user can place cash while counting
- **EOD flow improved** - Single authentication, no longer asks for password twice
- Added `open_drawer_only` server command for opening drawer without logging a transaction (used for BOD setup)

### Fixed
- BOD authentication now properly verifies credentials before opening drawer
- EOD authentication now properly verifies credentials before showing count form
- Both BOD and EOD now return after completion instead of falling through to normal transaction flow

## [3.10.23] - 2026-02-12

### Changed
- BOD drawer opens after authentication (was opening after transaction submit)
- Note: This version had a bug where the drawer wouldn't open - fixed in 3.10.24

## [3.10.22] - 2026-01-30

### Fixed
- Password reset now properly hashes passwords with BCrypt
- Fixed "invalid salt version" error when logging in after password reset

## [3.10.21] - 2026-01-30

### Added
- **Full multi-server sync** - All data now syncs between servers:
  - Transactions (with unique IDs for deduplication)
  - Safe drops (with unique IDs)
  - BOD float (syncs if local doesn't have one)
  - Users (two-way with LastModified conflict resolution)
  - Petty cash config (two-way with LastModified)

### Changed
- Transaction log format now includes TransactionId as first field
- Safe drops now have ServerID for tracking origin
- Increased sync buffer size to 256KB for large transaction logs

## [3.10.20] - 2026-01-30

### Added
- Transaction sync between servers
- Unique TransactionId for each transaction
- Server breakdown in EOD summary (shows per-location totals)

### Changed
- Transaction log format updated to include TransactionId
- Log parsing supports both old and new formats for backwards compatibility

## [3.10.19] - 2026-01-30

### Added
- Two-way user sync (updates existing users if peer version is newer)
- Petty cash config sync between servers
- `sync_all` command for full data sync
- LastModified timestamp on User model

### Changed
- MergeUsers now returns (added, updated) tuple
- Petty cash config includes LastModified for sync

## [3.10.18] - 2026-01-30

### Fixed
- PeerSyncService discovery now broadcasts to port 5003 (control service)
- Fixed response parsing for control service responses
- Added proper TCP timeouts for sync connections
- Increased buffer size for user sync

### Changed
- Discovery interval reduced to 2 minutes
- Added 10-second startup delay for service initialization
- Improved debug logging for peer discovery

## [3.10.17] - 2026-01-30

### Added
- Full name displayed on printouts instead of username
- "Cashier:" label instead of "User:" on receipts

### Changed
- ServerResponse now includes Name field
- Status messages show full name

## [3.10.16] - 2026-01-30

### Fixed
- BOD now correctly shows as IN (money in) not OUT
- EOD calculation fixed - proper handling of IN/OUT values
- Safe drops no longer double-subtracted in EOD
- Log parsing updated for correct column positions

### Changed
- CalculateOut skips auto-calculation for BOD/EOD transactions

## [3.10.15] - 2026-01-30

### Added
- Notification deduplication (tracks seen notification IDs)
- Security warning logs for failed authentication attempts

### Fixed
- Test notifications no longer repeat every 10 seconds

## [3.10.14] - 2026-01-30

### Changed
- Safe drop dialog enlarged (480px height)
- Amount display increased to 36pt bold
- Info panel expanded with separate warning icon
- Safe drop printout has prominent double-line header

## [3.10.12] - 2026-01-30

### Added
- Toast-style notifications (non-blocking, auto-dismiss after 8 seconds)
- ReadAllLinesShared helper for reading logs while Serilog has file open

### Fixed
- File lock errors when reading log files
- Notifications now appear even when client window not focused

## [3.10.11] - 2026-01-30

### Fixed
- EOD expected total calculation
- Safe drop tracking and display

## [3.10.10] - 2026-01-30

### Added
- Transaction receipt printing
- Safe drop receipts with confirmation status

## [3.10.9] - 2026-01-30

### Added
- Safe drop functionality with threshold warning
- Safe drop confirmation dialog

## [3.10.8] - 2026-01-30

### Changed
- Separated server logs from transaction logs
- Config path handling improvements

## [3.10.0] - 2026-01-29

### Added
- Initial multi-server architecture
- Admin authentication for remote management
- Control service for start/stop/restart
- Peer discovery via UDP broadcast
- Basic user sync between servers

## [3.0.0] - 2026-01-28

### Added
- Complete rewrite in C# (.NET 8.0)
- Windows service architecture
- WinForms client application
- BCrypt password hashing
- Serilog structured logging
- WiX installer

### Changed
- Migrated from Python to C#
- Replaced SQLite with JSON file storage
- New USB relay control via serial port
