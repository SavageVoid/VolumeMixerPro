using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;
namespace VolumeMixerPro
{
    public partial class App : Application
    {
        private NotifyIcon   _notifyIcon;
        private static Mutex _mutex;
        private MainWindow   _mainWindow;
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!IsAdmin())
            {
                RelaunchAsAdmin();
                return;
            }
            _mutex = new Mutex(true, "VolumeMixerPro_v3", out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Volume Mixer Pro is already running.",
                    "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            SettingsManager.Load();
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _mainWindow = new MainWindow();
            MainWindow  = _mainWindow;
            new WindowInteropHelper(_mainWindow).EnsureHandle();
            _notifyIcon = new NotifyIcon
            {
                Icon    = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text    = "Volume Mixer Pro"
            };
            _notifyIcon.ShowBalloonTip(2000, "Volume Mixer Pro",
                "Running in system tray. Press Ctrl+Alt+V to open.", ToolTipIcon.Info);
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Mixer  (Ctrl+Alt+V)", null, (s, ev) =>
                Dispatcher.Invoke(() => _mainWindow.ToggleMenu()));
            menu.Items.Add("Utility Setting", null, (s, ev) =>
                Dispatcher.Invoke(() => _mainWindow.OpenSettings(0)));
            menu.Items.Add("About", null, (s, ev) =>
                Dispatcher.Invoke(() => _mainWindow.OpenSettings(3)));
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, ev) =>
                Dispatcher.Invoke(() => Shutdown()));
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick     += (s, ev) =>
                Dispatcher.Invoke(() => _mainWindow.ToggleMenu());
        }
        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
            base.OnExit(e);
        }
        private static bool IsAdmin()
        {
            try
            {
                var id        = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
        private static void RelaunchAsAdmin()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb            = "runas"
                });
            }
            catch { }
            Current.Shutdown();
        }
    }
}
