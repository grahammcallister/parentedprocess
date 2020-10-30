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

        public MainWindow()
        {
            InitializeComponent();

        }
        

        public event PropertyChangedEventHandler PropertyChanged;


        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            DemoControl1.ViewModel.Close();
            DemoControl2.ViewModel.Close();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}