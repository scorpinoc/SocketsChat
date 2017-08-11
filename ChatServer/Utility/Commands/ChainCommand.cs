using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class ChainCommand
    {
        public static ICommand CreateCommand<T1, T2>(ICommand nextCommand,
            Func<T1, T2, object> execute,
            Func<T1, T2, Tuple<bool, object>> canExecute = null,
            INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
            =>
                new MultiParametersCommand(nextCommand, execute, canExecute ?? ((arg1,arg2) => new Tuple<bool, object>(true, new object[] { arg1, arg2})),
                    notifier, canExecuteNullParameterInvoke);

        #region Nested Types

        private abstract class ChainCommandBase : CommandBase
        {
            private static void ConstructorCheck(ICommand nextCommand,
                Delegate execute)
            {
                if (nextCommand == null) throw new ArgumentNullException(nameof(nextCommand));

                Debug.Assert(execute.Method.ReturnType == typeof(object),
                    $"{nameof(execute)} .Method.ReturnType == typeof(object)");
            }

            #region Properties

            protected ICommand NextCommand { get; }

            #endregion

            protected ChainCommandBase(ICommand next,
                Delegate execute,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute, notifier, typeof(Tuple<bool, object>))
            {
                ConstructorCheck(next, execute);

                NextCommand = next;
                next.CanExecuteChanged += (sender, args) => OnCanExecuteChanged();
            }
        }

        private sealed class MultiParametersCommand : ChainCommandBase
        {
            private CanExecuteNullParameterInvoke CanExecuteNullParameterInvoke { get; }

            #region Properties


            #endregion

            public MultiParametersCommand(ICommand next,
                Delegate execute,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
                : base(next, execute, canExecute, notifier)
            {
                CanExecuteNullParameterInvoke = canExecuteNullParameterInvoke;
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter)
            {
                if (parameter == null) return CanExecuteNullParameterInvoke.Return(nameof(parameter));
                var result = (Tuple<bool, object>) CanExecutePredicate.DynamicInvoke((object[]) parameter);
                return result.Item1 && NextCommand.CanExecute(result.Item2);
            }

            protected override void ExecuteImplementation(object parameter)
                => NextCommand.Execute(ExecuteAction.DynamicInvoke((object[])parameter));

            #endregion
        }

        #endregion
    }
}