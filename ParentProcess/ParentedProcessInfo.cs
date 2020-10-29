﻿using System;
using System.Diagnostics;

namespace ParentProcess
{
    public class ParentedProcessInfo
    {
        public Process Process { get; set; }
        public IntPtr MainWindowHandle { get; set; }
        public ProcessStartInfo ProcessStartInfo { get; set; }

        public string ProcessToParentFilename { get; set; }
        public string WindowCaption { get; set; }
        public string FriendlyName { get; set; }
    }
}