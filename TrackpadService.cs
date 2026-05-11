using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace VolumeMixerPro
{
    public class TrackpadService : IDisposable
    {
        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const uint RIM_TYPEHID = 2;
        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
            public byte bRawData; 
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWHID hid;
        }
        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);
        [DllImport("user32.dll")]
        public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        public event Action<int> OnGestureScroll;
        private double _lastY = -1;
        private double _accumulatedDelta = 0;
        private const double SCROLL_THRESHOLD = 80;
        public TrackpadService(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x0D; 
            rid[0].usUsage = 0x05;     
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = hwnd;
            RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }
        public void ProcessMessage(IntPtr lParam)
        {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
            if (dwSize == 0) return;
            IntPtr pData = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, pData, ref dwSize, headerSize) == dwSize)
                {
                    RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(pData, typeof(RAWINPUT));
                    if (raw.header.dwType == RIM_TYPEHID)
                    {
                        byte[] hidData = new byte[raw.hid.dwSizeHid];
                        int dataOffset = (int)headerSize + 8; 
                        Marshal.Copy(pData + dataOffset, hidData, 0, (int)raw.hid.dwSizeHid);
                        ParsePTP(hidData);
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pData); }
        }
        private void ParsePTP(byte[] data)
        {
            if (data.Length < 32) return; 
            int contactCount = data[data.Length - 3]; 
            if (contactCount < 0 || contactCount > 10)
            {
                for (int i = data.Length - 1; i > 0; i--)
                {
                    if (data[i] > 0 && data[i] <= 5)
                    {
                        contactCount = data[i];
                        break;
                    }
                }
            }
            if (contactCount == 4)
            {
                HotkeyService.Is4FingerGestureActive = true;
                if (data.Length >= 7)
                {
                    int y = BitConverter.ToUInt16(data, 5); 
                    double currentY = y;
                    if (_lastY != -1)
                    {
                        double dy = currentY - _lastY;
                        _accumulatedDelta += dy;
                        if (Math.Abs(_accumulatedDelta) >= SCROLL_THRESHOLD)
                        {
                            int direction = _accumulatedDelta > 0 ? -1 : 1;
                            OnGestureScroll?.Invoke(direction);
                            _accumulatedDelta = 0;
                        }
                    }
                    _lastY = currentY;
                }
            }
            else
            {
                HotkeyService.Is4FingerGestureActive = false;
                _lastY = -1;
                _accumulatedDelta = 0;
            }
        }
        public void Dispose()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x0D;
            rid[0].usUsage = 0x05;
            rid[0].dwFlags = 0x00000001; 
            rid[0].hwndTarget = IntPtr.Zero;
            RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }
    }
}
