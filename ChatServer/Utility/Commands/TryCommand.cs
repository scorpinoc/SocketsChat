using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    /// <summary>
    ///     Provides factory methods for wrapping <see cref="ICommand" /> methods in ry-catch-finally blocks.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class TryCommand
    {
        #region Static

        // todo catch null value to ignore exception
        /// <summary>
        ///     Creates <see cref="ICommand" /> whrere <paramref name="command" /> <seealso cref="ICommand.Execute" /> and
        ///     <seealso cref="ICommand.CanExecute" /> methods wrapped in try-catch-finally block.
        /// </summary>
        /// <param name="command"><see cref="ICommand" /> to wrap in try-catch-finally.</param>
        /// <param name="executeCatch">Action for catch block of <seealso cref="ICommand.Execute" /> method.</param>
        /// <param name="canExecuteCatch">
        ///     Function for catch block of <seealso cref="ICommand.CanExecute" /> method. Returns
        ///     <see cref="bool" />.
        /// </param>
        /// <param name="executeFinally">
        ///     Action for finally block of <seealso cref="ICommand.Execute" /> method. Argument types
        ///     must be the same as in original command.
        /// </param>
        /// <param name="canExecuteFinally">
        ///     >Action for finally block of <seealso cref="ICommand.CanExecute" /> method. Argument
        ///     types must be the same as in original command.
        /// </param>
        public static ICommand CreateCommand<T1, T2>(ICommand command,
            Action<Exception> executeCatch,
            Func<Exception, bool> canExecuteCatch,
            Action<T1, T2> executeFinally = null,
            Action<T1, T2> canExecuteFinally = null)
            =>
                new MultiParametersCommand(command, new ExecuteCatchWrapper(executeCatch, executeFinally),
                    new CanExecuteCatchWrapper(canExecuteCatch, canExecuteFinally));

        #endregion

        #region Nested Types

        private class FinallyWrapper
        {
            #region Properties

            internal Delegate Finally { get; }

            #endregion

            protected FinallyWrapper(Delegate @finally)
            {
                Finally = @finally;
            }
        }

        private class ExecuteCatchWrapper : FinallyWrapper
        {
            #region Properties

            internal Action<Exception> Catch { get; }

            #endregion

            public ExecuteCatchWrapper(Action<Exception> @catch, Delegate @finally = null) : base(@finally)
            {
                if (@catch == null) throw new ArgumentNullException(nameof(@catch));
                Catch = @catch;
            }
        }

        private class CanExecuteCatchWrapper : FinallyWrapper
        {
            #region Properties

            internal Func<Exception, bool> Catch { get; }

            #endregion

            public CanExecuteCatchWrapper(Func<Exception, bool> @catch, Delegate @finally = null) : base(@finally)
            {
                if (@catch == null) throw new ArgumentNullException(nameof(@catch));
                Catch = @catch;
            }
        }

        private abstract class TryCommandBase : ICommand
        {
            #region Properties

            public event EventHandler CanExecuteChanged;

            private ICommand Command { get; }

            private ExecuteCatchWrapper ExecuteCatchWrapper { get; }
            private CanExecuteCatchWrapper CanExecuteCatchWrapper { get; }

            #endregion

            protected TryCommandBase(ICommand command,
                ExecuteCatchWrapper executeCatchWrapper,
                CanExecuteCatchWrapper canExecuteCatchWrapper)
            {
                if (command == null) throw new ArgumentNullException(nameof(command));
                if (executeCatchWrapper == null) throw new ArgumentNullException(nameof(executeCatchWrapper));
                if (canExecuteCatchWrapper == null) throw new ArgumentNullException(nameof(canExecuteCatchWrapper));

                Command = command;
                ExecuteCatchWrapper = executeCatchWrapper;
                CanExecuteCatchWrapper = canExecuteCatchWrapper;
                Command.CanExecuteChanged += (sender, args) => CanExecuteChanged?.Invoke(this, args);
            }

            #region Methods

            public bool CanExecute(object parameter)
            {
                try
                {
                    return Command.CanExecute(parameter);
                }
                catch (Exception e)
                {
                    return CanExecuteCatchWrapper.Catch(e);
                }
                finally
                {
                    Finally(CanExecuteCatchWrapper, parameter);
                }
            }

            public void Execute(object parameter)
            {
                try
                {
                    if (CanExecute(parameter))
                        Command.Execute(parameter);
                }
                catch (Exception e)
                {
                    ExecuteCatchWrapper.Catch(e);
                }
                finally
                {
                    Finally(ExecuteCatchWrapper, parameter);
                }
            }

            private void Finally(FinallyWrapper finallyWrapper, object parameter)
            {
                if (finallyWrapper.Finally != null)
                    FinallyImplementation(finallyWrapper, parameter);
            }

            protected abstract void FinallyImplementation(FinallyWrapper finallyWrapper, object parameter);

            #endregion
        }

        private class MultiParametersCommand : TryCommandBase
        {
            public MultiParametersCommand(ICommand command,
                ExecuteCatchWrapper executeCatchWrapper,
                CanExecuteCatchWrapper canExecuteCatchWrapper)
                : base(command, executeCatchWrapper, canExecuteCatchWrapper)
            {
            }

            #region Methods

            protected override void FinallyImplementation(FinallyWrapper finallyWrapper, object parameter)
                => finallyWrapper.Finally.DynamicInvoke((object[]) parameter);

            #endregion
        }

        #endregion
    }
}