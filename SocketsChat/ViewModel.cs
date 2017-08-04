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
        public ICommand SendMessageCommand { get; }

        public string Nickname => Server.Nickname;

        public IEnumerable<Client> Clients => Server.Clients;

        #endregion

        public ViewModel(string nickname)
        {
            MainDispatcher.Initialize();

            Server = new Server(nickname);

            // ReSharper disable once RedundantArgumentDefaultValue
            Server.PropertyChanged += (sender, args) => OnPropertyChanged(null);

            OpenServerCommand = DelegateCommand.CreateCommand<IPEndPoint>(Server.OpenServer, point => !Server.ServerIsOn,
                Server);
            ConnectCommand = DelegateCommand.CreateCommand<IPEndPoint>(Server.ConnectTo, point => point != null);
            SendMessageCommand = DelegateCommand.CreateCommand<Client, string>(Server.SendMessage);
        }

        #region Methods

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}