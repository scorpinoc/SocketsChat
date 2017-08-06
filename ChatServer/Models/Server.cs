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
using ChatServer.Annotations;
using Newtonsoft.Json;

// todo !! refactor

namespace ChatServer.Models
{
    public sealed class Server : INotifyPropertyChanged
    {
        private bool _serverIsOn;

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private ICollection<Client> ClientsCollection { get; }

        private ManualResetEvent ResetEvent { get; }

        public string Nickname { get; }

        private Guid ServerId { get; }

        private IDictionary<IPEndPoint, Client> PendingClients { get; }

        private Client ServerClient { get; set; }

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

        public IEnumerable<Client> Clients => ClientsCollection;

        private IPEndPoint ServerAdress => ServerClient.Adress;

        #endregion

        public Server(string nickname)
        {
            MainDispatcher.Initialize();

            if (string.IsNullOrWhiteSpace(nickname))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or whitespace", nameof(nickname));

            if (nickname.Trim().Equals("Server"))
                // ReSharper disable once LocalizableElement
                throw new ArgumentOutOfRangeException(nameof(nickname), "'Server' can't be used as nickname");

            Nickname = nickname;
            ServerId = Guid.NewGuid();

            ResetEvent = new ManualResetEvent(false);

            ClientsCollection = new SynchronizedObservableCollection<Client>();

            PendingClients = new Dictionary<IPEndPoint, Client>();
        }

        #region Methods

        public void OpenServer([NotNull] IPEndPoint serverAdress)
        {
            if (ServerIsOn)
                throw new InvalidOperationException("Server can be initialized only once.");
            if (serverAdress == null) throw new ArgumentNullException(nameof(serverAdress));

            ServerClient = new Client(serverAdress, "Server", Guid.Empty);

            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                server.Bind(serverAdress);
                server.Listen(10);

                ServerIsOn = true;

                ClientsCollection.Add(ServerClient);
                ServerMessage($"Server opened on {serverAdress}");
                ServerMessage($"Nickname set as '{Nickname}'"); // todo redo and delete

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
                    AnalyzeRecieved(messageText);
            }
        }

