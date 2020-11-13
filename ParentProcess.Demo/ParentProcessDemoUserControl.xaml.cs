using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ParentProcess.Demo
{
    /// <summary>
    /// Interaction logic for ParentProcessDemoUserControl.xaml
    /// </summary>
    public partial class ParentProcessDemoUserControl : UserControl
    {
        public ParentProcessDemoUserControl()
        {
            InitializeComponent();
            _viewmodel = new ParentedProcessDemoViewModel();
            DataContext = _viewmodel;
            ViewModel.ParentControl = ParentControl;
            var panel = new System.Windows.Forms.Panel();
            panel.Dock = System.Windows.Forms.DockStyle.Fill;
            ParentControl.Child = panel;
        }

        private ParentedProcessDemoViewModel _viewmodel;

        public ParentedProcessDemoViewModel ViewModel { get => _viewmodel;  }
    }
}
