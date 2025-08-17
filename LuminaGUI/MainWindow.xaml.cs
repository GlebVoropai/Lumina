using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace LuminaGUI
{
    public partial class MainWindow : Window
    {
        private System.Windows.Media.Color SelectedColor;
        private NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            UpdateColorPreview();

            // Инициализация tray icon
            trayIcon = new NotifyIcon();
            try
            {
                // Используем ресурс WPF
                var iconUri = new Uri("pack://application:,,,/Logo.ico");
                var iconStream = System.Windows.Application.GetResourceStream(iconUri)?.Stream;


                if (iconStream != null)
                {
                    trayIcon.Icon = new System.Drawing.Icon(iconStream);
                    trayIcon.Visible = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("Tray icon resource not found!");
                }
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
    }
}
