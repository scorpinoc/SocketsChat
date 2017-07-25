using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        #region Auto 

        public ICommand SendCommand { get; }

        #endregion

        #endregion

        #region Constructors

        public MainWindow()
        {
            var requestWindow = new IPEndPointRequestWindow("Enter your server IP and Port");
            if (requestWindow.ShowDialog() == true)
                ThreadPool.QueueUserWorkItem(state => OpenServer(requestWindow.IpEndPoint));
            else
                Close();

            SendCommand = DelegateCommand.CreateCommand(SendMessage);

            InitializeComponent();
        }

        #endregion

        #region Methods

        #region Private

        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void OpenServer(EndPoint endPoint)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Bind(endPoint);
                    socket.Listen(10);

                    InvokeInMainThread(() => ChatBox.AppendText("Server opened\n"));

                    while (true)
                        Accept(socket);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void Accept(Socket socket)
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

                // todo ! refactor
                switch (JsonConvert.DeserializeObject<JsonType>(messageText).Type)
                {
                    case nameof(Message):
                    {
                        var message = JsonConvert.DeserializeObject<Message>(messageText);
                        message.RecieveTime = DateTime.Now;
                        AddMessage(message);
                        // todo ! delete and replace
                        using (var socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                        {
                            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse("3000"));
                            socket1.Connect(ipEndPoint);

                            var answer = new Answer(message.Number, DateTime.Now);

                            socket1.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(answer)));
                        }
                        break;
                    }
                    case nameof(Answer):
                    {
                        var answer = JsonConvert.DeserializeObject<Answer>(messageText);
                        var message = new Message(answer.Number, "server", "message recieved by client")
                                      {
                                          RecieveTime = answer.AnswerTime
                                      };
                        AddMessage(message);
                        break;
                    }
                    default:
                        throw new TypeAccessException("Type not implemented");
                }
            }
        }

        private void SendMessage()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(ConnectIp.Text), int.Parse(ConnectPort.Text));
                    socket.Connect(ipEndPoint);

                    var messageText = string.Empty;
                    InvokeInMainThread(() => messageText = MessageText.Text);
                    var message = new Message(0, "me", messageText);

                    AddMessage(message);

                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void AddMessage(Message message)
        {
            var date = "(" + (message.RecieveTime?.ToLongTimeString() ?? "sending...") + ")";
            InvokeInMainThread(() => ChatBox.AppendText($"{date} {message.NickName}: {message.MessageText}\n"));
        }

        #endregion

        #endregion

        #region Inner Classes

        private class JsonType
        {
            public string Type { get; }

            public JsonType(string type = null)
            {
                Type = string.IsNullOrEmpty(type) ? GetType().Name : type;
            }
        }

        private class Message : JsonType
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

        private class Answer : JsonType
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