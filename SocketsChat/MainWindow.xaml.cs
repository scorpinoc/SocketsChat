using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using SocketsChat.Annotations;

// todo ! refactoring 

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Static

        private static bool TryGetValueFrom<T>([NotNull] Window window, [NotNull] string propertyName, out T value)
            where T : class
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (string.IsNullOrEmpty(propertyName))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or empty", nameof(propertyName));

            if (window.ShowDialog() == false)
            {
                value = null;
                return false;
            }
            value = window.GetType().GetProperty(propertyName).GetValue(window) as T;
            return true;
        }

        #endregion

        #region Fields

        private IPEndPoint _connectEndPoint;
        private string _nickname;

        #endregion

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        #region Auto 

        public string ServerAdress { get; private set; }

        public ICommand SendCommand { get; }

        public ICommand ConnectCommand { get; }

        public ObservableCollection<Message> Messages { get; }
        public ObservableCollection<Message> PendingMessages { get; }

        public ICommand ChooseNicknameCommand { get; }

        private uint MessageCounter { get; set; }

        #endregion

        #region Delegates

        public IPEndPoint ConnectEndPoint
        {
            get { return _connectEndPoint; }
            set
            {
                if (Equals(value, _connectEndPoint)) return;
                _connectEndPoint = value;
                OnPropertyChanged();
            }
        }

        public string Nickname
        {
            get { return _nickname; }
            private set
            {
                if (value == _nickname) return;
                _nickname = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #endregion

        #region Constructors

        public MainWindow()
        {
            TryOpenServer();

            SendCommand = DelegateCommand.CreateCommand(SendMessage,
                () => ConnectEndPoint != null && !string.IsNullOrWhiteSpace(Nickname), this);

            ConnectCommand = DelegateCommand.CreateCommand(TryConnect, () => ConnectEndPoint == null, this);

            Messages = new ObservableCollection<Message>();
            PendingMessages = new ObservableCollection<Message>();

            Nickname = string.Empty;
            ChooseNicknameCommand = DelegateCommand.CreateCommand(TryChooseNickname,
                () => string.IsNullOrWhiteSpace(Nickname), this);

            InitializeComponent();
        }

        #endregion

        #region Methods

        #region Private

        private void TryChooseNickname()
        {
            try
            {
                string nickname;
                var valueGet = TryGetValueFrom(new NicknameChooseWindow("Guest #" + new Random().Next(1, 1000)),
                    nameof(NicknameChooseWindow.Nickname), out nickname);
                if (!valueGet) return;
                Nickname = nickname;
                ServerMessage($"Nickname set as {Nickname}");
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
        }

        private void ServerMessage(string messageText)
            => Messages.Add(new Message(MessageCounter++, "Server", messageText)
            {
                RecieveTime = DateTime.Now
            });

        private void SendMessage()
        {
            var message = new Message(MessageCounter++, Nickname, MessageText.Text);
            MessageText.Clear();
            PendingMessages.Add(message);
            Send(message);
        }

        private void TryOpenServer()
        {
            try
            {
                IPEndPoint serverAdress;
                var getServerAdress = TryGetValueFrom(new IPEndPointRequestWindow("Enter your server IP and Port"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out serverAdress);

                if (!getServerAdress) Close();
                ThreadPool.QueueUserWorkItem(state => OpenServer(serverAdress));
                ServerAdress = serverAdress.ToString();
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
        }

        private void TryConnect()
        {
            try
            {
                IPEndPoint connectEndPoint;
                var valueGet = TryGetValueFrom(new IPEndPointRequestWindow("Enter server for chatting"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out connectEndPoint);

                if (!valueGet) return;

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                    socket.Connect(connectEndPoint);
                ConnectEndPoint = connectEndPoint;
                ServerMessage($"Succesfully connected to {ConnectEndPoint}");
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void OpenServer(EndPoint endPoint)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Bind(endPoint);
                    socket.Listen(10);

                    InvokeInMainThread(() => ServerMessage($"Server opened on {endPoint}"));

                    while (true)
                        AcceptMessageFrom(socket);
                }
            }
            catch (SocketException e)
            {
                MessageBoxWith(e.Message);
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    InvokeInMainThread(Close);
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
        }

        private void MessageBoxWith(string text, string caption = "Error")
            => InvokeInMainThread(() => MessageBox.Show(this, "Error occupied:\n" + text, caption));

        private void AcceptMessageFrom(Socket socket)
        {
            using (var accept = socket.Accept())
            {
                int count;
                var bytes = new byte[4096];
                var messageText = string.Empty;

                while ((count = accept.Receive(bytes)) > 0)
                    if (count == 4096)
                        messageText += Encoding.UTF8.GetString(bytes);
                    else
                    {
                        messageText += Encoding.UTF8.GetString(bytes.Take(count).ToArray());
                        break;
                    }

                if (!string.IsNullOrEmpty(messageText))
                    AnalyzeRecievedMessage(messageText);
            }
        }

        private void AnalyzeRecievedMessage(string messageText)
        {
            switch (JsonConvert.DeserializeObject<TypeWrapper>(messageText).Type)
            {
                case nameof(Message):
                {
                    var message = JsonConvert.DeserializeObject<TypeWrapper<Message>>(messageText).Obj;
                    message.RecieveTime = DateTime.Now;
                    InvokeInMainThread(() => Messages.Add(message));
                    var answer = new Answer(message.Number, DateTime.Now);
                    Send(answer);
                    break;
                }
                case nameof(Answer):
                {
                    var answer = JsonConvert.DeserializeObject<TypeWrapper<Answer>>(messageText).Obj;
                    InvokeInMainThread(() =>
                    {
                        var message = PendingMessages.First(msg => msg.Number == answer.Number);
                        PendingMessages.Remove(message);
                        message.RecieveTime = answer.AnswerTime;
                        Messages.Add(message);
                    });
                    break;
                }
                default:
                    throw new TypeAccessException("Type not implemented");
            }
        }

        private void Send<T>(T message)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Connect(ConnectEndPoint);
                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TypeWrapper<T>(message))));
                }
            }
            catch (Exception e)
            {
                MessageBoxWith(e.Message);
            }
        }

        #endregion

        #endregion

        #region Inner Types

        private class TypeWrapper
        {
            public string Type { get; }

            // ReSharper disable once MemberCanBeProtected.Local
            public TypeWrapper([NotNull] string type)
            {
                if (string.IsNullOrEmpty(type))
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentException("Argument is null or empty", nameof(type));
                Type = type;
            }
        }

        private class TypeWrapper<T> : TypeWrapper
        {
            public T Obj { get; }

            public TypeWrapper([NotNull] T obj, string type = null)
                : base(string.IsNullOrEmpty(type)
                    ? obj.GetType().Name
                    : type)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
                Obj = obj;
            }
        }

        public class Message
        {
            public uint Number { get; }
            public string NickName { get; }
            public DateTime? RecieveTime { get; set; }
            public string MessageText { get; }

            public Message(uint number, string nickName, string messageText)
            {
                Number = number;
                NickName = nickName;
                MessageText = messageText;
            }
        }

        private class Answer
        {
            public uint Number { get; }
            public DateTime AnswerTime { get; }

            public Answer(uint number, DateTime answerTime)
            {
                Number = number;
                AnswerTime = answerTime;
            }
        }

        #endregion
    }
}