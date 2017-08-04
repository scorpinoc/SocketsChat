using System.Windows.Threading;

namespace SocketsChat
{
    public static class MainDispatcher
    {
        private static Dispatcher _dispatcher;

        public static Dispatcher Dispatcher
        {
            get { return _dispatcher ?? (_dispatcher = Dispatcher.CurrentDispatcher); }
            private set { _dispatcher = value; }
        }

        public static void Initialize()
        {
            if (Dispatcher == null)
                Dispatcher = Dispatcher.CurrentDispatcher;
        }
    }
}