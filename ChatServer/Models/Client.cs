using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ChatServer.Models
{
    public sealed class Client
    {
        #region Properties

        public uint MessageCounter { get; set; }
        public ICollection<Message> Messages { get; }
        public ICollection<Message> PendingMessages { get; }

        public IPEndPoint Adress { get; }

        public string Nickname { get; set; }

        public Guid ClientId { get; set; }

        #endregion

        public Client(IPEndPoint adress)
        {
            Adress = adress;
            Messages = new SynchronizedObservableCollection<Message>();
            PendingMessages = new SynchronizedObservableCollection<Message>();
        }

        #region Methods

        // todo form and return Answer
        public void Recieve(Message message)
        {
            message.RecieveTime = DateTime.Now;
            Messages.Add(message);
        }

        public void Recieve(Answer answer)
        {
            var message = PendingMessages.First(msg => msg.Number == answer.Number);
            PendingMessages.Remove(message);
            message.RecieveTime = answer.AnswerTime;
            Messages.Add(message);
        }

        #endregion
    }
}