using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;

namespace LuminaGUI
{
    public partial class MainWindow : Window
    {
        private float SelectedHue;
        private float SelectedSaturation;
        private float SelectedLightness;
        private bool _updatingSliders = false;

        private TrayIconManager trayIcon;

        private UdpClient? _udpClient;
        private IPEndPoint? _endpoint;
        private bool _isSending = false;
        private DispatcherTimer? _sendTimer;

        public MainWindow()
        {
            InitializeComponent(); // Создаём все элементы интерфейса

            // Инициализируем стартовые HSL значения после создания слайдеров
            SelectedHue = (float)HueSlider.Value;
            SelectedSaturation = (float)SaturationSlider.Value / 100f;
            SelectedLightness = (float)LightnessSlider.Value / 100f;
            UpdateColorPreview();

            InitializeColorPalette();

            // Восстанавливаем настройки
            IpTextBox.Text = Properties.Settings.Default.IpAddress;
            UdpPortTextBox.Text = Properties.Settings.Default.UdpPort;
            LedCountTextBox.Text = Properties.Settings.Default.LedCount;
            StartMinimizedCheckBox.IsChecked = Properties.Settings.Default.StartMinimized;

            if (Properties.Settings.Default.StartMinimized)
            {
                this.Hide();
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            // Инициализация иконки в трее
            trayIcon = new TrayIconManager();
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
            if (_updatingSliders) return;

            // Проверяем, что слайдеры уже созданы
            if (HueSlider == null || SaturationSlider == null || LightnessSlider == null)
                return;

            SelectedHue = (float)HueSlider.Value;
            SelectedSaturation = (float)SaturationSlider.Value / 100f;
            SelectedLightness = (float)LightnessSlider.Value / 100f;

            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            var color = HSLtoRGB(SelectedHue, SelectedSaturation, SelectedLightness);

            if (ColorPreview != null)
                ColorPreview.Background = new SolidColorBrush(color);

            if (ColorPreview?.Effect is DropShadowEffect shadow)
                shadow.Color = color;
        }

        public static Color HSLtoRGB(float h, float s, float l)
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

            return Color.FromRgb(r, g, b);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
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
                MessageBox.Show("Invalid UDP port");
                return;
            }

            if (!int.TryParse(LedCountTextBox.Text, out int ledCount))
            {
                MessageBox.Show("Invalid LED count");
                return;
            }

            string ip = IpTextBox.Text;

            try
            {
                _endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                _udpClient = new UdpClient();
            }
            catch
            {
                MessageBox.Show("Invalid IP address");
                return;
            }

            _sendTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000 / 30) // FPS 30
            };
            _sendTimer.Tick += (s, e) =>
            {
                if (_udpClient == null || _endpoint == null) return;

                byte[] data = new byte[ledCount * 3];

                for (int i = 0; i < ledCount; i++)
                {
                    data[i * 3] = (byte)(SelectedHue / 360f * 255);
                    data[i * 3 + 1] = (byte)(SelectedSaturation * 255);
                    data[i * 3 + 2] = (byte)(SelectedLightness * 255);
                }

                try
                {
                    _udpClient.Send(data, data.Length, _endpoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("UDP send error: " + ex.Message);
                }
            };

            _sendTimer.Start();
            _isSending = true;
            MessageBox.Show("Started sending colors");
        }

        private void StopSending()
        {
            _isSending = false;
            _sendTimer?.Stop();
            _udpClient?.Close();
            MessageBox.Show("Stopped sending colors");
        }

        private void SetHSLValues(float h, float s, float l)
        {
            SelectedHue = h;
            SelectedSaturation = s;
            SelectedLightness = l;

            if (HueSlider == null || SaturationSlider == null || LightnessSlider == null) return;

            _updatingSliders = true;

            HueSlider.Value = SelectedHue;
            SaturationSlider.Value = SelectedSaturation * 100;
            LightnessSlider.Value = SelectedLightness * 100;

            _updatingSliders = false;

            UpdateColorPreview();
        }

        private void InitializeColorPalette()
        {
            var rand = new Random();
            int cellCount = 27;

            for (int i = 0; i < cellCount; i++)
            {
                float h = (float)(rand.NextDouble() * 360);
                float s = (float)(rand.NextDouble());
                float l = (float)(rand.NextDouble());

                var color = HSLtoRGB(h, s, l);

                var border = new Border
                {
                    Width = 24,
                    Height = 24,
                    Background = new SolidColorBrush(color),
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand
                };

                border.MouseLeftButtonDown += (sObj, e) =>
                {
                    SetHSLValues(h, s, l);
                };

                ColorPalettePanel.Children.Add(border);
            }
        }
    }
}
