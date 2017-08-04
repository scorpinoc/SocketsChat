using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SocketsChat.Annotations;
using SocketsChat.Models;

namespace SocketsChat
{
    public sealed class ViewModel : INotifyPropertyChanged
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private Server Server { get; }
        public ICommand OpenServerCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand SetNicknameCommand { get; }
        public ICommand SendMessageCommand { get; }

        public EndPoint ConnectAdress => Server.ConnectAdress;
        public string Nickname => Server.Nickname;

        public IEnumerable<Message> Messages => Server.Messages;
        public IEnumerable<Message> PendingMessages => Server.PendingMessages;

        public bool CanSendMessage => Server.CanSendMessage;

        #endregion

        public ViewModel()
        {
            Server = new Server();

            // ReSharper disable once RedundantArgumentDefaultValue
            Server.PropertyChanged += (sender, args) => OnPropertyChanged(null);

            OpenServerCommand = DelegateCommand.CreateCommand<EndPoint>(Server.OpenServer,
                point => !Server.ServerIsOn, Server);
            ConnectCommand = DelegateCommand.CreateCommand<EndPoint>(Server.Connect,
                point => Server.ConnectAdress == null, Server);
            SetNicknameCommand = DelegateCommand.CreateCommand<string>(Server.SetNickname,
                s => string.IsNullOrWhiteSpace(Server.Nickname), Server);
            SendMessageCommand = DelegateCommand.CreateCommand<string>(Server.SendMessage,
                s => Server.CanSendMessage, Server);
        }

        #region Methods

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}