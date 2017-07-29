using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SocketsChat.Annotations;

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для NicknameChooseWindow.xaml
    /// </summary>
    public partial class NicknameChooseWindow : INotifyPropertyChanged
    {
        private string _nickname;

        public string Nickname
        {
            get { return _nickname; }
            set
            {
                if (value == _nickname) return;
                _nickname = value;
                OnPropertyChanged();
            }
        }

        public ICommand OkCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public NicknameChooseWindow(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or empty", nameof(nickname));

            Nickname = nickname;

            OkCommand = DelegateCommand.CreateCommand(() =>
            {
                DialogResult = true;
                Close();
            }, () => !string.IsNullOrWhiteSpace(Nickname), this);

            CancelCommand = DelegateCommand.CreateCommand(() =>
            {
                DialogResult = false;
                Close();
            });

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}