using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ParentProcess
{
    public class ParentedProcessManager
    {
        private BackgroundWorker _processWorker;
        private BackgroundWorker _shutdownWorker;

        public ParentedProcessManager(string processToParentFilename, string windowCaption, string friendlyName)
        {
            if (string.IsNullOrEmpty(processToParentFilename))
                throw new ArgumentNullException(nameof(processToParentFilename));
            if (!File.Exists(processToParentFilename))
                throw new FileNotFoundException(
                    $"Unable to parent process for file not found {processToParentFilename}", processToParentFilename);
            ProcessToParentFilename = processToParentFilename;
            ProcessToParentWindowCaption = windowCaption;
            ProcessToParentFriendlyName = friendlyName;
            InitialiseProcessBackgroundWorker();
            InitialiseProcessShutdownBackgroundWorker();
        }

        

        public ParentedProcessManager(string processToParentFilename, string windowCaption, string friendlyName,
            ProcessStartInfo processStartInfo) : this(processToParentFilename, windowCaption, friendlyName)
        {
            ProcessStartInfo = processStartInfo;
        }

        public string ProcessToParentFilename { get; set; }
        public string ProcessToParentWindowCaption { get; set; }
        public string ProcessToParentFriendlyName { get; set; }

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
                    var processes = Win32ChildprocessesWrapper.FindProcessesSpawnedBy((UInt32)ParentedProcessInfo.Process.Id);
                    if (processes.Count() == 1)
                    {
                        processes = Process.GetProcessesByName(ProcessToParentFriendlyName);
                    }
                    var processesWithWindows = processes.Where(x => x.MainWindowHandle != IntPtr.Zero);
                    foreach (var process in processesWithWindows)
                    {
                        var parent = ParentProcessUtilities.GetParentProcess(process.Id);
                        if (parent != null && parent.Id == ParentedProcessInfo.Process.Id)
                        {
                            // Found you!
                            ParentedProcessInfo.MainWindowHandle = process.MainWindowHandle;
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
                    var processes = Process.GetProcessesByName(ProcessToParentFriendlyName).Where(p => p.Id != ParentedProcessInfo.Process.Id);
                    foreach (var process in processes)
                    {
                        if (!process.HasExited && !ParentedProcessInfo.Process.HasExited)
                        {
                            if (ParentProcessUtilities.GetParentProcess(process.Id)?.Id ==
                                ParentedProcessInfo.Process.Id)
                            {
                                try
                                {
                                    process.Kill();
                                    process.WaitForExit();
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    if (!ParentedProcessInfo.Process.HasExited)
                    {
                        ParentedProcessInfo.Process.Kill();
                        ParentedProcessInfo.Process.WaitForExit();
                    }
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