using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    /// <summary>
    /// Lets a signed-in user change their own password. Collects username, current
    /// password and the new password (twice). The server verifies the current
    /// password before applying the change.
    /// </summary>
    public class ChangePasswordDialog : Form
    {
        private TextBox _usernameTextBox = null!;
        private TextBox _currentTextBox = null!;
        private TextBox _newTextBox = null!;
        private TextBox _confirmTextBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public string Username => _usernameTextBox.Text.Trim();
        public string CurrentPassword => _currentTextBox.Text;
        public string NewPassword => _newTextBox.Text;

        public ChangePasswordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Change Password";
            this.Size = new Size(380, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int labelX = 20;
            int fieldX = 20;
            int width = 320;

            var usernameLabel = new Label { Text = "Username:", Location = new Point(labelX, 15), AutoSize = true };
            _usernameTextBox = new TextBox { Location = new Point(fieldX, 35), Width = width };

            var currentLabel = new Label { Text = "Current password:", Location = new Point(labelX, 65), AutoSize = true };
            _currentTextBox = new TextBox { Location = new Point(fieldX, 85), Width = width, UseSystemPasswordChar = true };

            var newLabel = new Label { Text = "New password:", Location = new Point(labelX, 115), AutoSize = true };
            _newTextBox = new TextBox { Location = new Point(fieldX, 135), Width = width, UseSystemPasswordChar = true };

            var confirmLabel = new Label { Text = "Confirm new password:", Location = new Point(labelX, 165), AutoSize = true };
            _confirmTextBox = new TextBox { Location = new Point(fieldX, 185), Width = width, UseSystemPasswordChar = true };

            _okButton = new Button
            {
                Text = "Change",
                Location = new Point(155, 220),
                Size = new Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OkButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(250, 220),
                Size = new Size(85, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                usernameLabel, _usernameTextBox,
                currentLabel, _currentTextBox,
                newLabel, _newTextBox,
                confirmLabel, _confirmTextBox,
                _okButton, _cancelButton
            });

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
            {
                Warn("Please enter your username.", _usernameTextBox);
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentTextBox.Text))
            {
                Warn("Please enter your current password.", _currentTextBox);
                return;
            }
            if (string.IsNullOrWhiteSpace(_newTextBox.Text) || _newTextBox.Text.Length < 4)
            {
                Warn("New password must be at least 4 characters.", _newTextBox);
                return;
            }
            if (_newTextBox.Text != _confirmTextBox.Text)
            {
                Warn("The new passwords do not match.", _confirmTextBox);
                return;
            }
            if (_newTextBox.Text == _currentTextBox.Text)
            {
                Warn("New password must be different from the current password.", _newTextBox);
                return;
            }
            // Valid - allow the dialog to close with DialogResult.OK.
        }

        private void Warn(string message, Control focus)
        {
            MessageBox.Show(this, message, "Change Password",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            focus.Focus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _usernameTextBox.Focus();
        }
    }
}
