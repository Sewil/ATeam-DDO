using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatClient
{
    class Program
    {
        static Socket socket = null;
        const int BUFFERLENGTH = 100;
        const string IPADDRESS = "127.0.0.1";
        const int PORT = 8000;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint remoteEndPoint;
        static UTF8Encoding encoding = new UTF8Encoding();

        static void ConnectToServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            remoteEndPoint = new IPEndPoint(ipAddress, PORT);
            socket.Connect(remoteEndPoint);
            Console.WriteLine("Connected to server.");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            ConnectToServer();
            Chat();
        }

        static void Chat()
        {
            byte[] bufferIn;
            List<string> latestMessages;
            string messageToSend;
            Console.CursorVisible = false;

            while (true)
            {
                Console.Clear();
                bufferIn = new byte[BUFFERLENGTH];
                socket.Receive(bufferIn);

                latestMessages = encoding.GetString(bufferIn).TrimEnd('\0').Split(',').ToList();
                if(latestMessages.Count > 3)
                {
                    latestMessages.RemoveRange(0, latestMessages.Count - 4);
                }

                foreach (var message in latestMessages)
                {
                    Console.WriteLine(message);
                }

                Console.Write("Message: ");
                messageToSend = Console.ReadLine();
                SendMessage(messageToSend);
            }
        }

        static void SendMessage(string message)
        {
            byte[] bufferOut = encoding.GetBytes(message);
            socket.Send(bufferOut);
        }
    }
}
