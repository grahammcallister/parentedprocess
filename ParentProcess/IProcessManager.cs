using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParentProcess
{
    public interface IProcessManager
    {
        void StartProcess();
        void StopProcess();
        void FindMainWindowHandle();
        void PlaceInParent(object parent);

        ProcessInfo ParentedProcessInfo { get; }

        string ProcessFileName { get; set; }
        string WindowCaption { get; set; }
        string FriendlyName { get; set; }

        IntPtr ParentWindowHandle { get; set; }

        event ProcessStarted ProcessStartedEvent;
        event ProcessStopped ProcessStoppedEvent;
        event ProcessMainWindowHandleFound ProcessMainWindowHandleFoundEvent;
        event ProcessUnhandledException ProcessUnhandledExceptionEvent;
    }
}
