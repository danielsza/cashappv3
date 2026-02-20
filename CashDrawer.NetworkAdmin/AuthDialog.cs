using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.NetworkAdmin
{
    public class AuthDialog : Form
    {
        private TextBox _usernameText = null!;
        private TextBox _passwordText = null!;

        public string Username => _usernameText.Text.Trim();
        public string Password => _passwordText.Text;

        public AuthDialog(string serverName)
        {
            InitializeComponent(serverName);
        }

        private void InitializeComponent(string serverName)
        {
            this.Text = "Authentication Required";
            this.Size = new Size(400, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var infoLabel = new Label
            {
                Text = $"Enter credentials to connect to:\n{serverName}",
                Location = new Point(20, 20),
                Size = new Size(350, 40),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(infoLabel);

            var usernameLabel = new Label
            {
                Text = "Username:",
                Location = new Point(20, 70),
                Size = new Size(100, 25)
            };
            this.Controls.Add(usernameLabel);

            _usernameText = new TextBox
            {
                Location = new Point(130, 70),
                Width = 230
            };
            this.Controls.Add(_usernameText);

            var passwordLabel = new Label
            {
                Text = "Password:",
                Location = new Point(20, 110),
                Size = new Size(100, 25)
            };
            this.Controls.Add(passwordLabel);

            _passwordText = new TextBox
            {
                Location = new Point(130, 110),
                Width = 230,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_passwordText);

            var okButton = new Button
            {
                Text = "Connect",
                Location = new Point(200, 150),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 150),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Username and password are required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }
    }
}
