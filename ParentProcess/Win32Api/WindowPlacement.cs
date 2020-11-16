using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;

namespace ParentProcess.Win32Api
{
    public static class WindowPlacement
    {
        public static void PlaceChildWindowInParent(HandleRef parent, HandleRef child, bool showWin32Menu, bool noMessagePump)
        {
            uint originalStyle = Win32Wrapper.GetWindowLong(child, -16);
            uint style = (originalStyle) & 2134638591U;
            if (!showWin32Menu)
                style |= 1073741824U;
            Win32Wrapper.SetWindowLong(child, -16, style);
            Rectangle clientRect = GetClientRectangle(parent, noMessagePump);
            Win32Wrapper.SetWindowPos(child, 0, clientRect.X, clientRect.Y, clientRect.Width, clientRect.Height, 128);
            Win32Wrapper.SetParent(child, parent);
            Win32Wrapper.SetWindowPos(child, 0, clientRect.X, clientRect.Y, clientRect.Width, clientRect.Height, 64);
        }

        public static Rectangle GetClientRectangle(HandleRef window, bool noMessagePump)
        {
            Win32Wrapper.RECT lpRect = new Win32Wrapper.RECT();
            Win32Wrapper.GetWindowRect(window, ref lpRect);
            return !noMessagePump ? new Rectangle(0, 0, lpRect.Width, lpRect.Height) : new Rectangle(-4, -Win32Wrapper.GetSystemMetrics(4) - 4, lpRect.Width + 10, lpRect.Height + 12);
        }

    }
}
