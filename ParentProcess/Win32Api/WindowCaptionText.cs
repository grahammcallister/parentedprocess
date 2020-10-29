using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ParentProcess.Win32Api
{
    public static class WindowCaptionText
    {
        public static string GetWindowCaptionText(IntPtr hwnd)
        {
            string caption = "";
            StringBuilder windowText = null;
            try
            {
                int max_length = Win32Wrapper.GetWindowTextLength(hwnd);
                windowText = new StringBuilder("", max_length + 5);
                Win32Wrapper.GetWindowText(hwnd, windowText, max_length + 2);

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
}
