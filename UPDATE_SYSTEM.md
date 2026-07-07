# Auto-Update System (v3.11.0+)

The client checks for updates on startup and, if a newer version is published,
downloads and launches the installer, then exits so files can be replaced.

## How it works

1. On startup the client reads a JSON **manifest** (default URL below).
2. It compares the manifest `version` against its own assembly version.
3. If the manifest is newer, it prompts the user (or installs automatically when
   `mandatory: true`), downloads the installer from `url`, verifies the optional
   `sha256`, launches it, and calls `Application.Exit()`.
4. Any failure (offline, bad manifest, download error) is silently ignored so the
   cashier is never blocked.

## Manifest format (`update/version.json`)

```json
{
  "version": "3.11.0",
  "url": "https://github.com/danielsza/cashappv3/releases/download/v3.11.0/CashDrawerSetup.exe",
  "notes": "What changed in this release.",
  "mandatory": false,
  "sha256": ""
}
```

- `version` — SemVer. A leading `v` is tolerated.
- `url` — direct download of the installer (`.exe` or `.msi`). `.msi` is launched
  via `msiexec /i`.
- `mandatory` — when true, the update installs without a Yes/No prompt.
- `sha256` — optional hex digest; when present it must match or install aborts.

## Default manifest URL

```
https://raw.githubusercontent.com/danielsza/cashappv3/main/update/version.json
```

Override per site in `client_settings.json`:

```json
{ "UpdateManifestUrl": "https://yourhost.example.com/cashdrawer/version.json" }
```

(You can host the manifest + installer on GreenGeeks instead of GitHub if you
prefer — just point `UpdateManifestUrl` at it.)

## Publishing a new release

1. Bump `<Version>` in `CashDrawer.Client.csproj` (and Server if relevant).
2. Build the installer (see `HOW_TO_BUILD_EXE.md` / `Installer/`).
3. Create a GitHub release tagged `vX.Y.Z` and upload the installer as
   `CashDrawerSetup.exe` (matching the `url` you put in the manifest).
4. (Optional) Compute the installer hash and paste it into `sha256`:
   `certutil -hashfile CashDrawerSetup.exe SHA256`
5. Update `update/version.json` with the new `version`, `url`, `notes`, `sha256`
   and push to `main` (or upload to your web host).

Clients on the previous version will pick it up on their next launch.
