using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ParentProcess.Demo
{
    public class DelegateCommand : ICommand
    {
        private SimpleEventHandler handler;
        private bool isEnabled = true;

        public DelegateCommand(SimpleEventHandler handler)
        {
            this.handler = handler;
        }


        public event EventHandler CanExecuteChanged;

        public bool IsEnabled
        {
            get { return this.isEnabled; }
        }
        void ICommand.Execute(object arg)
        {
            this.handler();
        }

        bool ICommand.CanExecute(object arg)
        {
            return this.IsEnabled;
        }

        private void OnCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }
        public delegate void SimpleEventHandler();
    }
}