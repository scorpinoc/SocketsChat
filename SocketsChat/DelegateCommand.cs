using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Input;

// ReSharper disable UnusedMember.Global

namespace SocketsChat
{
    // todo create complex command

    public static class DelegateCommand
    {
        public static ICommand CreateCommand(Action execute,
            Func<bool> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new VoidDelegateCommand(execute, canExecute, notifier);

        public static ICommand CreateCommand<T>(Action<T> execute,
            Predicate<T> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new SingleParamDelegateCommand<T>(execute, canExecute, notifier);

        public static ICommand CreateCommand<T1, T2>(Action<T1, T2> execute,
            Func<T1, T2, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParamDelegateCommand(execute, canExecute ?? ((arg1, arg2) => true), notifier,
                    canExecuteWithNullParameter);

        public static ICommand CreateCommand<T1, T2, T3>(Action<T1, T2, T3> execute,
            Func<T1, T2, T3, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParamDelegateCommand(execute, canExecute ?? ((arg1, arg2, arg3) => true), notifier,
                    canExecuteWithNullParameter);

        #region inner classes

        #region abstract

        private abstract class DelegateCommandBase : ICommand
        {
            private static bool CheckParametersTypesFor(Delegate executeAction, Delegate canExecutePredicate)
            {
                var executeParameters = executeAction.Method.GetParameters();
                var canExecuteparameters = canExecutePredicate.Method.GetParameters();
                return
                    executeParameters.Select(info => info.ParameterType).
                                      Except(canExecuteparameters.Select(info => info.ParameterType)).
                                      Any();
            }

            private static void ConstructorCheck(Delegate executeAction, Delegate canExecutePredicate)
            {
                if (executeAction == null)
                    throw new ArgumentNullException(nameof(executeAction));

                Debug.Assert(canExecutePredicate != null, $"{nameof(canExecutePredicate)} != null");
                Debug.Assert(canExecutePredicate.Method.ReturnType == typeof (bool),
                    $"{nameof(canExecutePredicate)}.Method.ReturnType == typeof(bool)");
                Debug.Assert(
                    executeAction.Method.GetParameters().Length == canExecutePredicate.Method.GetParameters().Length,
                    $"{nameof(executeAction)} Parameters.Length == {nameof(canExecutePredicate)} Parameters.Length");
                Debug.Assert(!CheckParametersTypesFor(executeAction, canExecutePredicate),
                    $"{nameof(executeAction)} and {nameof(canExecutePredicate)} have different parameters types");
            }

            #region Properties

            public event EventHandler CanExecuteChanged;

            protected Delegate CanExecutePredicate { get; }
            protected Delegate ExecuteAction { get; }

            #endregion

            protected DelegateCommandBase(Delegate executeAction,
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
                        currentContext.Post(state => ((DelegateCommandBase) state).OnCanExecuteChanged(), this);
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

            private void OnCanExecuteChanged()
                => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            #endregion
        }

        #endregion

        #region classes

        private sealed class VoidDelegateCommand : DelegateCommandBase
        {
            public VoidDelegateCommand(Action execute,
                Func<bool> canExecute = null,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute ?? (() => true), notifier)
            {
            }

            protected override bool CanExecuteImplementation(object parameter)
                => parameter == null && (bool) CanExecutePredicate.DynamicInvoke();

            protected override void ExecuteImplementation(object parameter)
                => ExecuteAction.DynamicInvoke();
        }

        private sealed class SingleParamDelegateCommand<T> : DelegateCommandBase
        {
            public SingleParamDelegateCommand(Action<T> execute,
                Predicate<T> canExecute = null,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute ?? (arg => true), notifier)
            {
            }

            protected override bool CanExecuteImplementation(object parameter)
                => (bool) CanExecutePredicate.DynamicInvoke(parameter);

            protected override void ExecuteImplementation(object parameter)
                => ExecuteAction.DynamicInvoke(parameter);
        }

        private sealed class MultiParamDelegateCommand : DelegateCommandBase
        {
            #region Properties

            private bool CanExecuteWithNullParameter { get; }

            #endregion

            public MultiParamDelegateCommand(Delegate executeAction,
                Delegate canExecutePredicate,
                INotifyPropertyChanged notifier = null,
                bool canExecuteWithNullParameter = true)
                : base(executeAction, canExecutePredicate, notifier)
            {
                CanExecuteWithNullParameter = canExecuteWithNullParameter;
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter)
                => (CanExecuteWithNullParameter && parameter == null) ||
                   (parameter != null && (bool) CanExecutePredicate.DynamicInvoke((object[]) parameter));

            protected override void ExecuteImplementation(object parameter)
            {
                if (parameter != null)
                    ExecuteAction.DynamicInvoke((object[]) parameter);
            }

            #endregion
        }

        #endregion

        #endregion
    }
}