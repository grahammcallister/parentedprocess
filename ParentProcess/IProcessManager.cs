using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ParentProcess
{
    public interface IProcessManager
    {
        void StartProcess();
        void StopProcess();
        void FindChildMainWindowHandle();
        void PlaceInParent(object parent, Rectangle clientRect = new Rectangle());

        ProcessInfo ParentedProcessInfo { get; }

        string ProcessFileName { get; set; }
        string WindowCaption { get; set; }
        string FriendlyName { get; set; }

        IntPtr ParentWindowHandle { get; set; }

        event ProcessStarted ProcessStartedEvent;
        event ProcessStopped ProcessStoppedEvent;
        event ProcessMainWindowHandleFound ProcessMainWindowHandleFoundEvent;
        event ProcessUnhandledException ProcessUnhandledExceptionEvent;
        event ProcessNonResponsive ProcessNonResponsiveEvent;
    }
}
