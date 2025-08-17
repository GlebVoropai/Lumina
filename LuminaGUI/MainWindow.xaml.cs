using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // для NotifyIcon
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace LuminaGUI
{
    public partial class MainWindow : System.Windows.Window
    {
        private System.Windows.Media.Color SelectedColor;
        private NotifyIcon trayIcon;

        private UdpClient _udpClient;
        private IPEndPoint _endpoint;
        private Thread _sendThread;
        private bool _isSending = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateColorPreview();

            // восстановить настройки
            IpTextBox.Text = Properties.Settings.Default.IpAddress;
            UdpPortTextBox.Text = Properties.Settings.Default.UdpPort;
            LedCountTextBox.Text = Properties.Settings.Default.LedCount;
            StartMinimizedCheckBox.IsChecked = Properties.Settings.Default.StartMinimized;

            // Если стоит запуск свернутым
            if (Properties.Settings.Default.StartMinimized)
            {
                this.Hide();
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            // Инициализация tray icon
            trayIcon = new NotifyIcon();
            try
            {
                var iconUri = new Uri("pack://application:,,,/Logo.ico");
                var iconStream = System.Windows.Application.GetResourceStream(iconUri)?.Stream;

                if (iconStream != null)
                    trayIcon.Icon = new System.Drawing.Icon(iconStream);
                else
                    System.Windows.MessageBox.Show("Tray icon resource not found!");

                trayIcon.Visible = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Tray icon load error: {ex.Message}");
            }

            trayIcon.DoubleClick += (s, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            StopSending();
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void HSLSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HueSlider == null || SaturationSlider == null || LightnessSlider == null || ColorPreview == null)
                return;

            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            float h = (float)HueSlider.Value;
            float s = (float)SaturationSlider.Value / 100f;
            float l = (float)LightnessSlider.Value / 100f;

            System.Windows.Media.Color rgbColor = HSLtoRGB(h, s, l);

            ColorPreview.Background = new SolidColorBrush(rgbColor);
            SelectedColor = rgbColor;
            if (ColorPreview.Effect is DropShadowEffect shadow)
            {
                shadow.Color = rgbColor;
            }
        }

        public static System.Windows.Media.Color HSLtoRGB(float h, float s, float l)
        {
            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = l - c / 2;

            float r1 = 0, g1 = 0, b1 = 0;

            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)((r1 + m) * 255);
            byte g = (byte)((g1 + m) * 255);
            byte b = (byte)((b1 + m) * 255);

            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем окно и показываем только в трее
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // сохраняем настройки
            Properties.Settings.Default.IpAddress = IpTextBox.Text;
            Properties.Settings.Default.UdpPort = UdpPortTextBox.Text;
            Properties.Settings.Default.LedCount = LedCountTextBox.Text;
            Properties.Settings.Default.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
            Properties.Settings.Default.Save();
            StopSending();
            StartSending();
        }

        private void StartSending()
        {
            if (_isSending) return;

            if (!int.TryParse(UdpPortTextBox.Text, out int port))
            {
                System.Windows.MessageBox.Show("Неверный UDP порт");
                return;
            }

            if (!int.TryParse(LedCountTextBox.Text, out int ledCount))
            {
                System.Windows.MessageBox.Show("Неверное количество диодов");
                return;
            }

            string ip = IpTextBox.Text;

            try
            {
                _endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            }
            catch
            {
                System.Windows.MessageBox.Show("Неверный IP адрес");
                return;
            }

            _udpClient = new UdpClient();
            _isSending = true;

            _sendThread = new Thread(() =>
            {
                while (_isSending)
                {
                    byte[] data = new byte[ledCount * 3];

                    for (int i = 0; i < ledCount; i++)
                    {
                        data[i * 3] = SelectedColor.R;
                        data[i * 3 + 1] = SelectedColor.G;
                        data[i * 3 + 2] = SelectedColor.B;
                    }

                    try
                    {
                        _udpClient.Send(data, data.Length, _endpoint);
                    }
                    catch
                    {
                        // можно логировать ошибки
                    }

                    Thread.Sleep(1000 / 30); // FPS 30
                }
            })
            {
                IsBackground = true
            };
            _sendThread.Start();
        }

        private void StopSending()
        {
            _isSending = false;
            _sendThread?.Join();
            _udpClient?.Close();
        }
    }
}
