// Cash Drawer Management System
// Copyright (c) 2026 Daniel Szajkowski. All rights reserved.
// Contact: dszajkowski@johnbear.com | 905-575-9400 ext. 236

using System;
using System.Windows.Forms;

namespace CashDrawer.AdminTool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
    }
}