        private void AnalyzeRecieved([NotNull] string messageText)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(messageText));

            var messageWrapper = JsonConvert.DeserializeObject<MessageWrapper>(messageText);
            switch (messageWrapper.MessageType)
            {
                case nameof(Message):
                {
                    var message = JsonConvert.DeserializeObject<MessageWrapper<Message>>(messageText).Message;
                    var client = Clients.Single(c => c.ClientId == messageWrapper.ClientId);
                    Send(client.Adress, client.Recieve(message));
                    break;
                }
                case nameof(Answer):
                {
                    var answer = JsonConvert.DeserializeObject<MessageWrapper<Answer>>(messageText).Message;
                    var client = Clients.Single(c => c.ClientId == messageWrapper.ClientId);
                    client.Recieve(answer);
                    break;
                }
                case nameof(ClientData):
                {
                    var clientData = JsonConvert.DeserializeObject<MessageWrapper<ClientData>>(messageText).Message;
                    AnalyzeRecieved(clientData, messageWrapper);
                    break;
                }
                default:
                    throw new TypeAccessException("Type not implemented");
            }
        }

        private void AnalyzeRecieved(ClientData clientData, MessageWrapper messageWrapper)
        {
            var adressParts = clientData.Adress.Split(':');
            Debug.Assert(adressParts.Length == 2);

            var clientAdress = new IPEndPoint(IPAddress.Parse(adressParts[0]), int.Parse(adressParts[1]));

            switch (clientData.DataType)
            {
                case ClientData.Action.Registration:
                    ClientRegistration(messageWrapper, clientData, clientAdress);
                    break;
                case ClientData.Action.Answer:
                    ClientRegistrationAnswer(messageWrapper, clientData, clientAdress);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClientRegistration(MessageWrapper messageWrapper,
            ClientData clientData,
            IPEndPoint clientAdress)
        {
            if (ServerId != messageWrapper.ClientId &&
                Clients.FirstOrDefault(client => client.ClientId == messageWrapper.ClientId) != null)
                throw new Exception(
                    $"Connect from {clientData.Adress} aborted; Client with such Guid already connected");

            var registratingClient = new Client(clientAdress, clientData.Nickname, messageWrapper.ClientId);
            ClientsCollection.Add(registratingClient);
            ServerMessage($"Connect from {clientData.Adress} with nickname: {clientData.Nickname}");
            Send(clientAdress, new ClientData(Nickname, ServerAdress, ClientData.Action.Answer));
        }

        private void ClientRegistrationAnswer(MessageWrapper messageWrapper,
            ClientData clientData,
            IPEndPoint clientAdress)
        {
            if (!PendingClients.ContainsKey(clientAdress))
                throw new Exception("Answer from unknown client");

            var pendingClient = PendingClients[clientAdress];
            PendingClients.Remove(clientAdress);
            pendingClient.Registrate(clientData.Nickname, messageWrapper.ClientId);

            if (ServerId != messageWrapper.ClientId)
                ClientsCollection.Add(pendingClient);

            ServerMessage(
                $"Connect to {clientData.Adress} succed; Client nickname: {clientData.Nickname}");
        }

        private void Send<T>([NotNull] EndPoint adress, [NotNull] T message)
        {
            Debug.Assert(message != null);
            Debug.Assert(adress != null);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                socket.Connect(adress);
                socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new MessageWrapper<T>(ServerId, message))));
            }
        }

        private void ServerMessage(string messageText)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(messageText));

            ServerClient.Messages.Add(new Message(ServerClient.MessageCounter++, ServerClient.Nickname, messageText)
            {
                RecieveTime = DateTime.Now
            });
        }

        public void ConnectTo([NotNull] IPEndPoint adress)
        {
            if (adress == null) throw new ArgumentNullException(nameof(adress));

            var connectedClient = Clients.FirstOrDefault(c => Equals(c.Adress, adress) && c.ClientId != Guid.Empty);
            if (connectedClient != null)
                throw new Exception(
                    $"Already connected to {connectedClient.Adress} client with nickname {connectedClient.Nickname}");

            var client = new Client(adress);
            PendingClients.Add(adress, client);
            ServerMessage($"Connect to {adress} started");
            Send(adress, new ClientData(Nickname, ServerAdress, ClientData.Action.Registration));
        }

        public void SendMessage(Client client, string text)
        {
            if (client == ServerClient) return;
            var message = new Message(client.MessageCounter++, Nickname, text.Trim());
            client.Queue(message);
            Send(client.Adress, message);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Nested Types

        private sealed class ClientData
        {
            public enum Action
            {
                Registration,
                Answer
            }

            public Action DataType { get; }

            public string Nickname { get; }
            public string Adress { get; }

            [JsonConstructor]
            // ReSharper disable once UnusedMember.Local
            public ClientData([NotNull] string nickname, [NotNull] string adress, Action dataType)
                : this(nickname, dataType)
            {
                if (string.IsNullOrWhiteSpace(adress))
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentException("Argument is null or whitespace", nameof(adress));

                Adress = adress;
            }

            public ClientData([NotNull] string nickname, [NotNull] IPEndPoint adress, Action dataType)
                : this(nickname, dataType)
            {
                if (adress == null) throw new ArgumentNullException(nameof(adress));

                Adress = adress.ToString();
            }

            private ClientData([NotNull] string nickname, Action dataType)
            {
                if (string.IsNullOrWhiteSpace(nickname))
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentException("Argument is null or whitespace", nameof(nickname));

                Nickname = nickname;
                DataType = dataType;
            }
        }

        private class MessageWrapper
        {
            public string MessageType { get; }

            public Guid ClientId { get; }

            // ReSharper disable once MemberCanBeProtected.Local
            public MessageWrapper(Guid clientId, [NotNull] string messageType)
            {
                if (string.IsNullOrEmpty(messageType))
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentException("Argument is null or empty", nameof(messageType));
                if (clientId == Guid.Empty)
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentException("Argument is null or empty", nameof(clientId));

                MessageType = messageType;
                ClientId = clientId;
            }
        }

        private class MessageWrapper<T> : MessageWrapper
        {
            public T Message { get; }

            public MessageWrapper(Guid clientId, [NotNull] T message, string type = null)
                : base(clientId, string.IsNullOrEmpty(type)
                    ? message.GetType().Name
                    : type)
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));
                Message = message;
            }
        }

        #endregion

    }
}