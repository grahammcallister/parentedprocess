using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParentProcess
{
    public interface IParentedProcessManager
    {
        void StartProcess();
        void StopProcess();
        void FindMainWindowHandle();

        ParentedProcessInfo ParentedProcessInfo { get; }

        event ProcessStarted ProcessStartedEvent;
        event ProcessStopped ProcessStoppedEvent;
        event ProcessMainWindowHandleFound ProcessMainWindowHandleFoundEvent;
        event ProcessUnhandledException ProcessUnhandledExceptionEvent;
    }
}
