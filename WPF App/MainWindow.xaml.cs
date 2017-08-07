using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ChatServer;
using ChatServer.Models;
using ChatServer.Utility;
using WpfApp.Properties;

namespace WpfApp
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private static bool TryGetValueFrom<T>([NotNull] Window window, [NotNull] string propertyName, out T value)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (string.IsNullOrEmpty(propertyName))
                // ReSharper disable once LocalizableElement
                throw new ArgumentException("Argument is null or empty", nameof(propertyName));

            if (window.ShowDialog() == false)
            {
                value = default(T);
                return false;
            }
            value = (T) window.GetType().GetProperty(propertyName).GetValue(window);
            return true;
        }

        private static void SetBorderPadding(object sender, double thickness)
        {
            var control = sender as Control;
            if (control == null) return;
            control.BorderThickness = new Thickness(thickness);
            control.Padding = new Thickness(thickness);
        }

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private ViewModel ViewModel { get; }

        public ICommand SendCommand { get; }
        public ICommand ConnectCommand { get; }

        public string ServerAdress { get; private set; }

        public string Nickname => ViewModel.Nickname;

        public IEnumerable<Client> Clients => ViewModel.Clients;

        #endregion

        public MainWindow()
        {
            var nickname = string.Empty;
            Try(() =>
            {
                var valueGet = TryGetValueFrom(new NicknameChooseWindow("Guest #" + new Random().Next(1, 1000)),
                    nameof(NicknameChooseWindow.Nickname), out nickname);
                if (!valueGet)
                    Close();
            });

            ViewModel = new ViewModel(nickname);

            ViewModel.PropertyChanged += (sender, args) => PropertyChanged?.Invoke(this, args);

            TryOpenServer();

            SendCommand = ChainCommand.CreateCommand<Client, TextBox>(ViewModel.SendMessageCommand, (client, box) =>
            {
                var objects = new object[] { client, box.Text };
                box.Clear();
                return objects;
            }, (client, box) => new object[] { client, box.Text });

            //SendCommand = DelegateCommand.CreateCommand<Client, TextBox>(TrySendMessage,
            //    (client, box) => ViewModel.SendMessageCommand.CanExecute(new object[] {client, box.Text}), ViewModel);

            ConnectCommand = DelegateCommand.CreateCommand(TryConnect);

            InitializeComponent();
        }

        #region Methods

        private void TryOpenServer()
        {
            Try(() =>
            {
                IPEndPoint serverAdress;
                var getServerAdress = TryGetValueFrom(new IPEndPointRequestWindow("Enter your server IP and Port"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out serverAdress);

                if (!getServerAdress || !ViewModel.OpenServerCommand.CanExecute(serverAdress))
                    Close();

                ServerAdress = serverAdress.ToString();

                ThreadPool.QueueUserWorkItem(
                    state =>
                        Try(() => ViewModel.OpenServerCommand.Execute(serverAdress), () => InvokeInMainThread(Close)));
            });
        }

        private void TryConnect()
        {
            Try(() =>
            {
                IPEndPoint connectEndPoint;
                var valueGet = TryGetValueFrom(new IPEndPointRequestWindow("Enter server for chatting"),
                    nameof(IPEndPointRequestWindow.IpEndPoint), out connectEndPoint);

                if (valueGet)
                    ViewModel.ConnectCommand.Execute(connectEndPoint);
            });
        }

        private void TrySendMessage(Client client, TextBox textBox)
        {
            Try(() =>
            {
                ViewModel.SendMessageCommand.Execute(new object[] {client, textBox.Text});
                textBox.Clear();
            });
        }

        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void MessageBoxWith(string text, string caption = "Error")
            => InvokeInMainThread(() => MessageBox.Show(this, "Event occupied:\n" + text, caption));

        private void Try([NotNull] Action action, [CanBeNull] Action finallyAction = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException && e.InnerException != null)
                    e = e.InnerException;

                MessageBoxWith(e.Message);
            }
            finally
            {
                finallyAction?.Invoke();
            }
        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
            => (sender as ScrollViewer)?.ScrollToEnd();

        private void IncreaseBorderOnMouseEnter(object sender, MouseEventArgs e) => SetBorderPadding(sender, 0.5);

        private void DecreaseBorderOnMouseLeave(object sender, MouseEventArgs e) => SetBorderPadding(sender, 0);

        #endregion
    }

    public class MultiBingingToParamsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values.Clone();

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}