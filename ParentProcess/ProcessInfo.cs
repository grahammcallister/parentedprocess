using System;
using System.Diagnostics;

namespace ParentProcess
{
    public class ProcessInfo
    {
        public Process Process { get; set; }
        public IntPtr MainWindowHandle { get; set; }
        public ProcessStartInfo ProcessStartInfo { get; set; }
    }
}