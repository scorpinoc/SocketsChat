using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Windows.Input;
using ChatServer.Utility.Commands;

namespace WpfApp
{
    /// <summary>
    ///     Логика взаимодействия для IPEndPointRequestWindow.xaml
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class IPEndPointRequestWindow
    {
        public byte IP1 { get; set; }
        public byte IP2 { get; set; }
        public byte IP3 { get; set; }
        public byte IP4 { get; set; }

        public int Port { get; set; }

        public string Description { get; }
        public ICommand OkCommand { get; }
        //todo ask to confirm before close
        public ICommand CancelCommand { get; }

        public IPEndPoint IpEndPoint => new IPEndPoint(new IPAddress(new[] { IP1, IP2, IP3, IP4 }), Port);

        public IPEndPointRequestWindow(string description, string title = "IP:Port Request")
        {
            Description = description;
            Title = title;

            IP1 = 127;
            IP2 = 0;
            IP3 = 0;
            IP4 = 1;
            Port = 3000;

            OkCommand = DelegateCommand.CreateCommand(() =>
            {
                DialogResult = true;
                Close();
            });

            CancelCommand = DelegateCommand.CreateCommand(() =>
            {
                DialogResult = false;
                Close();
            });

            InitializeComponent();
        }
    }
}