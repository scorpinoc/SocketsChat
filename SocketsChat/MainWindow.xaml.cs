using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using SocketsChat.Annotations;

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        #region Auto 

        public ICommand SendCommand { get; }

        #endregion

        #endregion

        #region Constructors

        public MainWindow()
        {
            var requestWindow = new IPEndPointRequestWindow("Enter your server IP and Port");
            if (requestWindow.ShowDialog() == true)
                ThreadPool.QueueUserWorkItem(state => OpenServer(requestWindow.IpEndPoint));
            else
                Close();

            SendCommand = DelegateCommand.CreateCommand(SendMessage);

            InitializeComponent();
        }

        #endregion

        #region Methods

        #region Private

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void OpenServer(EndPoint endPoint)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Bind(endPoint);
                    socket.Listen(10);

                    InvokeInMainThread(() => { chat.Text += "Server opened\n"; });

                    while (true)
                        Accept(socket);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        
        private void Accept(Socket socket)
        {
            using (var accept = socket.Accept())
            {
                int count;
                var bytes = new byte[4096];
                while ((count = accept.Receive(bytes)) > 0)
                    if (count == 4096)
                        InvokeInMainThread(() => { chat.Text += Encoding.UTF8.GetString(bytes); });
                    else
                    {
                        InvokeInMainThread(() => { chat.Text += Encoding.UTF8.GetString(bytes.Take(count).ToArray()); });
                        break;
                    }
                InvokeInMainThread(() => { chat.Text += "\n"; });
            }
        }

        private void SendMessage()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    var ip = string.Empty;
                    InvokeInMainThread(() => { ip = connectIP.Text; });
                    var port = string.Empty;
                    InvokeInMainThread(() => { port = connectPort.Text; });

                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                    socket.Connect(ipEndPoint);
                    socket.Send(Encoding.UTF8.GetBytes("some test text"));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        #endregion

        #endregion
    }
}