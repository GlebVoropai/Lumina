using System;
using System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace LuminaGUI
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon trayIcon;

        public TrayIconManager()
        {
            trayIcon = new NotifyIcon();
            var iconStream = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Logo.ico"))?.Stream;

            if (iconStream != null)
                trayIcon.Icon = new DrawingIcon(iconStream);

            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            var main = System.Windows.Application.Current.MainWindow;
            if (main != null)
            {
                main.Show();
                main.WindowState = System.Windows.WindowState.Normal;
                main.ShowInTaskbar = true;
            }
        }

        public void Dispose()
        {
            trayIcon.Dispose();
        }
    }
}
