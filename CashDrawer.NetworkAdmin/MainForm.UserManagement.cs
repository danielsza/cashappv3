using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private async Task AddUserButton_Click()
        {
            if (_selectedServer == null) return;

            using var dialog = new UserDialog(null);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest 
                    { 
                        Command = "add_user",
                        Password = dialog.Password,
                        Data = new User 
                        { 
                            Username = dialog.Username,
                            Name = dialog.UserName,
                            Role = dialog.UserRole 
                        }
                    });

                    if (response?.Status == "success")
                    {
                        await RefreshSelectedServerAsync();
                        MessageBox.Show($"✅ User '{dialog.Username}' added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Failed to add user:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Error adding user:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task EditUserButton_Click()
        {
            if (_selectedServer?.Users == null || _usersList.SelectedIndex < 0) return;

            // Get username from the selected list item (format: "username - role")
            var selectedItem = _usersList.Items[_usersList.SelectedIndex].ToString();
            var username = selectedItem?.Split(new[] { " - " }, StringSplitOptions.None)[0];
            
            if (string.IsNullOrEmpty(username)) return;
            
            // Find user by username (not by index, since list is sorted but Users isn't)
            var user = _selectedServer.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return;
            
            using var dialog = new UserDialog(user);
            
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    user.Role = dialog.UserRole;
                    
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "update_user", Data = user });

                    if (response?.Status == "success")
                    {
                        await RefreshSelectedServerAsync();
                        MessageBox.Show($"✅ User '{user.Username}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Failed to update user:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Error updating user:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task DeleteUserButton_Click()
        {
            if (_selectedServer?.Users == null || _usersList.SelectedIndex < 0) return;

            // Get username from the selected list item (format: "username - role")
            var selectedItem = _usersList.Items[_usersList.SelectedIndex].ToString();
            var username = selectedItem?.Split(new[] { " - " }, StringSplitOptions.None)[0];
            
            if (string.IsNullOrEmpty(username)) return;
            
            // Find user by username (not by index, since list is sorted but Users isn't)
            var user = _selectedServer.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return;
            
            var result = MessageBox.Show($"Are you sure you want to delete user '{user.Username}'?\n\nThis action cannot be undone.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "delete_user", Username = user.Username });

                    if (response?.Status == "success")
                    {
                        await RefreshSelectedServerAsync();
                        MessageBox.Show($"✅ User '{user.Username}' deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Failed to delete user:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Error deleting user:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task UnlockUserButton_Click()
        {
            if (_selectedServer?.Users == null || _usersList.SelectedIndex < 0) return;

            // Get username from the selected list item (format: "username - role")
            var selectedItem = _usersList.Items[_usersList.SelectedIndex].ToString();
            var username = selectedItem?.Split(new[] { " - " }, StringSplitOptions.None)[0];
            
            if (string.IsNullOrEmpty(username)) return;
            
            // Find user by username (not by index, since list is sorted but Users isn't)
            var user = _selectedServer.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || !user.IsLocked) return;

            try
            {
                user.LockedUntil = null;
                user.FailedAttempts = 0;
                
                var response = await SendCommandAsync(_selectedServer, new ServerRequest { Command = "update_user", Data = user });

                if (response?.Status == "success")
                {
                    await RefreshSelectedServerAsync();
                    MessageBox.Show($"✅ User '{user.Username}' unlocked successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Failed to unlock user:\n{response?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error unlocking user:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ResetPasswordButton_Click()
        {
            if (_selectedServer == null || _usersList.SelectedIndex < 0) return;

            // Get username from the selected list item (format: "username - role")
            var selectedItem = _usersList.Items[_usersList.SelectedIndex].ToString();
            var username = selectedItem?.Split(new[] { " - " }, StringSplitOptions.None)[0];
            
            if (string.IsNullOrEmpty(username)) return;
            
            // Find user by username (not by index, since list is sorted but Users isn't)
            var user = _selectedServer.Users?.FirstOrDefault(u => u.Username == username);
            if (user == null) return;

            using var dialog = new ResetPasswordDialog(user.Username);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var response = await SendCommandAsync(_selectedServer, new ServerRequest
                    {
                        Command = "reset_password",
                        Username = user.Username,
                        Password = dialog.Password
                    });

                    if (response?.Status == "success")
                    {
                        MessageBox.Show(
                            $"✅ Password reset successfully for '{user.Username}'!",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"❌ Failed to reset password:\n{response?.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"❌ Error resetting password:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
