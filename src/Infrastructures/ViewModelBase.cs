using System;
using System.ComponentModel;

using System.Windows.Input;

namespace SpeechToTextAssistant.Infrastructures
{
    public class ViewModelBase : Livet.ViewModel, INotifyPropertyChanged
    {
        public ViewModelBase()
        {
        }
    }

    //Commandに引数を追加
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Relays commands to other objects by invoking a delegate. The default value of the CanExecute method is 'true'.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        #region fields

        private readonly Action<T> _execute;
        private readonly Predicate<object> _canExecute;

        #endregion fields

        #region Constructor

        /// <summary>
        /// Create a command that does not check whether it can be executed or not
        /// </summary>
        /// <param name="execute"></param>
        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Create a command
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="canExecute"></param>
        public RelayCommand(Action<T> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("param: execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion Constructor

        #region ICommand Members

        /// <summary>
        /// Determines whether this command can be executed in the current state.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        /// <summary>
        /// An event that occurs when something changes that affects whether a command should be executed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// The method that is called when the command is started.
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        #endregion ICommand Members
    }
}