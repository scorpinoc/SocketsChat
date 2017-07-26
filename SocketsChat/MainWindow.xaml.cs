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

// todo refactoring 

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Fields

        private IPEndPoint _connectEndPoint;

        #endregion

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        #region Auto 

        public string ServerAdress { get; }

        public ICommand SendCommand { get; }

        public ICommand ConnectCommand { get; }

        public ObservableCollection<Message> Messages { get; }
        public ObservableCollection<Message> PendingMessages { get; }

        private uint MessageCounter { get; set; }

        #endregion

        #region Delegates

        public IPEndPoint ConnectEndPoint
        {
            get { return _connectEndPoint; }
            set
            {
                _connectEndPoint = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #endregion

        #region Constructors

        public MainWindow()
        {
            var requestWindow = new IPEndPointRequestWindow("Enter your server IP and Port");
            if (requestWindow.ShowDialog() == true)
            {
                ThreadPool.QueueUserWorkItem(state => OpenServer(requestWindow.IpEndPoint));
                ServerAdress = requestWindow.IpEndPoint.ToString();
            }
            else
                Close();

            SendCommand = DelegateCommand.CreateCommand(() =>
            {
                var message = new Message(MessageCounter++, "me", MessageText.Text);
                MessageText.Clear();
                PendingMessages.Add(message);
                SendMessage(message);
            }, () => ConnectEndPoint != null, this);

            ConnectCommand = DelegateCommand.CreateCommand(TryConnect, () => ConnectEndPoint == null, this);

            Messages = new ObservableCollection<Message>();
            PendingMessages = new ObservableCollection<Message>();

            InitializeComponent();
        }

        #endregion

        #region Methods

        #region Private

        private void TryConnect()
        {
            var requestConnectWindow = new IPEndPointRequestWindow("Enter server for chatting");
            if (requestConnectWindow.ShowDialog() == false) return;
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                    socket.Connect(requestConnectWindow.IpEndPoint);
                ConnectEndPoint = requestConnectWindow.IpEndPoint;
                Messages.Add(new Message(MessageCounter++, "Server", $"Succesfully connected to {ConnectEndPoint}")
                {
                    RecieveTime = DateTime.Now
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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

                    InvokeInMainThread(
                        () =>
                            Messages.Add(new Message(MessageCounter++, "Server", $"Server opened on {endPoint}")
                            {
                                RecieveTime = DateTime.Now
                            }));

                    while (true)
                        AcceptMessageFrom(socket);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

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
                    SendMessage(answer);
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

        private void SendMessage<T>(T message)
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
                MessageBox.Show(e.Message);
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