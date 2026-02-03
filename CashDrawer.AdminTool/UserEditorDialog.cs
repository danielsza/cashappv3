using System;
using System.Drawing;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.AdminTool
{
    public class UserEditorDialog : Form
    {
        private TextBox _usernameText = null!;
        private TextBox _nameText = null!;
        private TextBox _passwordText = null!;
        private TextBox _confirmPasswordText = null!;
        private ComboBox _levelCombo = null!;
        private Button _okButton = null!;
        private Label _passwordLabel = null!;
        private Label _confirmLabel = null!;

        public string Username => _usernameText.Text.Trim();
        public string UserName => _nameText.Text.Trim();
        public string Password => _passwordText.Text;
        public UserLevel UserLevel => (UserLevel)(_levelCombo.SelectedIndex);
        public bool ForceAdmin { get; set; }

        private readonly User? _existingUser;

        public UserEditorDialog(User? existingUser)
        {
            _existingUser = existingUser;
            InitializeComponent();

            if (_existingUser != null)
            {
                this.Text = "Edit User";
                _usernameText.Text = _existingUser.Username;
                _usernameText.Enabled = false; // Can't change username
                _nameText.Text = _existingUser.Name;
                _levelCombo.SelectedIndex = (int)_existingUser.Level;
                
                // Hide password fields when editing
                _passwordLabel.Visible = false;
                _passwordText.Visible = false;
                _confirmLabel.Visible = false;
                _confirmPasswordText.Visible = false;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Add User";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            // Username
            var usernameLabel = new Label { Text = "Username:", Location = new Point(20, y), AutoSize = true };
            _usernameText = new TextBox { Location = new Point(150, y - 3), Width = 250 };
            this.Controls.Add(usernameLabel);
            this.Controls.Add(_usernameText);
            y += 40;

            // Name
            var nameLabel = new Label { Text = "Full Name:", Location = new Point(20, y), AutoSize = true };
            _nameText = new TextBox { Location = new Point(150, y - 3), Width = 250 };
            this.Controls.Add(nameLabel);
            this.Controls.Add(_nameText);
            y += 40;

            // Password
            _passwordLabel = new Label { Text = "Password:", Location = new Point(20, y), AutoSize = true };
            _passwordText = new TextBox { Location = new Point(150, y - 3), Width = 250, UseSystemPasswordChar = true };
            this.Controls.Add(_passwordLabel);
            this.Controls.Add(_passwordText);
            y += 40;

            // Confirm Password
            _confirmLabel = new Label { Text = "Confirm Password:", Location = new Point(20, y), AutoSize = true };
            _confirmPasswordText = new TextBox { Location = new Point(150, y - 3), Width = 250, UseSystemPasswordChar = true };
            this.Controls.Add(_confirmLabel);
            this.Controls.Add(_confirmPasswordText);
            y += 40;

            // Level
            var levelLabel = new Label { Text = "Level:", Location = new Point(20, y), AutoSize = true };
            _levelCombo = new ComboBox
            {
                Location = new Point(150, y - 3),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _levelCombo.Items.Add("User");
            _levelCombo.Items.Add("Admin");
            _levelCombo.SelectedIndex = 0;
            this.Controls.Add(levelLabel);
            this.Controls.Add(_levelCombo);
            y += 60;

            // Buttons
            _okButton = new Button
            {
                Text = "Save",
                Location = new Point(220, y),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(320, y),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(_okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = _okButton;
            this.CancelButton = cancelButton;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            if (ForceAdmin)
            {
                _levelCombo.SelectedIndex = 1; // Admin
                _levelCombo.Enabled = false;
            }

            _usernameText.Focus();
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Username is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                MessageBox.Show("Full name is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (_existingUser == null) // New user
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Password is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (Password.Length < 4)
                {
                    MessageBox.Show("Password must be at least 4 characters", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (Password != _confirmPasswordText.Text)
                {
                    MessageBox.Show("Passwords do not match", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }
        }
    }
}
