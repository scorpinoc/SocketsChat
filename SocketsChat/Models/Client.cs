using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using SocketsChat.Annotations;

namespace SocketsChat.Models
{
    public sealed class Client : INotifyPropertyChanged
    {
        private EndPoint _adress;

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        public uint MessageCounter { get; set; }
        public ICollection<Message> Messages { get; }
        public ICollection<Message> PendingMessages { get; }

        // todo readonly
        public EndPoint Adress
        {
            get { return _adress; }
            set
            {
                if (Equals(value, _adress)) return;
                _adress = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public Client()
        {
            Messages = new ObservableCollection<Message>();
            PendingMessages = new ObservableCollection<Message>();
        }

        #region Methods

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

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}