using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SocketsChat.Annotations;
using SocketsChat.Model;

// todo refactoring 

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private static bool TryGetValueFrom<T>([NotNull] Window window, [NotNull] string propertyName, out T value)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (string.IsNullOrEmpty(propertyName))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or empty", nameof(propertyName));

            if (window.ShowDialog() == false)
            {
                value = default(T);
                return false;
            }
            value = (T) window.GetType().GetProperty(propertyName).GetValue(window);
            return true;
        }

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand SendCommand { get; }

        public ICommand ConnectCommand { get; }

        public ICommand ChooseNicknameCommand { get; }

        private ChatServer ChatServer { get; }

        public string ServerAdress { get; private set; }

        public string ConnectEndPoint => ChatServer.ConnectAdress?.ToString();

        public string Nickname => ChatServer.Nickname;

        public IEnumerable<IMessage> Messages => ChatServer.Messages;
        public IEnumerable<IMessage> PendingMessages => ChatServer.PendingMessages;

        public bool CanSendMessage => ChatServer.CanSendMessage;

        #endregion

        public MainWindow()
        {
            ChatServer = new ChatServer();

            BindingOperations.EnableCollectionSynchronization(Messages, new object());
            BindingOperations.EnableCollectionSynchronization(PendingMessages, new object());

            // todo set to delegate only exist in MainWindow properties
            ChatServer.PropertyChanged += (sender, args) => PropertyChanged?.Invoke(this, args);

            TryOpenServer();

            SendCommand = DelegateCommand.CreateCommand(TrySendMessage, () => ChatServer.CanSendMessage, ChatServer);

            ConnectCommand = DelegateCommand.CreateCommand(TryConnect, () => ChatServer.ConnectAdress == null, ChatServer);

            ChooseNicknameCommand = DelegateCommand.CreateCommand(TrySetNickname,
                () => string.IsNullOrWhiteSpace(ChatServer.Nickname), ChatServer);

            InitializeComponent();
        }

        #region Methods

        private void TryOpenServer()
        {
            Try(() =>
            {
                IPEndPoint serverAdress;
                var getServerAdress = TryGetValueFrom(new IPEndPointRequestWindow("Enter your server IP and Port"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out serverAdress);

                if (!getServerAdress)
                    Close();

                ServerAdress = serverAdress.ToString();
                ThreadPool.QueueUserWorkItem(
                    state => Try(() => ChatServer.OpenServer(serverAdress), () => InvokeInMainThread(Close)));
            });
        }

        private void TryConnect()
        {
            Try(() =>
            {
                IPEndPoint connectEndPoint;
                var valueGet = TryGetValueFrom(new IPEndPointRequestWindow("Enter server for chatting"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out connectEndPoint);

                if (valueGet)
                    ChatServer.Connect(connectEndPoint);
            });
        }

        private void TrySetNickname()
        {
            Try(() =>
            {
                string nickname;
                var valueGet = TryGetValueFrom(new NicknameChooseWindow("Guest #" + new Random().Next(1, 1000)),
                    nameof(NicknameChooseWindow.Nickname), out nickname);
                if (valueGet) ChatServer.SetNickname(nickname);
            });
        }

        private void TrySendMessage()
        {
            Try(() =>
            {
                var message = MessageText.Text;
                MessageText.Clear();
                ChatServer.SendMessage(message);
            });
        }

        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void MessageBoxWith(string text, string caption = "Error")
            => InvokeInMainThread(() => MessageBox.Show(this, "Event occupied:\n" + text, caption));

        private void Try([NotNull] Action action, [CanBeNull] Action finallyAction = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
            finally
            {
                finallyAction?.Invoke();
            }
        }

        #endregion
    }
}