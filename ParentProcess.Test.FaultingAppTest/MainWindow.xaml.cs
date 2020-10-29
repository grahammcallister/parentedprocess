using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParentProcess.Test.FaultingAppTest
{
    /// <summary>
    /// This application will fault for testing purposes
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker _worker;

        public MainWindow()
        {
            InitializeComponent();
            _worker = new BackgroundWorker();
            _worker.DoWork += SleepThread;
            _worker.RunWorkerCompleted += SleepCompletedThrowException;
            _worker.RunWorkerAsync();
        }

        private void SleepCompletedThrowException(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new OutOfMemoryException("Test fault application");
        }

        private void SleepThread(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(2000);
            
        }
    }
}
