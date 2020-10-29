using ParentProcess.Win32Api;
using System;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ParentProcess
{
    public class ParentedProcessManager : IParentedProcessManager
    {
        private BackgroundWorker _processWorker;
        private BackgroundWorker _shutdownWorker;
        private BackgroundWorker _findMainWindowHandleWorker;

        public ParentedProcessManager(ParentedProcessInfo parentedProcessInfo) : this(parentedProcessInfo.ProcessToParentFilename, parentedProcessInfo.WindowCaption, parentedProcessInfo.FriendlyName)
        {
        }

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
            InitialiseFindMainWindowHandleBackgroundWorker();
        }

        public string ProcessToParentFilename { get; set; }
        public string ProcessToParentWindowCaption { get; set; }
        public string ProcessToParentFriendlyName { get; set; }

        public ProcessStartInfo ProcessStartInfo { get; set; }

        public bool IsRunning { 
            get {
                return (!ParentedProcessInfo?.Process?.HasExited ?? false);
            }
            private set { }
        }

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

        private void InitialiseFindMainWindowHandleBackgroundWorker()
        {
            _findMainWindowHandleWorker = new BackgroundWorker();
            _findMainWindowHandleWorker.WorkerSupportsCancellation = false;
            _findMainWindowHandleWorker.DoWork += BackgroundFindMainWindowHandle;
            _findMainWindowHandleWorker.RunWorkerCompleted += BackgroundFindMainWindowHandleCompleted;
        }

        private void BackgroundFindMainWindowHandleCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e?.Result != null)
            {
                var result = (Boolean)e.Result;
                if (result)
                {
                    OnProcessMainWindowHandleFoundEvent();
                }
            }
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
            
            ParentedProcessInfo.Process = new Process {
                StartInfo = ParentedProcessInfo.ProcessStartInfo,
                EnableRaisingEvents = true
            };

            ParentedProcessInfo.Process.Exited += ProcessOnExited;
            var result = ParentedProcessInfo.Process.Start();
            if (result)
            {
                //GetMainWindowHandle();
                IsRunning = true;
                OnProcessStarted();
            }
        }

        private void BackgroundFindMainWindowHandle(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            if (ParentedProcessInfo.Process.MainWindowHandle != IntPtr.Zero)
            {
                ParentedProcessInfo.MainWindowHandle = ParentedProcessInfo.Process.MainWindowHandle;
                doWorkEventArgs.Result = true;
                return;
            }
            else
            {
                while (ParentedProcessInfo.MainWindowHandle == IntPtr.Zero && !ParentedProcessInfo.Process.HasExited)
                {
                    var processes = FindParentProcess.FindProcessesSpawnedBy((UInt32)ParentedProcessInfo.Process.Id);
                    if (processes.Count() == 1)
                    {
                        processes = Process.GetProcessesByName(ProcessToParentFriendlyName);
                    }
                    var processesWithWindows = processes.Where(x => x.MainWindowHandle != IntPtr.Zero);
                    foreach (var process in processesWithWindows)
                    {
                        var parent = FindParentProcess.GetParentProcess(process.Id);
                        if (parent != null && parent.Id == ParentedProcessInfo.Process.Id)
                        {
                            // Found you!
                            ParentedProcessInfo.MainWindowHandle = process.MainWindowHandle;
                            doWorkEventArgs.Result = true;
                            return;
                        }
                    }

                }
            }
        }

        private void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            IsRunning = false;
            var senderProcess = sender as Process;
            if(senderProcess != null)
            {
                if(senderProcess.ExitCode != 0)
                {
                    OnProcessUnhandledExceptionEvent(new UnhandledExceptionEventArgs(new Exception($"Process exited with exit code {senderProcess.ExitCode}"), true));
                    return;
                }
            }
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
                            if (FindParentProcess.GetParentProcess(process.Id)?.Id ==
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

        public void FindMainWindowHandle()
        {
            if (IsRunning)
            {
                _findMainWindowHandleWorker.RunWorkerAsync();
            }
        }

        public ParentedProcessInfo ParentedProcessInfo { get; private set; }

        public event ProcessStarted ProcessStartedEvent;
        public event ProcessStopped ProcessStoppedEvent;
        public event ProcessMainWindowHandleFound ProcessMainWindowHandleFoundEvent;
        public event ProcessUnhandledException ProcessUnhandledExceptionEvent;

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

        protected virtual void OnProcessUnhandledExceptionEvent(UnhandledExceptionEventArgs args)
        {
            ProcessUnhandledExceptionEvent?.Invoke(this, args);
        }
    }
}