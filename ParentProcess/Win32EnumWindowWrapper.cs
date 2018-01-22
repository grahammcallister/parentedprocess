using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ParentProcess
{
    public static class Win32EnumWindowWrapper
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError=true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private static IEnumerable<IntPtr> CallEnumWindowsWithFilter(EnumWindowsProc filter)
        {
            List<IntPtr> windows = new List<IntPtr>();

            bool EnumProc(IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }

            EnumWindows(EnumProc, IntPtr.Zero);

            return windows;
        }

        private static string ProcessIdForWindow(IntPtr wnd)
        {
            uint processId = UInt32.MinValue;
            GetWindowThreadProcessId(wnd, out processId);
            return processId.ToString();
        }


        public static IEnumerable<IntPtr> EnumWindowsForProcessId(string processIdString)
        {
            bool Filter(IntPtr wnd, IntPtr param)
            {
                string processIdForWindow = ProcessIdForWindow(wnd);
                if (string.Equals(processIdForWindow, processIdString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
                return false;
            }

            return CallEnumWindowsWithFilter(Filter);
        }

    }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}