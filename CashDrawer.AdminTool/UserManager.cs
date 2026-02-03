using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CashDrawer.Shared.Models;
using BCrypt.Net;

namespace CashDrawer.AdminTool
{
    public class UserManager
    {
        private readonly string _userFilePath;
        private Dictionary<string, User> _users = new();

        public UserManager(string? serverPath = null)
        {
            // Always use ProgramData location (same as server)
            var programDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CashDrawer");
            
            if (!Directory.Exists(programDataPath))
                Directory.CreateDirectory(programDataPath);
                
            _userFilePath = Path.Combine(programDataPath, "users.json");
            LoadUsers();
        }
        
        public string UserFilePath => _userFilePath;

        private void LoadUsers()
        {
            if (File.Exists(_userFilePath))
            {
                var json = File.ReadAllText(_userFilePath);
                _users = JsonSerializer.Deserialize<Dictionary<string, User>>(json) 
                    ?? new Dictionary<string, User>();
            }
        }

        public void SaveUsers()
        {
            var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_userFilePath, json);
        }

        public Dictionary<string, User> GetUsers() => _users;

        public User? GetUser(string username) =>
            _users.TryGetValue(username.ToLower(), out var user) ? user : null;

        public void AddUser(string username, string password, string name, UserLevel level)
        {
            var key = username.ToLower();
            if (_users.ContainsKey(key))
                throw new Exception($"User '{username}' already exists");

            _users[key] = new User
            {
                Username = username,
                Name = name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Level = level,
                Created = DateTime.Now
            };
        }

        public void UpdateUser(string username, string name, UserLevel level)
        {
            var key = username.ToLower();
            if (!_users.TryGetValue(key, out var user))
                throw new Exception($"User '{username}' not found");

            user.Name = name;
            user.Level = level;
        }

        public void ChangePassword(string username, string newPassword)
        {
            var key = username.ToLower();
            if (!_users.TryGetValue(key, out var user))
                throw new Exception($"User '{username}' not found");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        public void DeleteUser(string username)
        {
            _users.Remove(username.ToLower());
        }

        public void UnlockUser(string username)
        {
            var key = username.ToLower();
            if (!_users.TryGetValue(key, out var user))
                throw new Exception($"User '{username}' not found");

            user.FailedAttempts = 0;
            user.LockedUntil = null;
        }
    }
}
