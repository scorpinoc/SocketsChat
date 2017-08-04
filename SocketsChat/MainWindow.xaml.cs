using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SocketsChat.Annotations;
using SocketsChat.Models;

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
            value = (T)window.GetType().GetProperty(propertyName).GetValue(window);
            return true;
        }

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private ViewModel ViewModel { get; }

        public ICommand SendCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand ChooseNicknameCommand { get; }

        public string ServerAdress { get; private set; }

        public string ConnectEndPoint => ViewModel.ConnectAdress?.ToString();

        public string Nickname => ViewModel.Nickname;

        public IEnumerable<Message> Messages => ViewModel.Messages;
        public IEnumerable<Message> PendingMessages => ViewModel.PendingMessages;

        public bool CanSendMessage => ViewModel.CanSendMessage;

        #endregion

        public MainWindow()
        {
            ViewModel = new ViewModel();

            BindingOperations.EnableCollectionSynchronization(Messages, new object());
            BindingOperations.EnableCollectionSynchronization(PendingMessages, new object());

            ViewModel.PropertyChanged += (sender, args) => PropertyChanged?.Invoke(this, args);

            TryOpenServer();

            SendCommand = DelegateCommand.CreateCommand(TrySendMessage, () => ViewModel.SendMessageCommand.CanExecute(null), ViewModel);

            ConnectCommand = DelegateCommand.CreateCommand(TryConnect, () => ViewModel.ConnectCommand.CanExecute(null), ViewModel);

            ChooseNicknameCommand = DelegateCommand.CreateCommand(TrySetNickname,
                () => ViewModel.SetNicknameCommand.CanExecute(null), ViewModel);

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

                if (!getServerAdress || !ViewModel.OpenServerCommand.CanExecute(serverAdress))
                    Close();

                ServerAdress = serverAdress.ToString();
                ThreadPool.QueueUserWorkItem(
                    state =>
                        Try(() => ViewModel.OpenServerCommand.Execute(serverAdress), () => InvokeInMainThread(Close)));
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
                    ViewModel.ConnectCommand.Execute(connectEndPoint);
            });
        }

        private void TrySetNickname()
        {
            Try(() =>
            {
                string nickname;
                var valueGet = TryGetValueFrom(new NicknameChooseWindow("Guest #" + new Random().Next(1, 1000)),
                    nameof(NicknameChooseWindow.Nickname), out nickname);
                if (valueGet)
                    ViewModel.SetNicknameCommand.Execute(nickname);
            });
        }

        private void TrySendMessage()
        {
            Try(() =>
            {
                var message = MessageText.Text;
                MessageText.Clear();
                ViewModel.SendMessageCommand.Execute(message);
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