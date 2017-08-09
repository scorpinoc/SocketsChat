using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    // todo rework summary
    /// <summary>
    ///     Provides factory methods for creating <see cref="ICommand" /> implementations delegating their methods to
    ///     delegates.
    /// </summary>
    /// <example>
    ///     <code>
    /// <see cref="DelegateCommand" />.CreateCommand&lt;<see cref="string" />&gt;(<see cref="Console" />.WriteLine, s => s != null);
    /// </code>
    /// </example>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class DelegateCommand
    {
        #region Static

        /// <summary>
        ///     Sets action in case of invoking <see cref="ICommand.CanExecute" /> with <c>null</c> parameter when expectiong for
        ///     <c><see cref="object" />[]</c>
        /// </summary>
        public enum CanExecuteNullParameterInvoke
        {
            /// <summary />
            ReturnTrue,

            /// <summary />
            ReturnFalse,

            /// <summary />
            ThrowException
        }

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates their methods to parameters.
        /// </summary>
        /// <param name="execute">
        ///     <see cref="Action" /> executed on use of <see cref="ICommand.Execute" /> method.
        ///     <para />
        ///     Can't be <c>null</c>.
        /// </param>
        /// <param name="canExecute">
        ///     Func&lt;<see cref="bool" />&gt; executed on use of <see cref="ICommand.CanExecute" /> method.
        ///     <para />
        ///     if <paramref name="canExecute" /> is <c>null</c> - set as
        ///     <para />
        ///     <c>() => true</c>
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand(Action execute,
            Func<bool> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new ParameterlessCommand(execute, canExecute, notifier);

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates their methods to parameters.
        /// </summary>
        /// <param name="execute">
        ///     <see cref="Action" />&lt;<typeparamref name="T" />&gt; executed on use of <see cref="ICommand.Execute" /> method.
        ///     <para />
        ///     Can't be <c>null</c>.
        /// </param>
        /// <param name="canExecute">
        ///     Func&lt;<typeparamref name="T" />, <see cref="bool" />&gt; executed on use of <see cref="ICommand.CanExecute" />
        ///     method.
        ///     <para />
        ///     if <paramref name="canExecute" /> is <c>null</c> - set as
        ///     <para />
        ///     <c>arg => true</c>
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T>(Action<T> execute,
            Predicate<T> canExecute = null,
            INotifyPropertyChanged notifier = null)
            => new SingleParameterCommand<T>(execute, canExecute, notifier);

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates their methods to parameters.
        /// </summary>
        /// <param name="execute">
        ///     <see cref="Action" />&lt;<typeparamref name="T1" />, <typeparamref name="T2" />&gt; executed on use of
        ///     <see cref="ICommand.Execute" /> method.
        ///     <para />
        ///     Can't be <c>null</c>.
        /// </param>
        /// <param name="canExecute">
        ///     Func&lt;<typeparamref name="T1" />, <typeparamref name="T2" />, <see cref="bool" />&gt; executed on use of
        ///     <see cref="ICommand.CanExecute" /> method.
        ///     <para />
        ///     if <paramref name="canExecute" /> is <c>null</c> - set as
        ///     <para />
        ///     <c>(arg1, arg2) => true</c>
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <param name="canExecuteNullParameterInvoke">
        ///     If <see cref="ICommand.CanExecute" /> parameter is <c>null</c> method reacts considered to
        ///     <paramref name="canExecuteNullParameterInvoke" /> value
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2>(Action<T1, T2> execute,
            Func<T1, T2, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2) => true), notifier,
                    canExecuteNullParameterInvoke);

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates their methods to parameters.
        /// </summary>
        /// <param name="execute">
        ///     <see cref="Action" />&lt;<typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />&gt;
        ///     executed on use of <see cref="ICommand.Execute" /> method.
        ///     <para />
        ///     Can't be <c>null</c>.
        /// </param>
        /// <param name="canExecute">
        ///     Func&lt;<typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />, <see cref="bool" />&gt;
        ///     executed on use of <see cref="ICommand.CanExecute" /> method.
        ///     <para />
        ///     if <paramref name="canExecute" /> is <c>null</c> - set as
        ///     <para />
        ///     <c>(arg1, arg2, arg3) => true</c>
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <param name="canExecuteNullParameterInvoke">
        ///     If <see cref="ICommand.CanExecute" /> parameter is <c>null</c> method reacts considered to
        ///     <paramref name="canExecuteNullParameterInvoke" /> value
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2, T3>(Action<T1, T2, T3> execute,
            Func<T1, T2, T3, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2, arg3) => true), notifier,
                    canExecuteNullParameterInvoke);

        /// <summary>
        ///     Creates <see cref="ICommand" /> wich delegates their methods to parameters.
        /// </summary>
        /// <param name="execute">
        ///     <see cref="Action" />&lt;<typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />,
        ///     <typeparamref name="T4" />&gt;
        ///     executed on use of <see cref="ICommand.Execute" /> method.
        ///     <para />
        ///     Can't be <c>null</c>.
        /// </param>
        /// <param name="canExecute">
        ///     Func&lt;<typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />,
        ///     <typeparamref name="T4" />, <see cref="bool" />&gt;
        ///     executed on use of <see cref="ICommand.CanExecute" /> method.
        ///     <para />
        ///     if <paramref name="canExecute" /> is <c>null</c> - set as
        ///     <para />
        ///     <c>(arg1, arg2, arg3, arg4)) => true</c>
        /// </param>
        /// <param name="notifier">Notifier to subscribe for <see cref="ICommand.CanExecuteChanged" /> event invoke.</param>
        /// <param name="canExecuteNullParameterInvoke">
        ///     If <see cref="ICommand.CanExecute" /> parameter is <c>null</c> method reacts considered to
        ///     <paramref name="canExecuteNullParameterInvoke" /> value
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> execute,
            Func<T1, T2, T3, T4, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
            => new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2, arg3, arg4) => true), notifier,
                    canExecuteNullParameterInvoke);

        private static bool Return(this CanExecuteNullParameterInvoke canExecuteNullParameterInvoke, string parameter)
        {
            switch (canExecuteNullParameterInvoke)
            {
                case CanExecuteNullParameterInvoke.ReturnTrue:
                    return true;
                case CanExecuteNullParameterInvoke.ReturnFalse:
                    return false;
                case CanExecuteNullParameterInvoke.ThrowException:
                    throw new ArgumentNullException(parameter);
                default:
                    throw new ArgumentOutOfRangeException(nameof(CanExecuteNullParameterInvoke));
            }
        }

        #endregion

        #region Nested Types

        private sealed class ParameterlessCommand : CommandBase
        {
            public ParameterlessCommand(Action execute,
                Func<bool> canExecute = null,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute ?? (() => true), notifier)
            {
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter)
                => parameter == null && (bool)CanExecutePredicate.DynamicInvoke();

            protected override void ExecuteImplementation(object parameter)
                => ExecuteAction.DynamicInvoke();

            #endregion
        }

        private sealed class SingleParameterCommand<T> : CommandBase
        {
            public SingleParameterCommand(Action<T> execute,
                Predicate<T> canExecute = null,
                INotifyPropertyChanged notifier = null)
                : base(execute, canExecute ?? (arg => true), notifier)
            {
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter)
                => (bool)CanExecutePredicate.DynamicInvoke(parameter);

            protected override void ExecuteImplementation(object parameter)
                => ExecuteAction.DynamicInvoke(parameter);

            #endregion
        }

        private sealed class MultiParametersCommand : CommandBase
        {
            #region Properties

            private CanExecuteNullParameterInvoke CanExecuteNullParameterInvoke { get; }

            #endregion

            public MultiParametersCommand(Delegate execute,
                Delegate canExecute,
                INotifyPropertyChanged notifier = null,
                CanExecuteNullParameterInvoke canExecuteNullParameterInvoke = CanExecuteNullParameterInvoke.ReturnTrue)
                : base(execute, canExecute, notifier)
            {
                CanExecuteNullParameterInvoke = canExecuteNullParameterInvoke;
            }

            #region Methods

            protected override bool CanExecuteImplementation(object parameter) => parameter == null
                ? CanExecuteNullParameterInvoke.Return(nameof(parameter))
                : (bool)CanExecutePredicate.DynamicInvoke((object[])parameter);

            protected override void ExecuteImplementation(object parameter)
                => ExecuteAction.DynamicInvoke((object[])parameter);

            #endregion
        }

        #endregion
    }
}