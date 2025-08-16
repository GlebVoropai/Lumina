using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main()
    {
        UdpClient client = new UdpClient();
        Console.WriteLine("Enter ESP IP (e.g., 192.168.1.50): ");
        string ip = Console.ReadLine();
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), 4210);

        while (true)
        {
            Console.Write("Enter RGB (r,g,b): ");
            string[] parts = Console.ReadLine().Split(',');
            if (parts.Length != 3) continue;

            byte r = byte.Parse(parts[0]);
            byte g = byte.Parse(parts[1]);
            byte b = byte.Parse(parts[2]);

            byte[] data = new byte[] { r, g, b };
            client.Send(data, data.Length, ep);
        }
    }
}
