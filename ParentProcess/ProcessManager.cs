using ParentProcess.Win32Api;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ParentProcess
{
    public class ProcessManager : IProcessManager
    {
        private BackgroundWorker _processWorker;
        private BackgroundWorker _shutdownWorker;
        private BackgroundWorker _findMainWindowHandleWorker;

        public ProcessManager(string processToParentFilename, string windowCaption, string friendlyName)
        {
            if (string.IsNullOrEmpty(processToParentFilename))
                throw new ArgumentNullException(nameof(processToParentFilename));
            if (!File.Exists(processToParentFilename))
                throw new FileNotFoundException(
                    $"Unable to parent process for file not found {processToParentFilename}", processToParentFilename);
            ProcessFileName = processToParentFilename;
            WindowCaption = windowCaption;
            FriendlyName = friendlyName;

            InitialiseProcessBackgroundWorker();
            InitialiseProcessShutdownBackgroundWorker();
            InitialiseFindMainWindowHandleBackgroundWorker();
        }

        public string ProcessFileName { get; set; }
        public string WindowCaption { get; set; }
        public string FriendlyName { get; set; }
        public ProcessInfo ParentedProcessInfo { get; set; }
        public ProcessStartInfo ProcessStartInfo { get; set; }
        public IntPtr ParentWindowHandle { get; set; }

        public bool HasExited { 
            get {
                return ParentedProcessInfo.Process.HasExited;
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
            ParentedProcessInfo = new ProcessInfo();
            if (ProcessStartInfo == null)
            {
                ParentedProcessInfo.ProcessStartInfo = new ProcessStartInfo
                {
                    FileName = ProcessFileName,
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

            ParentedProcessInfo.Process.Exited += OnProcessExitFired;
            var result = ParentedProcessInfo.Process.Start();
            if (result)
            {
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
                        processes = Process.GetProcessesByName(FriendlyName);
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

        private void OnProcessExitFired(object sender, EventArgs eventArgs)
        {
            var senderProcess = sender as Process;
            if(senderProcess != null)
            {
                if(senderProcess.ExitCode != 0)
                {
                    OnProcessUnhandledExceptionEvent(new UnhandledExceptionEventArgs(new Exception($"Process exited with exit code {senderProcess.ExitCode}"), true));
                    return;
                }
            }
           // OnProcessStoppedEvent();
        }

        private void BackgroundStopProcess(object sender, DoWorkEventArgs doWorkEventArgs)
        {

            _processWorker.CancelAsync();

            try
            {
                var process = ParentedProcessInfo.Process;
                if (process != null)
                {
                    process.CloseMainWindow();
                    process.WaitForExit(750);
                }
                if (!process.HasExited)
                {
                    if (process != null)
                    {
                        var processes = Process.GetProcessesByName(FriendlyName).Where(p => p.Id != ParentedProcessInfo.Process.Id);
                        foreach (var proc in processes)
                        {
                            if (!proc.HasExited && !ParentedProcessInfo.Process.HasExited)
                            {
                                if (FindParentProcess.GetParentProcess(proc.Id)?.Id ==
                                    process.Id)
                                {
                                    try
                                    {
                                        proc.Kill();
                                        proc.WaitForExit();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.Kill();
                                process.WaitForExit();
                            }
                            catch { }
                        }
                    }
                }
            } catch (Exception ex)
            {

            } finally
            {
                OnProcessStoppedEvent();
            }
        }

        public void StartProcess()
        {
          _processWorker.RunWorkerAsync();
        }

        public void StopProcess()
        {
            if (!HasExited)
            {
                _shutdownWorker.RunWorkerAsync();
            }
        }

        public void FindMainWindowHandle()
        {
            if (!HasExited)
            {
                _findMainWindowHandleWorker.RunWorkerAsync();
            }
        }

        public void PlaceInParent(object parentObject)
        {
            HandleRef parent = new HandleRef(parentObject, ParentWindowHandle);
            var hwndChild = ParentedProcessInfo.Process.MainWindowHandle;
            HandleRef child = new HandleRef(ParentedProcessInfo.Process, hwndChild);
            bool noMessagePump = false;
            bool showWin32Menu = false;
            Win32Wrapper.PlaceChildWindowInParent(parent, child, showWin32Menu, noMessagePump);
        }

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