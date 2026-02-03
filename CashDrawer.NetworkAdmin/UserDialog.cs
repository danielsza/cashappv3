using System;
using System.Drawing;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public class UserDialog : Form
    {
        private TextBox _usernameText = null!;
        private TextBox _nameText = null!;
        private TextBox _passwordText = null!;
        private TextBox _confirmPasswordText = null!;
        private ComboBox _roleCombo = null!;
        private readonly User? _existingUser;
        private readonly bool _isFirstUser;

        public string Username => _usernameText.Text.Trim();
        public string UserName => _nameText.Text.Trim();
        public string Password => _passwordText.Text;
        public string UserRole => _roleCombo.SelectedItem?.ToString() ?? "User";

        public UserDialog(User? existingUser, bool isFirstUser = false)
        {
            _existingUser = existingUser;
            _isFirstUser = isFirstUser;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = _existingUser == null ? (_isFirstUser ? "Create Admin User" : "Add User") : "Edit User";
            this.Size = new Size(400, _existingUser == null ? 400 : 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            if (_isFirstUser)
            {
                var infoLabel = new Label 
                { 
                    Text = "⚠️ Creating first admin user for server",
                    Location = new Point(20, y),
                    Size = new Size(340, 25),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.DarkOrange
                };
                this.Controls.Add(infoLabel);
                y += 35;
            }

            var usernameLabel = new Label { Text = "Username:", Location = new Point(20, y), Size = new Size(100, 25) };
            this.Controls.Add(usernameLabel);

            _usernameText = new TextBox { Location = new Point(130, y), Width = 230, Enabled = _existingUser == null };
            if (_existingUser != null) _usernameText.Text = _existingUser.Username;
            this.Controls.Add(_usernameText);
            y += 40;

            var nameLabel = new Label { Text = "Full Name:", Location = new Point(20, y), Size = new Size(100, 25) };
            this.Controls.Add(nameLabel);

            _nameText = new TextBox { Location = new Point(130, y), Width = 230 };
            if (_existingUser != null) _nameText.Text = _existingUser.Name;
            this.Controls.Add(_nameText);
            y += 40;

            if (_existingUser == null)
            {
                var passwordLabel = new Label { Text = "Password:", Location = new Point(20, y), Size = new Size(100, 25) };
                this.Controls.Add(passwordLabel);

                _passwordText = new TextBox { Location = new Point(130, y), Width = 230, UseSystemPasswordChar = true };
                this.Controls.Add(_passwordText);
                y += 40;

                var confirmLabel = new Label { Text = "Confirm:", Location = new Point(20, y), Size = new Size(100, 25) };
                this.Controls.Add(confirmLabel);

                _confirmPasswordText = new TextBox { Location = new Point(130, y), Width = 230, UseSystemPasswordChar = true };
                this.Controls.Add(_confirmPasswordText);
                y += 40;
            }
            else
            {
                _passwordText = new TextBox { Text = "" };
                _confirmPasswordText = new TextBox { Text = "" };
            }

            var roleLabel = new Label { Text = "Role:", Location = new Point(20, y), Size = new Size(100, 25) };
            this.Controls.Add(roleLabel);

            _roleCombo = new ComboBox { Location = new Point(130, y), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _roleCombo.Items.AddRange(new object[] { "User", "Admin" });
            
            if (_isFirstUser)
            {
                // First user MUST be Admin
                _roleCombo.SelectedIndex = 1; // Admin
                _roleCombo.Enabled = false;
            }
            else
            {
                _roleCombo.SelectedIndex = _existingUser?.Role == "Admin" ? 1 : 0;
            }
            this.Controls.Add(_roleCombo);
            y += 60;

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(200, y),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, y),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Username is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                MessageBox.Show("Full Name is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (_existingUser == null)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Password is required for new users", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (Password != _confirmPasswordText.Text)
                {
                    MessageBox.Show("Passwords do not match", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    _passwordText.Clear();
                    _confirmPasswordText.Clear();
                    _passwordText.Focus();
                    return;
                }

                if (Password.Length < 4)
                {
                    MessageBox.Show("Password must be at least 4 characters", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }
        }
    }
}
