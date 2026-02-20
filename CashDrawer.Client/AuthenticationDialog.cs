using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class AuthenticationDialog : Form
    {
        private TextBox _usernameText = null!;
        private TextBox _passwordText = null!;
        
        public string Username => _usernameText.Text.Trim();
        public string Password => _passwordText.Text;
        
        public AuthenticationDialog(string title = "Authentication Required")
        {
            InitializeComponent(title);
        }
        
        private void InitializeComponent(string title)
        {
            this.Text = title;
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            
            int y = 20;
            
            // Title
            var titleLabel = new Label
            {
                Text = "🔐 Enter your credentials to continue",
                Location = new Point(20, y),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            this.Controls.Add(titleLabel);
            y += 45;
            
            // Username
            var usernameLabel = new Label
            {
                Text = "Username:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(usernameLabel);
            
            _usernameText = new TextBox
            {
                Location = new Point(130, y),
                Width = 230,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(_usernameText);
            y += 40;
            
            // Password
            var passwordLabel = new Label
            {
                Text = "Password:",
                Location = new Point(20, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(passwordLabel);
            
            _passwordText = new TextBox
            {
                Location = new Point(130, y),
                Width = 230,
                Font = new Font("Segoe UI", 10),
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_passwordText);
            y += 50;
            
            // Buttons
            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(190, y),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(0, 102, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, y),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(cancelButton);
            
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_usernameText.Text))
            {
                MessageBox.Show(
                    "Please enter a username.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _usernameText.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_passwordText.Text))
            {
                MessageBox.Show(
                    "Please enter a password.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _passwordText.Focus();
                return;
            }
            
            this.DialogResult = DialogResult.OK;
        }
    }
}
