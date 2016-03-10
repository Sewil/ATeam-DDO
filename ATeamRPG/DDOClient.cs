using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class DDOClient
    {
        const string IPADDRESS = "10.42.104.189";
        const int PORT = 8001;
        static Socket socket = null;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, PORT);

        public static void ConnectToServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);
            Console.WriteLine("Connected to server.");
            Console.ReadLine();
        }
    }
}
