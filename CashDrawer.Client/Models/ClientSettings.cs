using System;
using System.IO;
using System.Text.Json;

namespace CashDrawer.Client.Models
{
    public class ClientSettings
    {
        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CashDrawer",
            "client_settings.json"
        );
        
        public bool AdminModeEnabled { get; set; }
        public string AdminUsername { get; set; } = "";
        public bool ShowErrorPopups { get; set; }
        public bool ShowCriticalAlertsOnly { get; set; } = true;
        public string WindowPosition { get; set; } = "";
        public bool MinimizeToTray { get; set; } = true;
        public bool StartWithWindows { get; set; }

        public static ClientSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<ClientSettings>(json) ?? new ClientSettings();
                }
            }
            catch
            {
                // Return defaults if load fails
            }

            return new ClientSettings();
        }

        public void Save()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(SettingsFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
                // Silent fail
            }
        }
    }
}
