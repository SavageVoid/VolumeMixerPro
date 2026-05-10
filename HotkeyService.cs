using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace VolumeMixerPro
{
    public class HotkeyService : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_MOUSEWHEEL = 0x020A;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelProc _keyboardProc;
        private LowLevelProc _mouseProc;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        public event Action<int> OnVolumeScroll; // delta: +1 for up, -1 for down
        public event Action OnToggleMenu;
        public event Action OnMuteActive;
        public event Action OnSoloMode;
        public event Action OnUnmuteAll;
        public event Action OnPanicReset;

        public HotkeyService()
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHookId = SetHook(_keyboardProc, WH_KEYBOARD_LL);
            _mouseHookId = SetHook(_mouseProc, WH_MOUSE_LL);
        }

        private IntPtr SetHook(LowLevelProc proc, int hookType)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;
                bool alt = (GetKeyState(0x12) & 0x8000) != 0;
                bool shift = (GetKeyState(0x10) & 0x8000) != 0;

                // Ctrl + Alt + V (Toggle Menu)
                if (ctrl && alt && vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.V))
                {
                    OnToggleMenu?.Invoke();
                    return (IntPtr)1; // Consumed
                }

                // Ctrl + Alt + M (Mute Active)
                if (ctrl && alt && !shift && vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.M))
                {
                    OnMuteActive?.Invoke();
                    return (IntPtr)1;
                }

                // Ctrl + Alt + Shift + M (Solo Mode)
                if (ctrl && alt && shift && vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.M))
                {
                    OnSoloMode?.Invoke();
                    return (IntPtr)1;
                }

                // Ctrl + Alt + 0 (Unmute All)
                if (ctrl && alt && vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.D0))
                {
                    OnUnmuteAll?.Invoke();
                    return (IntPtr)1;
                }

                // Ctrl + Alt + P or R (Panic/Reset)
                if (ctrl && alt && (vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.P) || vkCode == (int)KeyInterop.VirtualKeyFromKey(Key.R)))
                {
                    OnPanicReset?.Invoke();
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;
                if (ctrl)
                {
                    int delta = (short)((Marshal.ReadInt32(lParam + 8) >> 16) & 0xFFFF);
                    OnVolumeScroll?.Invoke(delta > 0 ? 1 : -1);
                    return (IntPtr)1; // Consumed
                }
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_keyboardHookId);
            UnhookWindowsHookEx(_mouseHookId);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);
    }
}
