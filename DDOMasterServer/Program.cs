using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System;
using DDOProtocol;

namespace DDOMasterServer
{
    class Program
    {
        const int LISTENERBACKLOG = 100;
        const int BUFFERLENGTHPLAYER = 100;
        const int BUFFERLENGTH = 100;
        const int PORT = 8000;
        static int clientPort = 8001;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);
        static UTF8Encoding encoding = new UTF8Encoding();
        static void Main(string[] args)
        {
            Console.WriteLine("Enter master server ip: ");
            var ip = Console.ReadLine();
            ipAddress = IPAddress.Parse(ip);

            var protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 500);
            var serverList = new List<string>();
            Socket listeningSocket = null;
            Socket[] sockets = new Socket[15];
            int i = 0;
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(localEndPoint);
            Console.WriteLine("MasterServer initialized");
            while (true) {
                listeningSocket.Listen(LISTENERBACKLOG);
                sockets[i] = listeningSocket.Accept();
                protocol.Socket = sockets[i];
                var r = protocol.Receive();
                if (r.Data == "im a server i promise") {
                    Console.WriteLine($"Server {i} connected");
                    string idAndPort = i.ToString() + " " + clientPort;
                    protocol.Send(new Response(ResponseStatus.OK, DataType.Text, clientPort.ToString()));
                    serverList.Add(idAndPort);
                    i++;
                    clientPort++;
                    foreach (var server in serverList) {
                        var info = server.Split(' ');
                        Console.WriteLine($"ServerID: {info[0]}     Port: {info[1]}");
                    }
                } else if (r.Data == "list") {
                    Console.WriteLine("Client connected");
                    string data = "";
                    foreach (var server in serverList) {
                        data += " " + server;
                    }
                    protocol.Send(new Response(ResponseStatus.OK, DataType.Text, data));
                    i++;
                }
            }
        }
    }
}
