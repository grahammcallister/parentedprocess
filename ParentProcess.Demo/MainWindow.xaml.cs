using System;
using System.ComponentModel;
using System.Windows;
using ParentProcess.Demo.Annotations;

namespace ParentProcess.Demo
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ParentedProcessManager _manager;
        private bool _running;
        private IntPtr _windowHandle;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Running = false;
            _manager = new ParentedProcessManager(@"C:\Windows\notepad.exe", "Notepad");
            _manager.ProcessMainWindowHandleFoundEvent += ManagerOnProcessMainWindowHandleFoundEvent;
        }

        public bool Running
        {
            get => _running;
            set
            {
                if (value == _running) return;
                _running = value;
                OnPropertyChanged(nameof(Running));
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void ManagerOnProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            WindowHandle = _manager.ParentedProcessInfo.MainWindowHandle;
        }

        private void StartClicked(object sender, RoutedEventArgs e)
        {
            if (_manager != null)
                if (!_manager.IsRunning)
                {
                    StartButton.IsEnabled = false;
                    _manager.ProcessStartedEvent += ManagerOnProcessStartedEvent;
                    _manager.StartProcess();
                    StopButton.IsEnabled = true;
                }
        }

        private void StopClicked(object sender, RoutedEventArgs e)
        {
            if (_manager != null)
            {
                StopButton.IsEnabled = false;
                _manager.StopProcess();
                StartButton.IsEnabled = true;
            }
        }

        private void ManagerOnProcessStoppedEvent(EventArgs args)
        {
            Running = false;
            _manager.ProcessStoppedEvent -= ManagerOnProcessStoppedEvent;
        }

        private void ManagerOnProcessStartedEvent(EventArgs args)
        {
            Running = true;
            _manager.ProcessStoppedEvent += ManagerOnProcessStoppedEvent;
            _manager.ProcessStartedEvent -= ManagerOnProcessStartedEvent;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _manager.StopProcess();
        }
    }
}