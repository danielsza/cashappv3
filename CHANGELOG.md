# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.11.8] - 2026-07-29

### Added
- **A failed relay no longer costs the sale** - Both servers are wired to the same
  till, so if one server's relay won't fire the client now opens the drawer through
  the other server's relay instead of refusing the transaction and making the
  cashier key it in again. Applies to sales and to the BOD/EOD count as well. The
  client stays on the working relay for the rest of the session so the next sale
  doesn't repeat the detour; this is not persisted, so the next launch returns to
  the configured primary and a relay fixed overnight is used again.

  This is safe because a relay failure records nothing: the server returns before
  logging, so the transaction is written only by the relay that actually fired.
  The original request - including its idempotency key - is reused, so in the case
  where the first server did open and log but its reply was lost, the second server
  collapses onto that same row rather than adding another.

  Failover is driven by a new machine-readable `ErrorCode` on the response, not by
  matching message text, and it triggers **only** on a relay failure. Any other
  failure (a rejected password, say) is returned as-is, and a response with no
  error code - which is what a pre-3.11.8 server sends - is never retried
  elsewhere. **Needs the servers on 3.11.8** to emit the code.

### Fixed
- **The relay now retries before giving up** - `OpenDrawer` caught the fault,
  logged it and returned, so a single transient COM port error refused the sale
  outright. The port is now closed and reopened and the pulse retried once, which
  clears the "semaphore timeout period has expired" fault these USB serial adapters
  produce.

## [3.11.7] - 2026-07-28

### Added
- **The client now records crashes** - There was no global exception handling at
  all: an unhandled exception on the UI thread killed the client instantly and
  wrote nothing anywhere, so "it crashed" left nothing to diagnose. Faults are now
  written to `%LOCALAPPDATA%\CashDrawer\Logs\client-errors-<date>.log` with the
  version, machine, user and full stack trace, covering the UI thread, background
  threads and unobserved tasks. A UI-thread fault no longer takes the till down -
  the user gets a dialog naming the log file and warning them to check the drawer
  and the transaction log before repeating the action.

### Fixed
- **The connection monitor could pull the socket out from under a transaction** -
  It runs every 15 seconds on the UI thread, and WinForms pumps timers during an
  `await` and inside modal dialogs, so it could fire while a drawer request was
  still on the wire. Because it reconnected by disposing the `NetworkClient`, the
  pending read failed; the caller cannot distinguish that from a server that never
  answered, so it resent the transaction - which landed a second time, typically on
  the other server. The drawer relay stalling (the server logs COM port semaphore
  timeouts) is what held the request open long enough for the timer to land. The
  monitor now skips a tick while a request is in flight, and a connection that is
  still mid-request is never disposed.
- **Two requests could share one connection** - The transaction path and the
  background notification poll used the same `NetworkClient` with no
  serialization, so both could sit on the socket at once and read each other's
  replies. Requests are now serialized, and the poll skips its tick rather than
  queueing behind a transaction.

  Together these remove the *cause* of the duplicate transactions that 3.11.5 made
  harmless after the fact.

## [3.11.6] - 2026-07-28

### Fixed
- **The drawer now opens once, at the right time, for both BOD and EOD** - At EOD the
  drawer did not open until the count was submitted, so there was nothing to count
  when the count screen appeared: login ran `authenticate`, which only checks
  credentials and never touches the relay. EOD now opens the drawer straight after
  login, the way BOD already did.
- **BOD no longer opens the drawer twice** - BOD opened it for the count and then
  again when the float was recorded. Recording a BOD or EOD count now carries
  `SkipDrawerOpen`, so the drawer is opened exactly once per operation - up front,
  where the cash is actually handled.

  EOD login still verifies username *and* password before opening; the open request
  that follows authenticates by password alone, so it was added after that check
  rather than replacing it. Pre-3.11.6 servers ignore the flag and behave as they do
  today, so **the single-open behaviour needs the servers on 3.11.6**.

## [3.11.5] - 2026-07-28

### Fixed
- **Duplicate transactions from a re-clicked Open Drawer** - The Open Drawer handler
  was `async void` with the button left enabled (and Enter in the IN field triggers
  it), so a cashier who clicked again because the server was slow started a second
  submission. The amounts were captured before the password prompt but the document
  number was read from the textbox *after* it, and the first transaction's cleanup
  ran `ClearForm()` inside the password dialog's nested message loop - so the second
  submission was logged with the same amounts and a **blank invoice number**. The
  handler now refuses re-entry and disables the button while a submission is in
  flight, and the document number is snapshotted with the amounts and used for the
  request, the receipt and the safe-drop record. Four transactions were double
  counted this way between January and July 2026.
- **Duplicate transactions from an automatic retry** - `SendWithFailoverAsync`
  resends a request when the response is lost, but the server had already opened the
  drawer and written the log line, and generated the transaction ID itself, so the
  resend was recorded as a second transaction. The client now mints an idempotency
  key per submission (`ClientTransactionId`) which travels with every retry, and the
  server adopts it as the transaction ID so the existing duplicate check refuses the
  second write. Because the key is identical on both servers, this also covers a
  retry that fails over to the peer - previously that produced two rows with
  different IDs that peer sync then happily kept.

  Keys are accepted only in a restricted character set and length, and fall back to
  server-side ID generation otherwise, so pre-3.11.5 clients are unaffected. **The
  retry fix requires the servers to be on 3.11.5**; the re-click fix is client-side
  and takes effect as soon as the client updates.

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
