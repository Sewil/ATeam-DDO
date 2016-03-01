using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

namespace DDOMasterServer
{
    class Program
    {
        const int LISTENERBACKLOG = 100;
        const int BUFFERLENGTHPLAYER = 100;
        const string IPADDRESS = "127.0.0.1";
        const int PORT = 8000;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);
        static UTF8Encoding encoding = new UTF8Encoding();
        static void Main(string[] args)
        {
            Socket listeningSocket = null;
            Socket[] sockets = new Socket[5];
            int i = 0;
            try
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                while (true)
                {
                    listeningSocket.Listen(LISTENERBACKLOG);
                    if (i < 5)
                    {
                        sockets[0] = listeningSocket.Accept();
                        // Skicka response (i) till servern
                        Console.WriteLine($"Server {i} connected");
                        i++;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }
        }
    }
}