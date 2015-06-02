using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace X2MP.Models
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action<object> _execute;
        private Func<object, bool> _canExecute;

        public Command(Action<object> execute, Func<object, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }

            //set delegates
            _execute = execute;
            _canExecute = canExecute;
        }

        public Command(Action<object> execute)
        {
            //set delegates
            _execute = execute;
            _canExecute = (parameter) => { return true; };
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            _execute.Invoke(parameter);
        }

        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void OnCanExecuteChanged()
        {
            var handle = CanExecuteChanged;
            if (handle != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
