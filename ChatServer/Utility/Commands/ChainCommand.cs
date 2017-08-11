using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    /// <summary>
    ///     Provides factory methods for creating <see cref="ICommand" /> wich delegates it's results to the next command in
    ///     chain.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class ChainCommand
    {
        #region Static

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates it's results to the <paramref name="nextCommand" /> in chain.
        /// </summary>
        /// <param name="nextCommand">
        ///     Next <see cref="ICommand" /> in chain. Results from <paramref name="execute" /> and
        ///     <paramref name="canExecute" /> uses as parameters of this command.
        ///     <para />
        ///     <see cref="ICommand.CanExecuteChanged" /> of <paramref name="nextCommand" /> delegates to new command.
        /// </param>
        /// <param name="execute">
        ///     Function executed on use of <see cref="ICommand.Execute" /> method. Returned
        ///     <see cref="object" /> used as paramerer for <see cref="ICommand.Execute" /> of <paramref name="nextCommand" />.
        /// </param>
        /// <param name="canExecute">
        ///     Function executed on use of <see cref="ICommand.CanExecute" /> method. Returned
        ///     <see cref="bool" /> used as part of return value. Returned <see cref="object" /> used as paramerer for
        ///     <see cref="ICommand.CanExecute" /> of <paramref name="nextCommand" />.
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <param name="canExecuteNullParameterInvoke">
        ///     If <see cref="ICommand.CanExecute" /> parameter is <c>null</c> method reacts considered to
        ///     <paramref name="canExecuteNullParameterInvoke" /> value
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2>(ICommand nextCommand,
            Func<T1, T2, object> execute,
            Func<T1, T2, Tuple<bool, object>> canExecute = null,
            INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
            =>
                new MultiParametersCommand(nextCommand, execute,
                    canExecute ?? ((arg1, arg2) => new Tuple<bool, object>(true, new object[] {arg1, arg2})),
                    notifier, canExecuteNullParameterInvoke);

        #endregion

        #region Nested Types

        private abstract class ChainCommandBase : CommandBase
        {
            #region Static

            private static void ConstructorCheck(ICommand nextCommand,
                Delegate execute)
            {
                if (nextCommand == null) throw new ArgumentNullException(nameof(nextCommand));

                Debug.Assert(execute.Method.ReturnType == typeof (object),
                    $"{nameof(execute)} .Method.ReturnType == typeof(object)");
            }

            #endregion

            #region Properties

            protected ICommand NextCommand { get; }

            #endregion

            protected ChainCommandBase(ICommand next,
                Delegate execute,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute, notifier, typeof (Tuple<bool, object>))
            {
                ConstructorCheck(next, execute);

                NextCommand = next;
                next.CanExecuteChanged += (sender, args) => OnCanExecuteChanged();
            }
        }

        private sealed class MultiParametersCommand : ChainCommandBase
        {
            #region Properties

            private CanExecuteNullParameterInvoke CanExecuteNullParameterInvoke { get; }

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
                => NextCommand.Execute(ExecuteAction.DynamicInvoke((object[]) parameter));

            #endregion
        }

        #endregion
    }
}