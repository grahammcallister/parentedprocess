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

        public static void PlaceChildWindowInParent(HandleRef parent, HandleRef child, bool showWin32Menu, bool noMessagePump)
        {
            uint originalStyle = GetWindowLong(child, -16);
            uint style = (originalStyle) & 2134638591U;
            if (!showWin32Menu)
                style |= 1073741824U;
            SetWindowLong(child, -16, style);
            Rectangle clientRect = Win32Wrapper.GetClientRectangle(parent, noMessagePump);
            SetWindowPos(child, 0, clientRect.X, clientRect.Y, clientRect.Width, clientRect.Height, 128);
            SetParent(child, parent);
            SetWindowPos(child, 0, clientRect.X, clientRect.Y, clientRect.Width, clientRect.Height, 64);
        }

        public static Rectangle GetClientRectangle(HandleRef window, bool noMessagePump)
        {
            Win32Wrapper.RECT lpRect = new Win32Wrapper.RECT();
            Win32Wrapper.GetWindowRect(window, ref lpRect);
            return !noMessagePump ? new Rectangle(0, 0, lpRect.Width, lpRect.Height) : new Rectangle(-4, -Win32Wrapper.GetSystemMetrics(4) - 4, lpRect.Width + 10, lpRect.Height + 12);
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
