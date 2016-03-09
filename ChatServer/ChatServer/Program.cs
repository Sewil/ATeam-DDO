using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {
        static Socket[] sockets;
        const int BUFFERLENGTH = 100;
        const int LISTENERBACKLOG = 100;
        const string IPADDRESS = "127.0.0.1";
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8000);
        static UTF8Encoding encoding = new UTF8Encoding();
        static List<string> latestMessages = new List<string>();

        static void Main(string[] args)
        {
            Socket listeningSocket = null;
            sockets = new Socket[2];

            try
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                while (true)
                {
                    listeningSocket.Listen(LISTENERBACKLOG);
                    sockets[0] = listeningSocket.Accept();
                    Console.WriteLine("Person 1 connected");

                    listeningSocket.Listen(LISTENERBACKLOG);
                    sockets[1] = listeningSocket.Accept();
                    Console.WriteLine("Person 2 connected");

                    latestMessages.Add("WELCOME");

                    byte[] bufferOut = encoding.GetBytes(latestMessages[0]);

                    foreach (var item in sockets)
                    {
                        item.Send(bufferOut);
                    }

                    ParameterizedThreadStart threadStart1 = new ParameterizedThreadStart(ChatHandler);
                    Thread thread1 = new Thread(threadStart1);
                    thread1.Start(sockets[0]);

                    ParameterizedThreadStart threadStart2 = new ParameterizedThreadStart(ChatHandler);
                    Thread thread2 = new Thread(threadStart2);
                    thread2.Start(sockets[1]);
                }
            }

            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }

            finally
            {
                if (listeningSocket != null)
                    listeningSocket.Close();
            }
        }

        static void ChatHandler(object parameter)
        {
            Socket socket = (Socket)parameter;
            byte[] bufferOut = new byte[BUFFERLENGTH];
            string incomingMessage;
            string messages;

            while (true)
            {

                incomingMessage = RecieveMessage(socket);
                if (latestMessages.Count >= 3)
                {
                    latestMessages.RemoveAt(0);
                }

                latestMessages.Add(incomingMessage);

                messages = null;
                foreach (var message in latestMessages)
                {
                    if (latestMessages[latestMessages.Count - 1].Equals(message))
                    {
                        messages += message;
                    }

                    else
                    {
                        messages += message + ",";
                    }
                }

                bufferOut = encoding.GetBytes(messages);

                foreach (var item in sockets)
                {
                    item.Send(bufferOut);
                }
            }
        }

        static string RecieveMessage(Socket socket)
        {
            byte[] bufferIn = new Byte[BUFFERLENGTH];
            socket.Receive(bufferIn);

            string message = encoding.GetString(bufferIn).TrimEnd('\0').Trim();
            return message;
        }
    }
}
