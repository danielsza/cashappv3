using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.NetworkAdmin
{
    public class ResetPasswordDialog : Form
    {
        private TextBox _passwordText = null!;
        private TextBox _confirmPasswordText = null!;
        private readonly string _username;

        public string Password => _passwordText.Text;

        public ResetPasswordDialog(string username)
        {
            _username = username;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Reset Password";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            var infoLabel = new Label
            {
                Text = $"Reset password for: {_username}",
                Location = new Point(20, y),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(infoLabel);
            y += 40;

            var passwordLabel = new Label
            {
                Text = "New Password:",
                Location = new Point(20, y),
                Size = new Size(120, 25)
            };
            this.Controls.Add(passwordLabel);

            _passwordText = new TextBox
            {
                Location = new Point(150, y),
                Width = 210,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_passwordText);
            y += 40;

            var confirmLabel = new Label
            {
                Text = "Confirm:",
                Location = new Point(20, y),
                Size = new Size(120, 25)
            };
            this.Controls.Add(confirmLabel);

            _confirmPasswordText = new TextBox
            {
                Location = new Point(150, y),
                Width = 210,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_confirmPasswordText);
            y += 60;

            var okButton = new Button
            {
                Text = "Reset Password",
                Location = new Point(150, y),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, y),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show(
                    "Password is required",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (Password != _confirmPasswordText.Text)
            {
                MessageBox.Show(
                    "Passwords do not match",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                _passwordText.Clear();
                _confirmPasswordText.Clear();
                _passwordText.Focus();
                return;
            }

            if (Password.Length < 4)
            {
                MessageBox.Show(
                    "Password must be at least 4 characters",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            var result = MessageBox.Show(
                $"Reset password for user '{_username}'?\n\nThis cannot be undone.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
