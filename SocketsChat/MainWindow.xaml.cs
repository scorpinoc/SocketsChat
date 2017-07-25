using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace SocketsChat
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

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
        
        private void InvokeInMainThread(Action callback) => Dispatcher.Invoke(callback);

        private void OpenServer(EndPoint endPoint)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Bind(endPoint);
                    socket.Listen(10);

                    InvokeInMainThread(() => ChatBox.AppendText("Server opened\n"));

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

                var appendMessage =
                    new Action<byte[]>(
                        message =>
                            InvokeInMainThread(
                                () => ChatBox.AppendText("other: " + Encoding.UTF8.GetString(message) + "\n")));

                while ((count = accept.Receive(bytes)) > 0)
                    if (count == 4096)
                        appendMessage(bytes);
                    else
                    {
                        appendMessage(bytes.Take(count).ToArray());
                        break;
                    }
            }
        }

        private void SendMessage()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(ConnectIp.Text), int.Parse(ConnectPort.Text));
                    socket.Connect(ipEndPoint);

                    var message = string.Empty;
                    InvokeInMainThread(() => message = Message.Text);
                    InvokeInMainThread(() => ChatBox.AppendText("I: " + message + "\n"));
                    socket.Send(Encoding.UTF8.GetBytes(message));
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