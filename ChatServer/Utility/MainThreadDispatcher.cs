using System.Windows.Threading;

namespace ChatServer.Utility
{
    public static class MainThreadDispatcher
    {
        private static Dispatcher _dispatcher;

        public static Dispatcher Dispatcher
        {
            get
            {
                if (_dispatcher == null)
                    Initialize();
                return _dispatcher;
            }
        }

        public static void Initialize()
        {
            if (_dispatcher == null)
                _dispatcher = Dispatcher.CurrentDispatcher;
        }
    }
}