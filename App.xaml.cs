using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace VolumeMixerPro
{
    public partial class App : Application
    {
        private NotifyIcon _notifyIcon;
        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. CRITICAL: Admin check BEFORE anything else to prevent flashing
            if (!IsRunningAsAdmin())
            {
                RestartAsAdmin();
                return;
            }

            // 2. Single Instance check
            const string appName = "VolumeMixerPro_SingleInstance_v2";
            bool createdNew;
            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Volume Mixer Pro is already running.", "Notice");
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // 3. Tray Icon Setup
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Volume Mixer Pro";
            
            // Notification to confirm it's running
            _notifyIcon.ShowBalloonTip(2000, "Volume Mixer Pro", "Utility is active and ready.", ToolTipIcon.Info);

            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Show Mixer (Ctrl+Alt+V)", null, (s, ev) => {
                (MainWindow as MainWindow)?.ToggleMenu();
            });
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, ev) => {
                Shutdown();
            });

            _notifyIcon.DoubleClick += (s, ev) => {
                (MainWindow as MainWindow)?.ToggleMenu();
            };
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
            }
            catch { }
            Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null) _notifyIcon.Dispose();
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
            }
            base.OnExit(e);
        }
    }
}
