using System;
using System.Diagnostics;

namespace ParentProcess
{
    public class ProcessInfo
    {
        public Process Process { get; set; }
        public IntPtr ChildMainWindowHandle { get; set; }
        public ProcessStartInfo ProcessStartInfo { get; set; }
    }
}