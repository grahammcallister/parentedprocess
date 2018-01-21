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

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] 
        static extern int GetWindowTextLength(IntPtr hWnd); 
 
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)] 
        static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, long cch); 

        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate(IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static IEnumerable<IntPtr> FindWindowsWithProcessId(string processIdString)
        {
            return FindWindows(delegate(IntPtr wnd, IntPtr param)
            {
                uint processId = UInt32.MinValue;
                GetWindowThreadProcessId(wnd, out processId);
                if (string.Equals(processId.ToString(), processIdString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
                return false;
            });
        }

        public static string GetCaptionOfWindow(IntPtr hwnd) 
        { 
            string caption = ""; 
            StringBuilder windowText  = null; 
            try 
            { 
                int max_length = GetWindowTextLength(hwnd); 
                windowText = new StringBuilder("", max_length + 5); 
                GetWindowText(hwnd, windowText, max_length + 2); 
 
                if (!String.IsNullOrEmpty(windowText.ToString()) && !String.IsNullOrWhiteSpace(windowText.ToString())) 
                    caption = windowText.ToString(); 
            } 
            catch (Exception ex) 
            { 
                caption = ex.Message; 
            } 
            finally 
            { 
                windowText = null; 
            } 
            return caption; 
        } 
    }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}