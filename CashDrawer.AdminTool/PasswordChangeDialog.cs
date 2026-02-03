// Cash Drawer Management System
// Copyright (c) 2026 Daniel Szajkowski. All rights reserved.
// Contact: dszajkowski@johnbear.com | 905-575-9400 ext. 236

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.AdminTool
{
    public class PasswordChangeDialog : Form
    {
        private TextBox _newPasswordText = null!;
        private TextBox _confirmPasswordText = null!;

        public string NewPassword => _newPasswordText.Text;

        public PasswordChangeDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Change Password";
            this.Size = new Size(400, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            // New Password
            var newLabel = new Label
            {
                Text = "New Password:",
                Location = new Point(20, y),
                AutoSize = true
            };
            _newPasswordText = new TextBox
            {
                Location = new Point(150, y - 3),
                Width = 200,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(newLabel);
            this.Controls.Add(_newPasswordText);
            y += 40;

            // Confirm Password
            var confirmLabel = new Label
            {
                Text = "Confirm Password:",
                Location = new Point(20, y),
                AutoSize = true
            };
            _confirmPasswordText = new TextBox
            {
                Location = new Point(150, y - 3),
                Width = 200,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(confirmLabel);
            this.Controls.Add(_confirmPasswordText);
            y += 60;

            // Buttons
            var okButton = new Button
            {
                Text = "Change Password",
                Location = new Point(150, y),
                Size = new Size(130, 35),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(290, y),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _newPasswordText.Focus();
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("Password is required", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (NewPassword.Length < 4)
            {
                MessageBox.Show("Password must be at least 4 characters", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (NewPassword != _confirmPasswordText.Text)
            {
                MessageBox.Show("Passwords do not match", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }
    }
}
