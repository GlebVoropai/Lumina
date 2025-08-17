using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LuminaGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Проверяем, что слайдеры созданы
            if (HueSlider != null && SaturationSlider != null && LightnessSlider != null && ColorPreview != null)
            {
                // Устанавливаем значения по умолчанию
                HueSlider.Value = 300;         // Пример: RGB(128,0,128) -> HSL примерно H=300, S=1, L=0.25
                SaturationSlider.Value = 100;
                LightnessSlider.Value = 25;

                // Обновляем превью
                UpdateColorPreview();
            }
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
            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            float h = (float)HueSlider.Value;
            float s = (float)SaturationSlider.Value / 100f;
            float l = (float)LightnessSlider.Value / 100f;

            ColorPreview.Background = new SolidColorBrush(HSLtoRGB(h, s, l));
        }

        // Конвертация HSL -> RGB
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
    }
}
