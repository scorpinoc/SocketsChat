using System;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
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

    internal static class CommandCanExecuteNullParameterInvoke
    {
        internal static bool Return(this CanExecuteNullParameterInvoke canExecuteNullParameterInvoke, string parameter)
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
    }
}