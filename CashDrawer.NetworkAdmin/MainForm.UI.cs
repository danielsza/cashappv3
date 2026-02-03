using System;
using System.Drawing;
using System.Windows.Forms;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private void InitializeComponent()
        {
            this.Text = "Cash Drawer - Network Administration";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main content panel - Tabs (LEFT SIDE - FULL WIDTH)
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            CreateServerConfigTab();
            CreateUsersTab();
            CreatePettyCashConfigTab();
            CreateLogViewerTab();
            CreateEmailConfigTab();
            CreateAboutTab();
            
            // Improve server control buttons
            ImproveServerControl();

            contentPanel.Controls.Add(_tabControl);

            // Bottom panel - Save button
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            _saveButton = new Button
            {
                Text = "💾 Save All Changes",
                Location = new Point(10, 15),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Enabled = false
            };
            _saveButton.Click += async (s, e) => await SaveButton_Click();
            bottomPanel.Controls.Add(_saveButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(220, 15),
                Size = new Size(120, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += async (s, e) => await RefreshSelectedServerAsync();
            bottomPanel.Controls.Add(cancelButton);

            contentPanel.Controls.Add(bottomPanel);
            this.Controls.Add(contentPanel);

            // Right panel - Server list (VERTICAL ON RIGHT SIDE)
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var titleLabel = new Label
            {
                Text = "🖥️ Servers",
                Location = new Point(15, 15),
                Size = new Size(290, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            rightPanel.Controls.Add(titleLabel);

            _serversList = new ListBox
            {
                Location = new Point(15, 55),
                Size = new Size(290, 500),
                Font = new Font("Consolas", 9),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 70
            };
            _serversList.DrawItem += ServersList_DrawItem;
            _serversList.SelectedIndexChanged += ServersList_SelectedIndexChanged;
            rightPanel.Controls.Add(_serversList);

            _discoverButton = new Button
            {
                Text = "🔍 Discover Servers",
                Location = new Point(15, 565),
                Size = new Size(290, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _discoverButton.Click += async (s, e) => await DiscoverServersAsync();
            rightPanel.Controls.Add(_discoverButton);

            _refreshButton = new Button
            {
                Text = "🔄 Refresh Selected",
                Location = new Point(15, 615),
                Size = new Size(290, 40),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            _refreshButton.Click += async (s, e) => await RefreshSelectedServerAsync();
            rightPanel.Controls.Add(_refreshButton);

            _statusLabel = new Label
            {
                Text = "Click Discover to find servers",
                Location = new Point(15, 665),
                Size = new Size(290, 80),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.TopLeft
            };
            rightPanel.Controls.Add(_statusLabel);

            this.Controls.Add(rightPanel);
        }

        private void ServersList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _serversList.Items.Count) return;

            var server = _discoveredServers[e.Index];
            e.DrawBackground();

            var rect = e.Bounds;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var bgColor = isSelected ? Color.FromArgb(0, 120, 215) : Color.White;
            var textColor = isSelected ? Color.White : Color.Black;

            using (var brush = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(brush, rect);

            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
                e.Graphics.DrawString(server.ServerID, font, brush, rect.X + 8, rect.Y + 8);

            using (var font = new Font("Consolas", 8))
            using (var brush = new SolidBrush(isSelected ? Color.LightGray : Color.Gray))
                e.Graphics.DrawString($"{server.Host}:{server.Port}", font, brush, rect.X + 8, rect.Y + 28);

            var statusColor = server.IsConnected ? Color.FromArgb(16, 124, 16) : Color.Gray;
            var statusText = server.IsConnected ? "● Online" : "○ Offline";
            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(statusColor))
                e.Graphics.DrawString(statusText, font, brush, rect.X + 8, rect.Y + 45);

            e.DrawFocusRectangle();
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
    }
}
