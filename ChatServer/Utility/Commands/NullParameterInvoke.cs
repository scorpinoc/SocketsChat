using System;
using System.Windows.Input;

namespace ChatServer.Utility.Commands
{
    /// <summary>
    ///     Sets action in case of invoking <see cref="ICommand.CanExecute" /> with <c>null</c> parameter when expectiong for
    ///     <c><see cref="object" />[]</c>
    /// </summary>
    public enum NullParameterInvoke
    {
        /// <summary />
        ReturnTrue,

        /// <summary />
        ReturnFalse,

        /// <summary />
        ThrowException
    }

    internal static class CommandCanExecuteNullParameter
    {
        internal static bool Return(this NullParameterInvoke nullParameterInvoke, string paramName)
        {
            switch (nullParameterInvoke)
            {
                case NullParameterInvoke.ReturnTrue:
                    return true;
                case NullParameterInvoke.ReturnFalse:
                    return false;
                case NullParameterInvoke.ThrowException:
                    throw new ArgumentNullException(paramName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(NullParameterInvoke));
            }
        }
    }
}