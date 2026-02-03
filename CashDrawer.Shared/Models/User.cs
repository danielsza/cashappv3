using System;

namespace CashDrawer.Shared.Models
{
    /// <summary>
    /// User account model
    /// </summary>
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserLevel Level { get; set; } = UserLevel.User;
        public string AuthToken { get; set; } = string.Empty;
        public int FailedAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; } = null;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; } = null;
        public DateTime LastModified { get; set; } = DateTime.Now;  // For sync conflict resolution
        
        // Alias for Level as string (for compatibility)
        public string Role
        {
            get => Level == UserLevel.Admin ? "Admin" : "User";
            set
            {
                Level = value switch
                {
                    "Admin" => UserLevel.Admin,
                    _ => UserLevel.User
                };
            }
        }
        
        // Convenience property for checking admin status
        public bool IsAdmin
        {
            get => Level == UserLevel.Admin;
            set => Level = value ? UserLevel.Admin : UserLevel.User;
        }
        
        public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.Now;
        
        /// <summary>
        /// Sets the password hash for this user
        /// </summary>
        public void SetPassword(string passwordHash)
        {
            PasswordHash = passwordHash;
            LastModified = DateTime.Now;
        }
    }
    
    public enum UserLevel
    {
        User,
        Admin
    }
}
