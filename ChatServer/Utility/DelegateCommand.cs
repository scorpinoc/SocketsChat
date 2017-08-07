using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class DelegateCommand
    {
        public static ICommand CreateCommand(Action execute,
            Func<bool> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new ParameterlessCommand(execute, canExecute, notifier);

        public static ICommand CreateCommand<T>(Action<T> execute,
            Predicate<T> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new SingleParameterCommand<T>(execute, canExecute, notifier);

        public static ICommand CreateCommand<T1, T2>(Action<T1, T2> execute,
            Func<T1, T2, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2) => true), notifier,
                    canExecuteWithNullParameter);

        public static ICommand CreateCommand<T1, T2, T3>(Action<T1, T2, T3> execute,
            Func<T1, T2, T3, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2, arg3) => true), notifier,
                    canExecuteWithNullParameter);

        #region Nested Types

        private sealed class ParameterlessCommand : CommandBase
        {
            public ParameterlessCommand(Action execute,
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

        private sealed class SingleParameterCommand<T> : CommandBase
        {
            public SingleParameterCommand(Action<T> execute,
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

        private sealed class MultiParametersCommand : CommandBase
        {
            private bool CanExecuteWithNullParameter { get; }

            public MultiParametersCommand(Delegate execute,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null,
                bool canExecuteWithNullParameter = true)
                : base(execute, canExecute, notifier)
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

    }
}