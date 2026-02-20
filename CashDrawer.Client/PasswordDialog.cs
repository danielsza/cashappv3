using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    public class PasswordDialog : Form
    {
        private TextBox _passwordTextBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public string Password => _passwordTextBox.Text;

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Enter Password";
            this.Size = new Size(350, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = "Enter your password to open drawer:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            _passwordTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Width = 290,
                UseSystemPasswordChar = true
            };
            _passwordTextBox.KeyPress += PasswordTextBox_KeyPress;

            _okButton = new Button
            {
                Text = "OK",
                Location = new Point(130, 80),
                Size = new Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OkButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(225, 80),
                Size = new Size(85, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] {
                label,
                _passwordTextBox,
                _okButton,
                _cancelButton
            });

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void PasswordTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                _okButton.PerformClick();
            }
            else if (e.KeyChar == (char)Keys.Escape)
            {
                e.Handled = true;
                _cancelButton.PerformClick();
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
            {
                MessageBox.Show("Please enter a password", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _passwordTextBox.Focus();
                this.DialogResult = DialogResult.None;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _passwordTextBox.Focus();
        }
    }
}
