using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    internal abstract class CommandBase : ICommand
    {
        protected static bool ParametersListIsEqualFor(Delegate x, Delegate y)
        {
            var executeParameters = x.Method.GetParameters();
            var canExecuteparameters = y.Method.GetParameters();
            return
                executeParameters.Select(info => info.ParameterType).
                                  Except(canExecuteparameters.Select(info => info.ParameterType)).
                                  Any() == false;
        }

        private static void ConstructorCheck(Delegate executeAction, Delegate canExecutePredicate)
        {
            if (executeAction == null)
                throw new ArgumentNullException(nameof(executeAction));

            Debug.Assert(canExecutePredicate != null, $"{nameof(canExecutePredicate)} != null");
            Debug.Assert(canExecutePredicate.Method.ReturnType == typeof(bool),
                $"{nameof(canExecutePredicate)}.Method.ReturnType == typeof(bool)");
            Debug.Assert(
                executeAction.Method.GetParameters().Length == canExecutePredicate.Method.GetParameters().Length,
                $"{nameof(executeAction)} Parameters.Length == {nameof(canExecutePredicate)} Parameters.Length");
            Debug.Assert(ParametersListIsEqualFor(executeAction, canExecutePredicate),
                $"{nameof(executeAction)} and {nameof(canExecutePredicate)} have different parameters types");
        }

        #region Properties

        public event EventHandler CanExecuteChanged;

        protected Delegate CanExecutePredicate { get; }
        protected Delegate ExecuteAction { get; }

        #endregion

        protected CommandBase(Delegate executeAction,
            Delegate canExecutePredicate,
            INotifyPropertyChanged notifier = null)
        {
            ConstructorCheck(executeAction, canExecutePredicate);

            ExecuteAction = executeAction;

            CanExecutePredicate = canExecutePredicate;

            if (notifier == null) return;
            // MultiThread
            var currentContext = SynchronizationContext.Current;
            notifier.PropertyChanged +=
                (sender, args) =>
                    currentContext.Post(state => ((CommandBase)state).OnCanExecuteChanged(), this);
        }

        #region Methods

        public bool CanExecute(object parameter) => CanExecuteImplementation(parameter);

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            ExecuteImplementation(parameter);
            OnCanExecuteChanged();
        }

        protected abstract bool CanExecuteImplementation(object parameter);

        protected abstract void ExecuteImplementation(object parameter);

        protected void OnCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        #endregion
    }
}