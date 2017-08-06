using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ChatServer.Annotations;
using ChatServer.Utility;

namespace ChatServer.Models
{
    public sealed class Client
    {
        #region Properties

        private ICollection<Message> MessagesCollection { get; }
        private ICollection<Message> PendingMessagesCollection { get; }

        private uint MessageCounter { get; set; }

        public IPEndPoint Adress { get; }
        public string Nickname { get; private set; }
        public Guid ClientId { get; private set; }

        public IEnumerable<Message> Messages => MessagesCollection;
        public IEnumerable<Message> PendingMessages => PendingMessagesCollection;

        #endregion

        public Client([NotNull] IPEndPoint adress)
        {
            if (adress == null) throw new ArgumentNullException(nameof(adress));

            Adress = adress;

            var dispatcher = MainThreadDispatcher.Dispatcher;

            MessagesCollection = new ControlledObservableCollection<Message>(dispatcher);
            PendingMessagesCollection = new ControlledObservableCollection<Message>(dispatcher);
        }

        public Client([NotNull] IPEndPoint adress, [NotNull] string nickname, Guid clientId)
            : this(adress)
        {
            Registrate(nickname, clientId);
        }

        #region Methods

        internal void Registrate([NotNull] string nickname, Guid clientId)
        {
            if (Nickname != null)
                throw new ArgumentException("Property can't be set twice", nameof(nickname));
            if (string.IsNullOrWhiteSpace(nickname))
                throw new ArgumentException("Argument is null or whitespace", nameof(nickname));

            Nickname = nickname;
            ClientId = clientId;
        }

        internal Answer Recieve(Message message)
        {
            message.RecieveTime = DateTime.Now;
            MessagesCollection.Add(message);
            return new Answer(message.Number, message.RecieveTime.Value);
        }

        internal void Recieve(Answer answer)
        {
            var message = PendingMessages.First(msg => msg.Number == answer.Number);
            PendingMessagesCollection.Remove(message);
            message.RecieveTime = answer.AnswerTime;
            MessagesCollection.Add(message);
        }

        internal Message Queue(string nickname, string messageText)
        {
            var message = new Message(MessageCounter++, nickname, messageText.Trim());
            PendingMessagesCollection.Add(message);
            return message;
        }

        internal void Add(string message)
            => MessagesCollection.Add(new Message(MessageCounter++, Nickname, message) {RecieveTime = DateTime.Now});

        #endregion
    }
}