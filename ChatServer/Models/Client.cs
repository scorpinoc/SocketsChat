using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ChatServer.Annotations;

namespace ChatServer.Models
{
    public sealed class Client
    {
        #region Properties

        private ICollection<Message> PendingMessagesCollection { get; }

        public IPEndPoint Adress { get; }
        public string Nickname { get; private set; }
        public Guid ClientId { get; private set; }

        public uint MessageCounter { get; set; }
        public ICollection<Message> Messages { get; }

        public IEnumerable<Message> PendingMessages => PendingMessagesCollection;

        #endregion

        public Client([NotNull] IPEndPoint adress)
        {
            if (adress == null) throw new ArgumentNullException(nameof(adress));

            Adress = adress;
            Messages = new SynchronizedObservableCollection<Message>();
            PendingMessagesCollection = new SynchronizedObservableCollection<Message>();
        }

        public Client([NotNull] IPEndPoint adress, [NotNull] string nickname, Guid clientId)
            : this(adress)
        {
            Registrate(nickname, clientId);
        }

        #region Methods

        public void Registrate([NotNull] string nickname, Guid clientId)
        {
            if (Nickname != null)
                throw new ArgumentException("Property can't be set twice", nameof(nickname));
            if (string.IsNullOrWhiteSpace(nickname))
                throw new ArgumentException("Argument is null or whitespace", nameof(nickname));

            Nickname = nickname;
            ClientId = clientId;
        }

        public Answer Recieve(Message message)
        {
            message.RecieveTime = DateTime.Now;
            Messages.Add(message);
            return new Answer(message.Number, message.RecieveTime.Value);
        }

        public void Recieve(Answer answer)
        {
            var message = PendingMessages.First(msg => msg.Number == answer.Number);
            PendingMessagesCollection.Remove(message);
            message.RecieveTime = answer.AnswerTime;
            Messages.Add(message);
        }

        public void Queue(Message message) => PendingMessagesCollection.Add(message);

        #endregion
    }
}