using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SocketsChat.Annotations;

// todo ! refactor
namespace SocketsChat.Model
{
    public class ChatServer : INotifyPropertyChanged
    {
        private EndPoint _connectAdress;
        private string _nickname;

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private ManualResetEvent ResetEvent { get; }
        private uint MessageCounter { get; set; }

        public ICollection<IMessage> Messages { get; }
        public ICollection<IMessage> PendingMessages { get; }

        public EndPoint ConnectAdress
        {
            get { return _connectAdress; }
            private set
            {
                if (Equals(value, _connectAdress)) return;
                _connectAdress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
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
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        public bool CanSendMessage => ConnectAdress != null && !string.IsNullOrWhiteSpace(Nickname);

        #endregion

        public ChatServer()
        {
            Messages = new ObservableCollection<IMessage>();
            PendingMessages = new ObservableCollection<IMessage>();

            Nickname = string.Empty;

            ResetEvent = new ManualResetEvent(false);
        }

        #region Methods

        public void OpenServer([NotNull] EndPoint serverAdress)
        {
            if (serverAdress == null) throw new ArgumentNullException(nameof(serverAdress));

            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                server.Bind(serverAdress);
                server.Listen(10);

                ServerMessage($"Server opened on {serverAdress}");

                while (true)
                {
                    ResetEvent.Reset();
                    server.BeginAccept(AcceptMessage, server);
                    ResetEvent.WaitOne();
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void AcceptMessage(IAsyncResult ar)
        {
            Debug.Assert(ar.AsyncState is Socket);

            ResetEvent.Set();

            using (var accept = ((Socket)ar.AsyncState).EndAccept(ar))
            {
                int count;
                const int acceptSize = 4096;
                var bytes = new byte[acceptSize];
                var messageText = string.Empty;

                while ((count = accept.Receive(bytes)) > 0) // todo async revieve
                    messageText += count == acceptSize
                        ? Encoding.UTF8.GetString(bytes)
                        : Encoding.UTF8.GetString(bytes.Take(count).ToArray());

                if (!string.IsNullOrWhiteSpace(messageText))
                    AnalyzeRecievedMessage(messageText);
            }
        }

        private void AnalyzeRecievedMessage([NotNull] string messageText)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(messageText));

            switch (JsonConvert.DeserializeObject<TypeWrapper>(messageText).Type)
            {
                case nameof(Message):
                    {
                        var message = JsonConvert.DeserializeObject<TypeWrapper<Message>>(messageText).Obj;
                        message.RecieveTime = DateTime.Now;
                        Messages.Add(message);
                        var answer = new Answer(message.Number, DateTime.Now);
                        Send(answer);
                        break;
                    }
                case nameof(Answer):
                    {
                        var answer = JsonConvert.DeserializeObject<TypeWrapper<Answer>>(messageText).Obj;
                        var message = PendingMessages.First(msg => msg.Number == answer.Number);
                        PendingMessages.Remove(message);
                        message.RecieveTime = answer.AnswerTime;
                        Messages.Add(message);
                        break;
                    }
                default:
                    throw new TypeAccessException("Type not implemented");
            }
        }

        private void Send<T>([NotNull] T message)
        {
            Debug.Assert(message != null);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                socket.Connect(ConnectAdress);
                socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TypeWrapper<T>(message))));
            }
        }

        private void ServerMessage(string messageText)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(messageText));

            Messages.Add(new Message(MessageCounter++, "Server", messageText)
            {
                RecieveTime = DateTime.Now
            });
        }

        public void Connect([NotNull] IPEndPoint connectEndPoint)
        {
            if (connectEndPoint == null) throw new ArgumentNullException(nameof(connectEndPoint));

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                socket.Connect(connectEndPoint);
            ConnectAdress = connectEndPoint;
            ServerMessage($"Succesfully connected to {ConnectAdress}");
        }

        public void SetNickname([NotNull] string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or whitespace", nameof(nickname));

            nickname = nickname.Trim();

            if (nickname.Equals("Server"))
                // ReSharper disable once LocalizableElement
                throw new ArgumentOutOfRangeException(nameof(nickname), "'Server' can't be used as nickname");

            Nickname = nickname;
            ServerMessage($"Nickname set as '{Nickname}'");
        }

        public void SendMessage(string text)
        {
            var message = new Message(MessageCounter++, Nickname, text);
            PendingMessages.Add(message);
            Send(message);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Nested Types

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

        private class Message : IMessage
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