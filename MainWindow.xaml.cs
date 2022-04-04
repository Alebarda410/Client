using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static System.Threading.Tasks.Task;

namespace Client
{
    public partial class MainWindow
    {
        private string _name;
        private byte[] _nameByte;
        private readonly Telepathy.Client _client = new(512 * 1024);
        private bool _state;
        private CancellationTokenSource _cts;
        private  MemoryStream _memoryStream = new();

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
            _cts.Cancel();

            MessageBox.Show("Трансляция закончилась или вы были отключены!", "Уведомление",
                MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private  void Dat(ArraySegment<byte> msg)
        {
            _memoryStream.Position = 0;
            _memoryStream.Write(msg);
            _memoryStream.Position = 0;
            
            Dispatcher.Invoke(() =>
            {
                PictureBox.Source = BitmapFrame.Create(_memoryStream,
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad);
            });
        }

        private async void GettingAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    _client.Tick(2);
                    await Delay(10, token);
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

        private static bool IsCorrect(string arg, out int numb, int lf = 0, int rt = int.MaxValue)
        {
            if (!int.TryParse(arg, out numb)) return false;
            return numb >= lf && numb <= rt;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCorrect(portTextBox.Text, out var port, 10000, 60000))
            {
                MessageBox.Show(@"Номер порта должен находиться между 10000 и 60000!",
                    @"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ipString = toolStripComboBox1.Text;
            _client.Connect(ipString, port);
            await Delay(100);
            if (!_client.Send(_nameByte))
            {
                MessageBox.Show("Не удалось подключиться!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Connect.IsEnabled = false;
            Disconnect.IsEnabled = true;
            _cts = new CancellationTokenSource();
            new Task(() => GettingAsync(_cts.Token), TaskCreationOptions.LongRunning).Start();
            Run(() => MessageBox.Show("Вы находитесь в зале ожидания",
                "Уведомление", MessageBoxButton.OK, MessageBoxImage.Asterisk));
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            Connect.IsEnabled = true;
            Disconnect.IsEnabled = false;
            _client.Disconnect();
            PictureBox.Source = null;

            _cts.Cancel();
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