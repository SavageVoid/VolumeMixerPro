using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace VolumeMixerPro
{
    public static class WindowHelper
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;  
        private const int DWMWA_SYSTEMBACKDROP_TYPE      = 38;  
        private const int DWMWCP_ROUND = 2;
        private const int DWMSBT_MAINWINDOW       = 2;
        private const int DWMSBT_TRANSIENTWINDOW  = 3;
        private const int WCA_ACCENT_POLICY = 19;
        private enum AccentState
        {
            ACCENT_DISABLED                   = 0,
            ACCENT_ENABLE_GRADIENT            = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND          = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND   = 4,
            ACCENT_INVALID_STATE              = 5
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int         AccentFlags;
            public int         GradientColor;   
            public int         AnimationId;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int    Attribute;
            public IntPtr Data;
            public int    SizeOfData;
        }
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd,
            ref WindowCompositionAttributeData data);
        public static void ApplyModernStyle(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            ApplyRoundedCorners(hwnd);
            ApplyBackdrop(hwnd);
        }
        public static void ApplyRoundedCorners(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            ApplyRoundedCorners(hwnd);
        }
        private static void ApplyRoundedCorners(IntPtr hwnd)
        {
        }
        private static void ApplyBackdrop(IntPtr hwnd)
        {
            if (TryMica(hwnd)) return;
            TryAcrylicWin10(hwnd);
        }
        private static bool TryMica(IntPtr hwnd)
        {
            try
            {
                int backdrop = DWMSBT_MAINWINDOW;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                return hr == 0;
            }
            catch { return false; }
        }
        private static void TryAcrylicWin10(IntPtr hwnd)
        {
            var accent = new AccentPolicy
            {
                AccentState   = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = unchecked((int)0xCC0F1419)
            };
            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute  = WCA_ACCENT_POLICY,
                    Data       = ptr,
                    SizeOfData = size
                };
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            catch { }
            finally { Marshal.FreeHGlobal(ptr); }
        }
    }
}
