using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VolumeMixerPro
{
    public static class WindowHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_SYSTEMBACKDROP_TYPE = 38
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        public enum SystemBackdropType
        {
            DWMSBT_AUTO = 0,
            DWMSBT_NONE = 1,
            DWMSBT_MAINWINDOW = 2, // Mica
            DWMSBT_TRANSIENTWINDOW = 3, // Acrylic
            DWMSBT_TABBEDWINDOW = 4 // Tabbed
        }

        public static void ApplyModernStyle(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            // Round corners
            int corner = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, (int)DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

            // System Backdrop (Win 11)
            int backdrop = (int)SystemBackdropType.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(hwnd, (int)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
    }
}
