using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System;

namespace DDOMasterServer
{
    class Program
    {
        const int LISTENERBACKLOG = 100;
        const int BUFFERLENGTHPLAYER = 100;
        const int BUFFERLENGTH = 100;
        const string IPADDRESS = "127.0.0.1";
        const int PORT = 8000;
        static int clientPort = 8001;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);
        static UTF8Encoding encoding = new UTF8Encoding();

        static void Main(string[] args)
        {
            var serverList = new List<string>();
            Socket listeningSocket = null;
            Socket[] sockets = new Socket[15];
            int i = 0;

            try
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                Console.WriteLine("MasterServer initialized");

                while (true)
                {
                    listeningSocket.Listen(LISTENERBACKLOG);
                    sockets[i] = listeningSocket.Accept();
                    byte[] bufferIn = new Byte[BUFFERLENGTH];
                    sockets[i].Receive(bufferIn);
                    var request = encoding.GetString(bufferIn).TrimEnd('\0');

                    if (request.Equals("server"))
                    {
                        Console.WriteLine($"Server {i} connected");
                        string idAndPort = i.ToString() + " " + clientPort;
                        byte[] bufferOut = encoding.GetBytes(idAndPort);
                        sockets[i].Send(bufferOut);
                        serverList.Add(idAndPort);
                        i++;
                        clientPort++;

                        foreach (var server in serverList)
                        {
                            var info = server.Split(' ');
                            Console.WriteLine($"ServerID: {info[0]}     Port: {info[1]}");
                        }
                    }
                    else if (request.Equals("list"))
                    {
                        Console.WriteLine("Client connected");
                        string response = "";
                        foreach (var server in serverList)
                        {
                            response += " " + server;
                        }

                        byte[] bufferOut = encoding.GetBytes(response);
                        sockets[i].Send(bufferOut);
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
