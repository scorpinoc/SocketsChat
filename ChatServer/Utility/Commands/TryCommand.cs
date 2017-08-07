using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class TryCommand
    {
        public static ICommand CreateCommand<T1, T2>(ICommand command,
            Action<Exception> @executeCatch,
            Func<Exception, bool> @canExecuteCatch,
            Action<T1, T2> @finally = null) => new MultiParametersCommand(command, @executeCatch, @canExecuteCatch, @finally);

        #region Nested Types

        private abstract class TryCommandBase : ICommand
        {
            #region Properties

            public event EventHandler CanExecuteChanged;

            private ICommand Command { get; }
            private Action<Exception> ExecuteCatch { get; }
            private Func<Exception, bool> CanExecuteCatch { get; }
            protected Delegate Finally { get; }

            #endregion

            protected TryCommandBase(ICommand command,
                Action<Exception> @executeCatch,
                Func<Exception, bool> @canExecuteCatch,
                Delegate @finally = null
                )
            {
                if (command == null) throw new ArgumentNullException(nameof(command));
                if (executeCatch == null) throw new ArgumentNullException(nameof(executeCatch));
                if (canExecuteCatch == null) throw new ArgumentNullException(nameof(canExecuteCatch));

                Command = command;
                ExecuteCatch = @executeCatch;
                CanExecuteCatch = @canExecuteCatch;
                Finally = @finally;

                command.CanExecuteChanged += (sender, args) => CanExecuteChanged?.Invoke(this, args);
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
                    return CanExecuteCatch(e);
                }
                finally
                {
                    if (Finally != null)
                        FinallyImplementation(parameter);
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
                    ExecuteCatch(e);
                }
                finally
                {
                    if (Finally != null)
                        FinallyImplementation(parameter);
                }
            }

            protected abstract void FinallyImplementation(object parameter);

            #endregion
        }

        private class MultiParametersCommand : TryCommandBase
        {
            public MultiParametersCommand(ICommand command,
                Action<Exception> executeCatch,
                Func<Exception, bool> canExecuteCatch,
                Delegate @finally = null) : base(command, executeCatch, canExecuteCatch, @finally)
            {
            }

            protected override void FinallyImplementation(object parameter)
                => Finally.DynamicInvoke((object[]) parameter);
        }

        #endregion
    }
}