using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CashDrawer.Shared.Models;

namespace CashDrawer.NetworkAdmin
{
    public partial class MainForm
    {
        private PictureBox _logoPreview = null!;
        private Button _uploadLogoButton = null!;
        private Button _removeLogoButton = null!;
        private Label _logoStatusLabel = null!;

        private void CreatePrintingConfigSection(Panel parent, ref int y)
        {
            // Section title
            var sectionLabel = new Label
            {
                Text = "📄 Printing Configuration",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            parent.Controls.Add(sectionLabel);
            y += 40;

            // Company logo
            var logoLabel = CreateLabel("Company Logo:", 20, y);
            parent.Controls.Add(logoLabel);

            _logoPreview = new PictureBox
            {
                Location = new Point(200, y - 3),
                Size = new Size(150, 150),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            parent.Controls.Add(_logoPreview);

            _uploadLogoButton = new Button
            {
                Text = "📁 Upload Logo",
                Location = new Point(360, y),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _uploadLogoButton.Click += UploadLogoButton_Click;
            parent.Controls.Add(_uploadLogoButton);

            _removeLogoButton = new Button
            {
                Text = "🗑️ Remove",
                Location = new Point(360, y + 45),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Enabled = false
            };
            _removeLogoButton.Click += RemoveLogoButton_Click;
            parent.Controls.Add(_removeLogoButton);

            _logoStatusLabel = new Label
            {
                Text = "No logo configured",
                Location = new Point(200, y + 155),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            parent.Controls.Add(_logoStatusLabel);
            y += 195;

            // Printer settings
            var printerLabel = CreateLabel("Default Printer:", 20, y);
            parent.Controls.Add(printerLabel);

            var printerCombo = new ComboBox
            {
                Location = new Point(200, y - 3),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // Add available printers
            printerCombo.Items.Add("(Use System Default)");
            try
            {
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    printerCombo.Items.Add(printer);
                }
            }
            catch
            {
                // If can't enumerate, just show default option
            }
            printerCombo.SelectedIndex = 0;
            parent.Controls.Add(printerCombo);
            y += 40;

            // Auto-print option
            var autoPrintCheck = new CheckBox
            {
                Text = "Automatically print receipts (no popup)",
                Location = new Point(20, y),
                Size = new Size(400, 25),
                Checked = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            parent.Controls.Add(autoPrintCheck);
            y += 50;
        }

        private void UploadLogoButton_Click(object? sender, EventArgs e)
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Select Company Logo",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                FilterIndex = 1
            };

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Load and validate image
                    using var img = Image.FromFile(openDialog.FileName);
                    
                    // Show preview
                    _logoPreview.Image?.Dispose();
                    _logoPreview.Image = new Bitmap(img);

                    // Upload to server
                    UploadLogoToServer(openDialog.FileName);

                    _removeLogoButton.Enabled = true;
                    _logoStatusLabel.Text = $"✅ Logo: {Path.GetFileName(openDialog.FileName)}";
                    _logoStatusLabel.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to load logo:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void RemoveLogoButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Remove company logo?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _logoPreview.Image?.Dispose();
                _logoPreview.Image = null;
                _removeLogoButton.Enabled = false;
                _logoStatusLabel.Text = "No logo configured";
                _logoStatusLabel.ForeColor = Color.Gray;

                // Remove from server
                RemoveLogoFromServer();
            }
        }

        private async void UploadLogoToServer(string filePath)
        {
            if (_selectedServer == null) return;

            try
            {
                // Read file as base64
                var fileBytes = File.ReadAllBytes(filePath);
                var base64 = Convert.ToBase64String(fileBytes);
                var fileName = Path.GetFileName(filePath);

                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "upload_logo",
                    Data = new
                    {
                        FileName = fileName,
                        FileData = base64
                    }
                });

                if (response?.Status != "success")
                {
                    MessageBox.Show(
                        "Failed to upload logo to server",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error uploading logo:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void RemoveLogoFromServer()
        {
            if (_selectedServer == null) return;

            try
            {
                await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "remove_logo"
                });
            }
            catch
            {
                // Silent fail
            }
        }

        private async void LoadLogoFromServer()
        {
            if (_selectedServer == null) return;

            try
            {
                var response = await SendCommandAsync(_selectedServer, new ServerRequest
                {
                    Command = "get_logo"
                });

                if (response?.Status == "success" && response.Data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(response.Data);
                    var logoData = System.Text.Json.JsonSerializer.Deserialize<LogoData>(json);

                    if (logoData != null && !string.IsNullOrWhiteSpace(logoData.FileData))
                    {
                        var imageBytes = Convert.FromBase64String(logoData.FileData);
                        using var ms = new MemoryStream(imageBytes);
                        
                        _logoPreview.Image?.Dispose();
                        _logoPreview.Image = Image.FromStream(ms);
                        
                        _removeLogoButton.Enabled = true;
                        _logoStatusLabel.Text = $"✅ Logo: {logoData.FileName}";
                        _logoStatusLabel.ForeColor = Color.Green;
                    }
                }
            }
            catch
            {
                // No logo configured, that's ok
            }
        }

        private class LogoData
        {
            public string FileName { get; set; } = "";
            public string FileData { get; set; } = "";
        }
    }
}
