using System;

namespace ParentProcess
{
    public delegate void ProcessStarted(EventArgs args);

    public delegate void ProcessMainWindowHandleFound(EventArgs args);

    public delegate void ProcessStopped(EventArgs args);

    public delegate void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs args);

    public delegate void ProcessNonResponsive(EventArgs args);
}