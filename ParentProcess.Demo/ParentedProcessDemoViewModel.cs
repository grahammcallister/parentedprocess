using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace ParentProcess.Demo
{
    public class ParentedProcessDemoViewModel : INotifyPropertyChanged
    {
        private readonly ParentedProcessManager _manager;
        private bool _running;
        private string _pathToExecutable;
        private IntPtr _windowHandle;
        public DelegateCommand StartCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }
        public DelegateCommand GetWindowHandleCommand { get; set; }

        public ParentedProcessDemoViewModel()
        {
            SetNotRunningUi();
            PathToExecutable = @"C:\Windows\notepad.exe";
            _manager = new ParentedProcessManager(PathToExecutable, "App", "App");
            _manager.ProcessMainWindowHandleFoundEvent += ManagerOnProcessMainWindowHandleFoundEvent;
            _manager.ProcessStartedEvent += ManagerOnProcessStartedEvent;
            _manager.ProcessStoppedEvent += ManagerOnProcessStoppedEvent;
            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(Close);
            GetWindowHandleCommand = new DelegateCommand(GetWindowHandle);
        }

        public void Close()
        {
            if (_manager != null && _manager.IsRunning)
            {
                _manager.StopProcess();
            }
        }

        public void Start()
        {
            if (_manager != null) { 
                if (!_manager.IsRunning)
                {
                    _manager.ProcessToParentFilename = PathToExecutable;
                    _manager.StartProcess();
                }
            }
        }

        public void GetWindowHandle()
        {
            if (_manager != null)
            {
                if (_manager.IsRunning)
                {
                    _manager.FindMainWindowHandle();
                }
            }
        }

        public bool Running
        {
            get => _running;
            set
            {
                if (value == _running) return;
                _running = value;
                OnPropertyChanged(nameof(Running));
                OnPropertyChanged("CanGetWindowHandle");
                OnPropertyChanged("CanStop");
                OnPropertyChanged("CanStart");
            }
        }

        public string PathToExecutable
        {
            get => _pathToExecutable;
            set
            {
                _pathToExecutable = value;
                OnPropertyChanged(nameof(PathToExecutable));
            }
        }

        public IntPtr WindowHandle
        {
            get => _windowHandle;
            set
            {
                if (value == _windowHandle) return;
                _windowHandle = value;
                OnPropertyChanged(nameof(WindowHandle));
            }
        }

        public ParentedProcessManager ParentedProcessManager { get => _manager; }

        private void SetNotRunningUi()
        {
            Running = false;
            WindowHandle = IntPtr.Zero;
        }

        private void SetRunningUi()
        {
            Running = true;
        }

        public bool CanStop { get => Running; }
        public bool CanStart { get => !Running; }
        public bool CanGetWindowHandle { get => Running; }

        private void ManagerOnProcessStoppedEvent(EventArgs args)
        {
            Application.Current?.Dispatcher?.Invoke(new Action(SetNotRunningUi));
        }

        private void ManagerOnProcessStartedEvent(EventArgs args)
        {
            Application.Current?.Dispatcher?.Invoke(new Action(SetRunningUi));
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ManagerOnProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            WindowHandle = _manager.ParentedProcessInfo.MainWindowHandle;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
