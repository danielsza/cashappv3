# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
