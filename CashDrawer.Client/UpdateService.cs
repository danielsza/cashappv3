using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    /// <summary>
    /// Shape of the update manifest published by the maintainer.
    /// Example version.json:
    /// {
    ///   "version": "3.11.0",
    ///   "url": "https://github.com/danielsza/cashappv3/releases/download/v3.11.0/CashDrawerSetup.exe",
    ///   "notes": "Auto-failover, single-instance, password change.",
    ///   "mandatory": false,
    ///   "sha256": "ABCD... (optional, hex)"
    /// }
    /// </summary>
    public class UpdateManifest
    {
        public string Version { get; set; } = "";
        public string Url { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool Mandatory { get; set; } = false;
        public string Sha256 { get; set; } = "";
    }

    /// <summary>
    /// Full auto-update: checks a version manifest, and if a newer version is
    /// available, downloads the installer (verifying an optional SHA-256),
    /// launches it, and closes the app so the installer can replace files.
    ///
    /// A failed or unreachable update check NEVER interrupts the cashier - all
    /// failures are swallowed and the app continues normally.
    /// </summary>
    public static class UpdateService
    {
        // Default location the client looks for update metadata. Override per-site
        // via client_settings.json ("UpdateManifestUrl"). Point this at a raw file
        // in the repo or a file on your own web host (e.g. GreenGeeks).
        public const string DefaultManifestUrl =
            "https://raw.githubusercontent.com/danielsza/cashappv3/main/update/version.json";

        private static readonly HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            // GitHub's raw/redirect endpoints are happier with a User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CashDrawerClient");
            return client;
        }

        public static Version CurrentVersion =>
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

        /// <summary>
        /// Check for an update and, if the user agrees (or it's mandatory),
        /// download and launch the installer.
        /// </summary>
        /// <param name="owner">Owner window for dialogs (may be null).</param>
        /// <param name="manifestUrl">Override manifest URL, or null/empty for the default.</param>
        /// <param name="silentIfCurrent">When true, say nothing if already up to date.</param>
        public static async Task CheckAndPromptAsync(IWin32Window? owner, string? manifestUrl = null, bool silentIfCurrent = true)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(manifestUrl) ? DefaultManifestUrl : manifestUrl!;
                var json = await _http.GetStringAsync(url);

                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (manifest == null
                    || string.IsNullOrWhiteSpace(manifest.Version)
                    || string.IsNullOrWhiteSpace(manifest.Url))
                {
                    return;
                }

                if (!Version.TryParse(NormalizeVersion(manifest.Version), out var remote))
                    return;

                var current = CurrentVersion;
                if (remote <= current)
                {
                    if (!silentIfCurrent)
                    {
                        MessageBox.Show(owner, $"You are up to date (v{current}).",
                            "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                var promptText =
                    $"A new version of CashDrawer Client is available.\n\n" +
                    $"Installed:  v{current}\n" +
                    $"Available:  v{manifest.Version}\n\n" +
                    (string.IsNullOrWhiteSpace(manifest.Notes) ? "" : manifest.Notes + "\n\n") +
                    "Download and install it now?";

                if (manifest.Mandatory)
                {
                    MessageBox.Show(owner,
                        promptText.Replace("Download and install it now?",
                            "This is a required update and will be installed now."),
                        "Required Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var result = MessageBox.Show(owner, promptText, "Update Available",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (result != DialogResult.Yes)
                        return;
                }

                await DownloadAndInstallAsync(owner, manifest);
            }
            catch
            {
                // Never let an update check break the app or block the cashier.
            }
        }

        private static async Task DownloadAndInstallAsync(IWin32Window? owner, UpdateManifest manifest)
        {
            string tempPath;
            try
            {
                var ext = Path.GetExtension(new Uri(manifest.Url).AbsolutePath);
                if (string.IsNullOrEmpty(ext)) ext = ".exe";
                tempPath = Path.Combine(Path.GetTempPath(), $"CashDrawerUpdate_{manifest.Version}{ext}");

                using (var resp = await _http.GetAsync(manifest.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    resp.EnsureSuccessStatusCode();
                    using var src = await resp.Content.ReadAsStreamAsync();
                    using var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await src.CopyToAsync(dst);
                }

                // Optional integrity check.
                if (!string.IsNullOrWhiteSpace(manifest.Sha256))
                {
                    var actual = ComputeSha256(tempPath);
                    if (!actual.Equals(manifest.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(owner,
                            "The downloaded update failed its integrity check (checksum mismatch). " +
                            "Installation has been aborted.",
                            "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        TryDelete(tempPath);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Failed to download the update:\n{ex.Message}",
                    "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                ProcessStartInfo psi;
                if (Path.GetExtension(tempPath).Equals(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "msiexec",
                        Arguments = $"/i \"{tempPath}\"",
                        UseShellExecute = true
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    };
                }

                var installer = Process.Start(psi);

                // After the installer finishes, relaunch the client so the user isn't
                // left with no running app (the installer replaced the exe while we
                // were closed). A detached helper waits for the installer to exit,
                // then starts the upgraded client.
                TryScheduleRelaunch(installer);

                // Close the app so the installer can replace the running executable.
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Failed to launch the installer:\n{ex.Message}",
                    "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Spawn a detached helper that waits for the installer process to exit and
        /// then relaunches this client executable. Best-effort: any failure is ignored
        /// (worst case the user starts the app manually, as before).
        /// </summary>
        private static void TryScheduleRelaunch(Process? installer)
        {
            try
            {
                var exePath = Environment.ProcessPath; // upgraded exe lives at the same path
                if (installer == null || string.IsNullOrWhiteSpace(exePath))
                    return;

                // Escape single quotes for the PowerShell single-quoted string.
                var safePath = exePath.Replace("'", "''");
                var command =
                    $"Wait-Process -Id {installer.Id} -ErrorAction SilentlyContinue; " +
                    "Start-Sleep -Seconds 2; " +
                    $"Start-Process -FilePath '{safePath}'";

                var waiter = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{command}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(waiter);
            }
            catch
            {
                // Relaunch is a convenience, never fatal.
            }
        }

        private static string NormalizeVersion(string v)
        {
            v = v.Trim().TrimStart('v', 'V');
            // System.Version needs at least Major.Minor.
            if (!v.Contains('.')) v += ".0";
            return v;
        }

        private static string ComputeSha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(fs));
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
