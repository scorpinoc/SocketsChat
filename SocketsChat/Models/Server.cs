using System;
using System.Collections.Generic;
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

namespace SocketsChat.Models
{
    public sealed class Server : INotifyPropertyChanged
    {
        private bool _serverIsOn;

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        private Client Client { get; }

        private ManualResetEvent ResetEvent { get; }

        public string Nickname { get; private set; }

        public bool ServerIsOn
        {
            get { return _serverIsOn; }
            private set
            {
                if (value == _serverIsOn) return;
                _serverIsOn = value;
                OnPropertyChanged();
            }
        }

        public ICollection<Message> Messages => Client.Messages;
        public ICollection<Message> PendingMessages => Client.PendingMessages;

        public EndPoint ConnectAdress => Client.Adress;

        public bool CanSendMessage => ConnectAdress != null && !string.IsNullOrWhiteSpace(Nickname);

        #endregion

        public Server()
        {
            Client = new Client();

            Client.PropertyChanged += (sender, args) => OnPropertyChanged(args.PropertyName);

            Nickname = string.Empty;

            ResetEvent = new ManualResetEvent(false);
        }

        #region Methods

        public void OpenServer([NotNull] EndPoint serverAdress)
        {
            if (ServerIsOn)
                throw new InvalidOperationException("Server can be initialized only once.");
            if (serverAdress == null) throw new ArgumentNullException(nameof(serverAdress));

            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                server.Bind(serverAdress);
                server.Listen(10);

                ServerIsOn = true;
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

            using (var accept = ((Socket) ar.AsyncState).EndAccept(ar))
            {
                int count;
                const int acceptSize = 4096;
                var bytes = new byte[acceptSize];
                var messageText = string.Empty;

                while ((count = accept.Receive(bytes)) > 0) // todo async recieve
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

                    Client.Recieve(message);

                    var answer = new Answer(message.Number, DateTime.Now);
                    Send(answer);
                    break;
                }
                case nameof(Answer):
                {
                    var answer = JsonConvert.DeserializeObject<TypeWrapper<Answer>>(messageText).Obj;

                    Client.Recieve(answer);
                    break;
                }
                default:
                    throw new TypeAccessException("Type not implemented");
            }
        }

        private void Send<T>([NotNull] T message)
        {
            Debug.Assert(message != null);

            if (ConnectAdress == null) return;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                socket.Connect(ConnectAdress);
                socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TypeWrapper<T>(message))));
            }
        }

        private void ServerMessage(string messageText)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(messageText));

            Messages.Add(new Message(Client.MessageCounter++, "Server", messageText)
            {
                RecieveTime = DateTime.Now
            });
        }

        public void Connect([NotNull] EndPoint connectEndPoint)
        {
            if (connectEndPoint == null) throw new ArgumentNullException(nameof(connectEndPoint));

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                socket.Connect(connectEndPoint);

            Client.Adress = connectEndPoint;
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
            var message = new Message(Client.MessageCounter++, Nickname, text.Trim());
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

        #endregion
    }
}