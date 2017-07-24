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
        public ICommand OpenServerCommand { get; }
        public ICommand SendCommand { get; }

        public MainWindow()
        {
            OpenServerCommand = DelegateCommand.CreateCommand(() =>
            {
                new Thread(() =>
                {
                    try
                    {
                        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                        {
                            var ip = string.Empty;
                            s1.Dispatcher.Invoke(() => { ip = s1.Text; });
                            var port = string.Empty;
                            s2.Dispatcher.Invoke(() => { port = s2.Text; });

                            var ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                            socket.Bind(ipEndPoint);
                            socket.Listen(10);

                            r.Dispatcher.Invoke(() => { r.Text += "Server opened\n"; });


                            while (true)
                                using (var accept = socket.Accept())
                                {

                                    int count;
                                    var bytes = new byte[4096];
                                    while ((count = accept.Receive(bytes)) > 0)
                                        if (count == 4096)
                                            r.Dispatcher.Invoke(() => { r.Text += Encoding.UTF8.GetString(bytes); });
                                        else
                                        {
                                            r.Dispatcher.Invoke(() => { r.Text += Encoding.UTF8.GetString(bytes.Take(count).ToArray()); });
                                            break;
                                        }
                                    r.Dispatcher.Invoke(() => { r.Text += "\n"; });
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }).Start();
            });

            SendCommand = DelegateCommand.CreateCommand(() =>
            {
                try
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                    {
                        var ip = string.Empty;
                        c1.Dispatcher.Invoke(() => { ip = c1.Text; });
                        var port = string.Empty;
                        c2.Dispatcher.Invoke(() => { port = c2.Text; });

                        var ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                        socket.Connect(ipEndPoint);
                        socket.Send(Encoding.UTF8.GetBytes("some test text"));
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            });

            InitializeComponent();
        }
    }
}