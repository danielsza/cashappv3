// Cash Drawer Management System
// Copyright (c) 2026 Daniel Szajkowski. All rights reserved.
// Contact: dszajkowski@johnbear.com | 905-575-9400 ext. 236

using System;
using System.IO;
using System.Text.Json;
using CashDrawer.Shared.Models;

namespace CashDrawer.AdminTool
{
    public class ConfigManager
    {
        private readonly string _configFilePath;

        public ConfigManager(string? serverPath = null)
        {
            // Always use ProgramData location (same as server)
            var programDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CashDrawer");
            
            if (!Directory.Exists(programDataPath))
                Directory.CreateDirectory(programDataPath);
                
            _configFilePath = Path.Combine(programDataPath, "appsettings.json");
        }
        
        public string ConfigFilePath => _configFilePath;

        public ServerConfig LoadConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                // Return default config
                return new ServerConfig();
            }

            var json = File.ReadAllText(_configFilePath);
            var doc = JsonDocument.Parse(json);
            
            var serverSection = doc.RootElement.GetProperty("Server");

            return new ServerConfig
            {
                ServerID = serverSection.GetProperty("ServerID").GetString() ?? "SERVER1",
                Port = serverSection.GetProperty("Port").GetInt32(),
                DiscoveryPort = serverSection.TryGetProperty("DiscoveryPort", out var dp) ? dp.GetInt32() : 5001,
                COMPort = serverSection.GetProperty("COMPort").GetString() ?? "COM10",
                RelayPin = Enum.Parse<RelayType>(serverSection.GetProperty("RelayPin").GetString() ?? "DTR"),
                RelayDuration = serverSection.GetProperty("RelayDuration").GetDouble(),
                LogPath = serverSection.GetProperty("LogPath").GetString() ?? "",
                LocalLogPath = serverSection.GetProperty("LocalLogPath").GetString() ?? "./Logs",
                PeerServerHost = serverSection.TryGetProperty("PeerServerHost", out var psh) ? psh.GetString() : null,
                PeerServerPort = serverSection.TryGetProperty("PeerServerPort", out var psp) ? psp.GetInt32() : 5000,
                TestMode = serverSection.TryGetProperty("TestMode", out var tm) ? tm.GetBoolean() : false
            };
        }

        public void SaveConfig(ServerConfig config)
        {
            var json = $$"""
            {
              "Server": {
                "ServerID": "{{config.ServerID}}",
                "Port": {{config.Port}},
                "DiscoveryPort": {{config.DiscoveryPort}},
                "COMPort": "{{config.COMPort}}",
                "RelayPin": "{{config.RelayPin}}",
                "RelayDuration": {{config.RelayDuration}},
                "LogPath": "{{config.LogPath.Replace("\\", "\\\\")}}",
                "LocalLogPath": "{{config.LocalLogPath.Replace("\\", "\\\\")}}",
                "PeerServerHost": {{(config.PeerServerHost != null ? $"\"{config.PeerServerHost}\"" : "null")}},
                "PeerServerPort": {{config.PeerServerPort}},
                "TestMode": {{config.TestMode.ToString().ToLower()}}
              },
              "Security": {
                "MaxFailedAttempts": 3,
                "LockoutDurationSeconds": 300,
                "SessionTimeoutSeconds": 3600
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft": "Warning",
                  "Microsoft.Hosting.Lifetime": "Information"
                }
              }
            }
            """;

            File.WriteAllText(_configFilePath, json);
        }
    }
}
