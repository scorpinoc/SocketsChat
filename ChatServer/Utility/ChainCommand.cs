using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class ChainCommand
    {
        public static ICommand CreateCommand<T1, T2>(ICommand nextCommand,
            Func<T1, T2, object> execute,
            Func<T1, T2, object> canExecutePapametersConvertor = null,
            Func<T1, T2, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(nextCommand, execute,
                    canExecutePapametersConvertor ?? ((arg1, arg2) => new object[] {arg1, arg2}),
                    canExecute ?? ((arg1, arg2) => true), notifier,
                    canExecuteWithNullParameter);

        #region Nested Types

        private abstract class ChainCommandBase : CommandBase
        {
            private static void ConstructorCheck(ICommand nextCommand,
                Delegate execute,
                Delegate canExecutePredicateConvertor,
                Delegate canExecutePredicate)
            {
                if (nextCommand == null) throw new ArgumentNullException(nameof(nextCommand));

                Debug.Assert(execute.Method.ReturnType == typeof (object),
                    $"{nameof(execute)} .Method.ReturnType == typeof(object)");
                Debug.Assert(canExecutePredicateConvertor.Method.ReturnType == typeof (object),
                    $"{nameof(canExecutePredicateConvertor)}.Method.ReturnType == typeof(object)");
                Debug.Assert(ParametersListIsEqualFor(canExecutePredicate, canExecutePredicateConvertor),
                    $"{nameof(canExecutePredicate)} and {nameof(canExecutePredicateConvertor)} have different parameters types");
            }

            #region Properties

            protected ICommand NextCommand { get; }
            // todo change to IMultiValueConverter
            protected Delegate CanExecutePapametersConvertor { get; }

            #endregion

            protected ChainCommandBase(ICommand next,
                Delegate execute,
                Delegate canExecutePapametersConvertor,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute, notifier)
            {
                ConstructorCheck(next, execute, canExecutePapametersConvertor, canExecute);

                NextCommand = next;
                next.CanExecuteChanged += (sender, args) => OnCanExecuteChanged();
                
                CanExecutePapametersConvertor = canExecutePapametersConvertor;
            }
        }

        private sealed class MultiParametersCommand : ChainCommandBase
        {
            #region Properties

            private bool CanExecuteWithNullParameter { get; }

            #endregion

            public MultiParametersCommand(ICommand next,
                Delegate execute,
                Delegate canExecutePredicateConvertor,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null,
                bool canExecuteWithNullParameter = true)
                : base(next, execute, canExecutePredicateConvertor, canExecute, notifier)
            {
                CanExecuteWithNullParameter = canExecuteWithNullParameter;
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter)
            {
                if (CanExecuteWithNullParameter && parameter == null) return true;

                var parameters = (object[]) parameter;

                var canExecure = (bool) CanExecutePredicate.DynamicInvoke(parameters);
                var nextCommandCanExecute =
                    NextCommand.CanExecute(CanExecutePapametersConvertor.DynamicInvoke(parameters));
                return canExecure && nextCommandCanExecute;
            }

            protected override void ExecuteImplementation(object parameter)
                => NextCommand.Execute(ExecuteAction.DynamicInvoke((object[]) parameter));

            #endregion
        }

        #endregion
    }
}