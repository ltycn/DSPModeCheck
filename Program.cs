using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DSPModeCheck
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            IntPtr consoleWindow = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(consoleWindow, NativeMethods.SW_HIDE);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new TransparentForm();
            Application.Run(form);
        }
    }

    public class TransparentForm : Form
    {
        private Label label;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;

        private string AppName = "DSPModeCheck";

        public TransparentForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;

            int taskbarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
            this.Size = new Size(220, taskbarHeight);
            this.Location = new Point(0, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            this.TopMost = true;
            this.ShowInTaskbar = false;

            label = new Label
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Red,
                BackColor = Color.Transparent
            };

            Controls.Add(label);

            var timer = new Timer { Interval = 1000 };
            timer.Tick += Timer_Tick;
            timer.Start();

            contextMenu = new ContextMenuStrip();

            ToolStripMenuItem startupMenuItem = new ToolStripMenuItem("Start with Windows");
            startupMenuItem.Checked = IsStartupEnabled(); // 设置菜单项的初始状态
            startupMenuItem.Click += StartupMenuItem_Click;
            contextMenu.Items.Add(startupMenuItem);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = appIcon;
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;
            notifyIcon.Text = "Dispatcher Mode Checker";
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }
        private bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                return key.GetValue(AppName) != null;
            }
        }
        private void StartupMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem startupMenuItem = sender as ToolStripMenuItem;
            if (startupMenuItem != null)
            {
                if (startupMenuItem.Checked)
                {
                    RemoveFromStartup();
                    startupMenuItem.Checked = false;
                }
                else
                {
                    AddToStartup();
                    startupMenuItem.Checked = true;
                }
            }
        }

        private void AddToStartup()
        {

            CopyToPath();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(AppName, Path.Combine("C:\\", Path.GetFileName(Application.ExecutablePath)));
            }
        }
        private void RemoveFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue(AppName, false);
            }
        }

        
        private void CopyToPath()
        {
            string sourceFilePath = Application.ExecutablePath;
            string destinationFilePath = @"C:\"; // 目标路径为C:\

            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c copy /Y \"{sourceFilePath}\" \"{destinationFilePath}\"",
                    Verb = "runas", // 请求提升权限
                    UseShellExecute = true
                };

                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying application: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            string firstLine = "DISPATCHER";
            string secondLine = "Not Support";

            using (RegistryKey itsKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LenovoProcessManagement\Performance\PowerSlider"))
            {
                if (itsKey != null)
                {
                    object currentSetting = itsKey.GetValue("ITS_CurrentSetting");
                    object automaticModeSetting = itsKey.GetValue("ITS_AutomaticModeSetting");

                    if (currentSetting != null)
                    {
                        switch ((int)currentSetting)
                        {
                            case 0:
                                firstLine = "Intelligent Mode";
                                break;
                            case 1:
                                firstLine = "Battery Saving Mode";
                                break;
                            case 3:
                                firstLine = "Extreme Performance Mode";
                                break;
                        }
                    }

                    if (automaticModeSetting != null)
                    {
                        switch ((int)automaticModeSetting)
                        {
                            case 1:
                                secondLine = "BSM";
                                break;
                            case 3:
                                secondLine = "AQM";
                                break;
                            case 4:
                                secondLine = "STD";
                                break;
                            case 5:
                                secondLine = "APM";
                                break;
                            case 6:
                                secondLine = "i-EPM";
                                break;
                            case 7:
                                secondLine = "EPM";
                                break;
                        }
                    }
                }
            }

            label.Text = $"{firstLine}\r\n{secondLine}";

        }
    }

    

    internal static class NativeMethods
    {
        internal const int SW_HIDE = 0;
        internal const int SW_SHOW = 5;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
