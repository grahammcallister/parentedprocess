using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ParentProcess.Win32Api
{
    public static class EnumerateProcessWindows
    {
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

        private static IEnumerable<IntPtr> CallEnumWindowsWithFilter(Win32Wrapper.EnumWindowsProc filter)
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

            Win32Wrapper.EnumWindows(EnumProc, IntPtr.Zero);

            return windows;
        }

        private static string ProcessIdForWindow(IntPtr wnd)
        {
            uint processId = UInt32.MinValue;
            Win32Wrapper.GetWindowThreadProcessId(wnd, out processId);
            return processId.ToString();
        }
    }

    
}