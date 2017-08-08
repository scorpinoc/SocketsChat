using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    /// <summary>
    ///     Provides constructor for wrapping methods in <see cref="ICommand" /> interface.
    /// </summary>
    /// <example>
    /// <code>
    /// <see cref="DelegateCommand" />.CreateCommand&lt;<see cref="string" />&gt;(<see cref="Console" />.WriteLine, s => s != null);
    /// </code>
    /// </example>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class DelegateCommand
    {
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
        /// <param name="canExecuteWithNullParameter">
        ///     If <see cref="ICommand.CanExecute" /> parameter is null returns
        ///     <paramref name="canExecuteWithNullParameter" /> value.
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2>(Action<T1, T2> execute,
            Func<T1, T2, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2) => true), notifier,
                    canExecuteWithNullParameter);

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
        /// <param name="canExecuteWithNullParameter">
        ///     If <see cref="ICommand.CanExecute" /> parameter is null returns
        ///     <paramref name="canExecuteWithNullParameter" /> value.
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2, T3>(Action<T1, T2, T3> execute,
            Func<T1, T2, T3, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2, arg3) => true), notifier,
                    canExecuteWithNullParameter);

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
        /// <param name="canExecuteWithNullParameter">
        ///     If <see cref="ICommand.CanExecute" /> parameter is null returns
        ///     <paramref name="canExecuteWithNullParameter" /> value.
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public static ICommand CreateCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> execute,
            Func<T1, T2, T3, T4, bool> canExecute = null,
            INotifyPropertyChanged notifier = null,
            bool canExecuteWithNullParameter = true)
            =>
                new MultiParametersCommand(execute, canExecute ?? ((arg1, arg2, arg3, arg4) => true), notifier,
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
            #region Properties

            // todo enum
            private bool CanExecuteWithNullParameter { get; }

            #endregion

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