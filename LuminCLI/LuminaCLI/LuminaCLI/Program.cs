using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    const int NUM_LEDS = 60;           // количество диодов
    const int CAPTURE_WIDTH = 1920;    // ширина экрана
    const int CAPTURE_HEIGHT = 1080;   // высота экрана
    const int FPS = 30;                // частота обновления

    static void Main()
    {
        UdpClient client = new UdpClient();
        string ip = "192.168.100.74"; // IP ESP
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), 4210);

        Bitmap bmp = new Bitmap(CAPTURE_WIDTH, CAPTURE_HEIGHT);
        Graphics g = Graphics.FromImage(bmp);

        while (true)
        {
            // захват экрана
            g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

            // массив данных для ESP
            byte[] data = new byte[NUM_LEDS * 3];

            int zoneWidth = CAPTURE_WIDTH / NUM_LEDS;

            for (int i = 0; i < NUM_LEDS; i++)
            {
                int startX = i * zoneWidth;
                int endX = startX + zoneWidth;

                long rSum = 0, gSum = 0, bSum = 0;
                int count = 0;

                for (int x = startX; x < endX; x += 10) // шаг пикселей для скорости
                {
                    for (int y = 0; y < CAPTURE_HEIGHT; y += 10)
                    {
                        Color pixel = bmp.GetPixel(x, y);
                        rSum += pixel.R;
                        gSum += pixel.G;
                        bSum += pixel.B;
                        count++;
                    }
                }

                byte r = (byte)(rSum / count);
                byte gcol = (byte)(gSum / count);
                byte b = (byte)(bSum / count);

                data[i * 3] = r;
                data[i * 3 + 1] = gcol;
                data[i * 3 + 2] = b;
            }

            // отправка пакета
            client.Send(data, data.Length, ep);

            Thread.Sleep(1000 / FPS);
        }
    }
}
