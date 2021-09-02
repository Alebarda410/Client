using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Client
{
    public partial class MainWindow : Window
    {
        private Thread _thread;

        private string _name;
        private byte[] _nameByte;
        private readonly Telepathy.Client _client = new(512 * 1024);

        private bool _state;

        public MainWindow()
        {
            InitializeComponent();
            _client.OnConnected = null;
            _client.OnData = Dat;
            _client.OnDisconnected = Dis;
        }
        private void Dis()
        {
            Dispatcher.Invoke(() =>
            {
                Connect.IsEnabled = true;
                Disconnect.IsEnabled = false;
                PictureBox.Source = null;
            });
            MessageBox.Show("Трансляция закончилась или вы были отключены!", "Уведомление",
                MessageBoxButton.OK, MessageBoxImage.Asterisk);
            _thread.Interrupt();
        }
        private void Dat(ArraySegment<byte> msg)
        {
            using var stream = new MemoryStream(msg.Array!);
            var temp = BitmapFrame.Create(stream,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
            Dispatcher.Invoke(() =>
            {
                PictureBox.Source = temp;
            });
        }
        private void Getting()
        {
            try
            {
                while (true)
                {
                    _client.Tick(2);
                    Thread.Sleep(10);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _name = $"{Environment.UserName} - {Environment.MachineName}";
            _nameByte = Encoding.UTF8.GetBytes(_name);
            Title = _name;
            toolStripComboBox1.SelectedIndex = 0;
        }

        public static bool IsCorrect(string arg, out int numb, int lf = 0, int rt = int.MaxValue)
        {
            if (!int.TryParse(arg, out numb)) return false;
            return numb >= lf && numb <= rt;
        }
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCorrect(portTextBox.Text, out var port, 10000, 60000))
            {
                MessageBox.Show(@"Номер порта должен находиться между 10000 и 60000!",
                    @"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var ipString = toolStripComboBox1.Text;
            _client.Connect(ipString, port);
            Thread.Sleep(100);
            if (!_client.Send(_nameByte))
            {
                MessageBox.Show("Не удалось подключиться!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Connect.IsEnabled = false;
            Disconnect.IsEnabled = true;
            _thread = new Thread(Getting)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            Task.Run(() => MessageBox.Show("Вы находитесь в зале ожидания",
                "Уведомление", MessageBoxButton.OK, MessageBoxImage.Asterisk));
            _thread.Start();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            Connect.IsEnabled = true;
            Disconnect.IsEnabled = false;
            _client.Disconnect();
            PictureBox.Source = null;
            _thread.Interrupt();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            StateChange(null, true);
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            StateChange(e);
        }

        private void StateChange(KeyEventArgs e = null, bool check = false)
        {
            if (_state && check || e?.Key == Key.Escape)
            {
                Bar1.Visibility = Visibility.Visible;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResize;
                _state = false;
            }
            else if (!_state && check)
            {
                Bar1.Visibility = Visibility.Collapsed;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                _state = true;
            }
        }
    }
}
