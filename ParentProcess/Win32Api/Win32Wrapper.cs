using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace ParentProcess.Win32Api
{
    public class Win32Wrapper
    {
        public const int WS_POPUP = -2147483648;
        public const int WS_CHILD = 1073741824;
        public const int WS_CAPTION = 12582912;
        public const int WS_THICKFRAME = 262144;
        public const int GWL_STYLE = -16;
        public const int SM_CYCAPTION = 4;
        public const int HWND_TOP = 0;
        public const int SWP_NOZORDER = 4;
        public const int SWP_NOACTIVATE = 16;
        public const int SWP_SHOWWINDOW = 64;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(HandleRef child, HandleRef newParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetSystemMetrics(int flag);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowLong(HandleRef hWnd, int type);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SetWindowLong(HandleRef hWnd, int type, uint value);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
          HandleRef hWnd,
          int hWndInsertAfter,
          int x,
          int y,
          int cx,
          int cy,
          int uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowRect(HandleRef hWnd, ref RECT lpRect);

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => this.Right - this.Left;

            public int Height => this.Bottom - this.Top;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, long cch);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
}
