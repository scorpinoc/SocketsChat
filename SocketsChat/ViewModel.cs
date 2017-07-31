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

        private ChatServer ChatServer { get; }
        public ICommand OpenServerCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand SetNicknameCommand { get; }
        public ICommand SendMessageCommand { get; }

        public EndPoint ConnectAdress => ChatServer.ConnectAdress;
        public string Nickname => ChatServer.Nickname;

        public IEnumerable<IMessage> Messages => ChatServer.Messages;
        public IEnumerable<IMessage> PendingMessages => ChatServer.PendingMessages;

        public bool CanSendMessage => ChatServer.CanSendMessage;

        #endregion

        public ViewModel()
        {
            ChatServer = new ChatServer();

            // ReSharper disable once RedundantArgumentDefaultValue
            ChatServer.PropertyChanged += (sender, args) => OnPropertyChanged(null);

            OpenServerCommand = DelegateCommand.CreateCommand<EndPoint>(ChatServer.OpenServer,
                point => !ChatServer.ServerIsOn, ChatServer);
            ConnectCommand = DelegateCommand.CreateCommand<EndPoint>(ChatServer.Connect,
                point => ChatServer.ConnectAdress == null, ChatServer);
            SetNicknameCommand = DelegateCommand.CreateCommand<string>(ChatServer.SetNickname,
                s => string.IsNullOrWhiteSpace(ChatServer.Nickname), ChatServer);
            SendMessageCommand = DelegateCommand.CreateCommand<string>(ChatServer.SendMessage,
                s => ChatServer.CanSendMessage, ChatServer);
        }

        #region Methods

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}