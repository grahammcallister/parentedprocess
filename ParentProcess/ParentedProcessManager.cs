using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ParentProcess
{
    public class ParentedProcessManager
    {
        private BackgroundWorker _processWorker;
        private BackgroundWorker _shutdownWorker;

        public ParentedProcessManager(string processToParentFilename, string windowCaption)
        {
            if (string.IsNullOrEmpty(processToParentFilename))
                throw new ArgumentNullException(nameof(processToParentFilename));
            if (!File.Exists(processToParentFilename))
                throw new FileNotFoundException(
                    $"Unable to parent process for file not found {processToParentFilename}", processToParentFilename);
            ProcessToParentFilename = processToParentFilename;
            ProcessToParentWindowCaption = windowCaption;
            InitialiseProcessBackgroundWorker();
            InitialiseProcessShutdownBackgroundWorker();
        }

        public ParentedProcessManager(string processToParentFilename, string windowCaption,
            ProcessStartInfo processStartInfo) : this(processToParentFilename, windowCaption)
        {
            ProcessStartInfo = processStartInfo;
        }

        public string ProcessToParentFilename { get; set; }
        public string ProcessToParentWindowCaption { get; set; }

        public ProcessStartInfo ProcessStartInfo { get; set; }

        public bool IsRunning { get; private set; }

        private void InitialiseProcessBackgroundWorker()
        {
            _processWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            _processWorker.DoWork += BackgroundStartProcess;
        }

        private void InitialiseProcessShutdownBackgroundWorker()
        {
            _shutdownWorker = new BackgroundWorker();
            _shutdownWorker.WorkerSupportsCancellation = false;
            _shutdownWorker.DoWork += BackgroundStopProcess;
        }

        private void BackgroundStartProcess(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            ParentedProcessInfo = new ParentedProcessInfo();
            if (ProcessStartInfo == null)
            {
                ParentedProcessInfo.ProcessStartInfo = new ProcessStartInfo
                {
                    FileName = ProcessToParentFilename,
                    WindowStyle = ProcessWindowStyle.Minimized
                };
            }
            else
            {
                ParentedProcessInfo.ProcessStartInfo = ProcessStartInfo;
            }
            ParentedProcessInfo.Process = new Process {StartInfo = ParentedProcessInfo.ProcessStartInfo, EnableRaisingEvents = true};
            ParentedProcessInfo.Process.Exited += ProcessOnExited;
            var result = ParentedProcessInfo.Process.Start();
            ParentedProcessInfo.Process.WaitForInputIdle(5000);
            ParentedProcessInfo.Process.Refresh();
            if (result)
            {
                GetMainWindowHandle();
                IsRunning = true;
                OnProcessStarted();
            }
        }

        private void GetMainWindowHandle()
        {
            if (ParentedProcessInfo.Process.MainWindowHandle != IntPtr.Zero)
            {
                ParentedProcessInfo.MainWindowHandle = ParentedProcessInfo.Process.MainWindowHandle;
                OnProcessMainWindowHandleFoundEvent();
            }
            else
            {
                while (ParentedProcessInfo.MainWindowHandle == IntPtr.Zero && !ParentedProcessInfo.Process.HasExited)
                {
                    var hnd = Win32EnumWindowWrapper.FindWindowsWithProcessId(ParentedProcessInfo.Process.Id.ToString());
                    foreach (var hwnd in hnd)
                    {
                        var caption = Win32EnumWindowWrapper.GetCaptionOfWindow(hwnd);
                        if (!string.IsNullOrEmpty(caption) && caption.Contains(ProcessToParentWindowCaption))
                        {
                            ParentedProcessInfo.MainWindowHandle = hwnd;
                            OnProcessMainWindowHandleFoundEvent();
                            return;
                        }
                    }
                }
            }
        }

        private void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            IsRunning = false;
            OnProcessStoppedEvent();
        }

        private void BackgroundStopProcess(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            _processWorker.CancelAsync();
            if (!ParentedProcessInfo.Process.HasExited)
            {
                if (ParentedProcessInfo.Process != null)
                {
                    ParentedProcessInfo.Process.Kill();
                    ParentedProcessInfo.Process.WaitForExit();
                }
            }
        }

        public void StartProcess()
        {
            if (!IsRunning)
            {
                _processWorker.RunWorkerAsync();
            }
        }

        public void StopProcess()
        {
            if (IsRunning)
            {
                _shutdownWorker.RunWorkerAsync();
            }
        }

        public ParentedProcessInfo ParentedProcessInfo { get; private set; }

        public event ProcessStarted ProcessStartedEvent;
        public event ProcessStopped ProcessStoppedEvent;
        public event ProcessMainWindowHandleFound ProcessMainWindowHandleFoundEvent;

        protected virtual void OnProcessStarted()
        {
            ProcessStartedEvent?.Invoke(EventArgs.Empty);
        }

        protected virtual void OnProcessStoppedEvent()
        {
            ProcessStoppedEvent?.Invoke(EventArgs.Empty);
        }

        protected virtual void OnProcessMainWindowHandleFoundEvent()
        {
            ProcessMainWindowHandleFoundEvent?.Invoke(EventArgs.Empty);
        }
    }
}