using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CashDrawer.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BCrypt.Net;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// Manages user accounts and authentication
    /// </summary>
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly SecurityConfig _securityConfig;
        private readonly Dictionary<string, User> _users = new();
        private readonly string _userFilePath;
        private readonly object _lock = new();

        public UserService(
            ILogger<UserService> logger,
            IOptions<SecurityConfig> securityConfig)
        {
            _logger = logger;
            _securityConfig = securityConfig.Value;
            
            // Store users.json in ProgramData (writable by Windows Service)
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CashDrawer");
            
            // Ensure directory exists
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            
            _userFilePath = Path.Combine(dataDir, "users.json");
            _logger.LogInformation($"User file path: {_userFilePath}");
            
            LoadUsers();
        }

        /// <summary>
        /// Load users from JSON file
        /// </summary>
        private void LoadUsers()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_userFilePath))
                    {
                        var json = File.ReadAllText(_userFilePath);
                        var users = JsonSerializer.Deserialize<Dictionary<string, User>>(json);
                        if (users != null)
                        {
                            _users.Clear();
                            foreach (var kvp in users)
                                _users[kvp.Key.ToLower()] = kvp.Value;
                            
                            _logger.LogInformation($"Loaded {_users.Count} users");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No user file found. Please create the first user via NetworkAdmin.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load users");
                }
            }
        }

        /// <summary>
        /// Save users to JSON file
        /// </summary>
        public void SaveUsers()
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    File.WriteAllText(_userFilePath, json);
                    _logger.LogInformation("Users saved");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save users");
                }
            }
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        public (bool success, string? message, User? user) AuthenticateUser(string username, string password)
        {
            lock (_lock)
            {
                username = username.ToLower().Trim();

                if (!_users.TryGetValue(username, out var user))
                {
                    _logger.LogWarning($"Authentication failed: Unknown user '{username}'");
                    return (false, "Invalid username or password", null);
                }

                // Check if account is locked
                if (user.IsLocked)
                {
                    _logger.LogWarning($"Authentication failed: Account '{username}' is locked");
                    return (false, "Account is locked. Please try again later.", null);
                }

                // Verify password
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    user.FailedAttempts = 0;
                    user.LockedUntil = null;
                    SaveUsers();
                    _logger.LogInformation($"User '{username}' authenticated successfully");
                    return (true, null, user);
                }
                else
                {
                    // Increment failed attempts
                    user.FailedAttempts++;
                    
                    if (user.FailedAttempts >= _securityConfig.MaxFailedAttempts)
                    {
                        user.LockedUntil = DateTime.Now.AddSeconds(_securityConfig.LockoutDurationSeconds);
                        _logger.LogWarning($"Account '{username}' locked after {user.FailedAttempts} failed attempts");
                        SaveUsers();
                        return (false, $"Account locked after {user.FailedAttempts} failed attempts", null);
                    }

                    SaveUsers();
                    _logger.LogWarning($"Authentication failed for '{username}': Invalid password (attempt {user.FailedAttempts})");
                    return (false, "Invalid username or password", null);
                }
            }
        }

        /// <summary>
        /// Find user by password (for password-only authentication)
        /// </summary>
        public User? FindUserByPassword(string password)
        {
            lock (_lock)
            {
                foreach (var user in _users.Values)
                {
                    if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                        return user;
                }
                return null;
            }
        }

        /// <summary>
        /// Add new user
        /// </summary>
        public bool AddUser(string username, string password, string name, UserLevel level)
        {
            lock (_lock)
            {
                username = username.ToLower().Trim();

                if (_users.ContainsKey(username))
                {
                    _logger.LogWarning($"Cannot add user: '{username}' already exists");
                    return false;
                }

                if (password.Length < 4)
                {
                    _logger.LogWarning("Cannot add user: Password too short");
                    return false;
                }

                var now = DateTime.Now;
                var user = new User
                {
                    Username = username,
                    Name = name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Level = level,
                    Created = now,
                    LastModified = now
                };

                _users[username] = user;
                SaveUsers();
                _logger.LogInformation($"User '{username}' created");
                return true;
            }
        }

        /// <summary>
        /// Get all users (for sync)
        /// </summary>
        public Dictionary<string, User> GetAllUsers()
        {
            lock (_lock)
            {
                return new Dictionary<string, User>(_users);
            }
        }

        /// <summary>
        /// Merge users from peer server (Dictionary overload) - two-way sync
        /// </summary>
        public (int added, int updated) MergeUsers(Dictionary<string, User> peerUsers)
        {
            return MergeUsers(peerUsers.Values.ToList());
        }

        /// <summary>
        /// Merge users from peer - two-way sync based on LastModified timestamp
        /// </summary>
        public (int added, int updated) MergeUsers(List<User> peerUsers)
        {
            lock (_lock)
            {
                int added = 0;
                int updated = 0;
                
                foreach (var peerUser in peerUsers)
                {
                    var username = peerUser.Username.ToLower();
                    
                    if (!_users.ContainsKey(username))
                    {
                        // New user - add it
                        _users[username] = peerUser;
                        added++;
                        _logger.LogInformation($"Synced new user '{username}' from peer");
                    }
                    else
                    {
                        // Existing user - check if peer's version is newer
                        var localUser = _users[username];
                        
                        if (peerUser.LastModified > localUser.LastModified)
                        {
                            // Peer's version is newer - update local
                            // Preserve local-only fields
                            peerUser.FailedAttempts = localUser.FailedAttempts;
                            peerUser.LockedUntil = localUser.LockedUntil;
                            
                            _users[username] = peerUser;
                            updated++;
                            _logger.LogInformation($"Updated user '{username}' from peer (peer modified: {peerUser.LastModified}, local: {localUser.LastModified})");
                        }
                    }
                }

                if (added > 0 || updated > 0)
                    SaveUsers();

                return (added, updated);
            }
        }

        public User? GetUser(string username)
        {
            lock (_lock)
            {
                var normalizedUsername = username.ToLower();
                return _users.TryGetValue(normalizedUsername, out var user) ? user : null;
            }
        }

        public Dictionary<string, User> GetUsers()
        {
            lock (_lock)
            {
                return new Dictionary<string, User>(_users);
            }
        }

        public bool DeleteUser(string username)
        {
            lock (_lock)
            {
                var normalizedUsername = username.ToLower();
                if (_users.Remove(normalizedUsername))
                {
                    SaveUsers();
                    _logger.LogInformation($"User '{username}' deleted");
                    return true;
                }
                return false;
            }
        }

        public bool UnlockUser(string username)
        {
            lock (_lock)
            {
                var normalizedUsername = username.ToLower();
                if (_users.TryGetValue(normalizedUsername, out var user))
                {
                    user.LockedUntil = null;
                    user.FailedAttempts = 0;
                    SaveUsers();
                    _logger.LogInformation($"User '{username}' unlocked");
                    return true;
                }
                return false;
            }
        }

        public bool HasUsersChanged(DateTime since)
        {
            try
            {
                if (!File.Exists(_userFilePath))
                    return false;

                var fileInfo = new FileInfo(_userFilePath);
                return fileInfo.LastWriteTime > since;
            }
            catch
            {
                return false;
            }
        }
    }
}
