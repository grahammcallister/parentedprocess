using System;

namespace ParentProcess
{
    public delegate void ProcessStarted(EventArgs args);

    public delegate void ProcessMainWindowHandleFound(EventArgs args);

    public delegate void ProcessStopped(EventArgs args);
}