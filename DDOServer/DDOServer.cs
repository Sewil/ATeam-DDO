using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DDODatabase;

namespace DDOServer
{
    public class DDOServer
    {
        public static ATeamDB db = new ATeamDB();
        static int serverID;
        static Socket socket = null;
        const int BUFFERLENGTH = 100;
        const Int32 LISTENERBACKLOG = 100;
        const Int32 BUFFERLENGTHMAP = 2000;
        const int BUFFERLENGTHPLAYER = 100;
        const String IPADDRESS = "127.0.0.1";
        static Int32 PORT;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint localEndPoint;
        static UTF8Encoding encoding = new UTF8Encoding();
        static IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 8000);

        static void ReceiveStartRequest(Socket socket, out string name)
        {
            name = null;

            byte[] bufferIn = new Byte[BUFFERLENGTHMAP];
            socket.Receive(bufferIn);

            string request = encoding.GetString(bufferIn).TrimEnd('\0').Trim();
            name = request;
        }

        static char RecieveDirection(Socket socket)
        {
            byte[] bufferIn = new Byte[BUFFERLENGTHMAP];
            socket.Receive(bufferIn);

            string request = encoding.GetString(bufferIn).TrimEnd('\0').Trim();
            return Convert.ToChar(request);
        }

        static void SendMap(Socket[] sockets, string mapStr)
        {
            byte[] bufferOut = encoding.GetBytes(mapStr);
            foreach (var socket in sockets)
            {
                socket.Send(bufferOut);
            }
        }

        static void SendPlayerInfo(Socket[] sockets, params Player[] players)
        {
            string response = null;
            byte[] bufferOut = new Byte[BUFFERLENGTHPLAYER];

            for (int i = 0; i < players.Length; i++)
            {
                response = ($"Name: {players[i].Name} Health: {players[i].Health} Damage: {players[i].Damage} Gold: {players[i].Gold}");
                bufferOut = encoding.GetBytes(response);
                sockets[i].Send(bufferOut);
            }
        }

        static void PlayDDO(object parameter)
        {
            Socket[] sockets = (Socket[])parameter;
            string playerOneName = null;
            string playerTwoName = null;
            Map map = null;
            string mapStr = null;
            char direction;


            try
            {
                ReceiveStartRequest(sockets[0], out playerOneName);
                ReceiveStartRequest(sockets[1], out playerTwoName);

                var players = new Player[sockets.Length];
                players[0] = new Player(playerOneName);
                players[1] = new Player(playerTwoName);

                map = Map.Load(players);
                mapStr = map.MapToString();

                SendMap(sockets, mapStr);
                SendPlayerInfo(sockets, players);

                while (true)
                {
                    map.SpawnMonster();
                    direction = RecieveDirection(sockets[0]);
                    map.MovePlayer(direction, players[0].Name);
                    map.SpawnHealthPotion();
                    mapStr = map.MapToString();
                    SendMap(sockets, mapStr);
                    SendPlayerInfo(sockets, players);

                    map.SpawnMonster();
                    direction = RecieveDirection(sockets[1]);
                    map.MovePlayer(direction, players[1].Name);
                    map.SpawnHealthPotion();
                    mapStr = map.MapToString();
                    SendMap(sockets, mapStr);
                    SendPlayerInfo(sockets, players);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: " + exception.Message);
                Console.WriteLine("Stack trace: " + exception.StackTrace);
                Console.ReadLine();
            }

            finally
            {
                if (sockets[0] != null)
                    sockets[0].Close();
                if (sockets[1] != null)
                    sockets[1].Close();
            }
        }

        static void ConnectToMasterServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);
            Console.WriteLine("Connected to MasterServer.");

            // Låt master-servern veta att det är en server som ansluter
            var request = "server";
            byte[] bufferOut = encoding.GetBytes(request);
            socket.Send(bufferOut);

            // Ta emot server-ID och Port från MasterServern
            byte[] bufferIn = new Byte[BUFFERLENGTH];
            socket.Receive(bufferIn);
            string response = encoding.GetString(bufferIn).TrimEnd('\0');
            var tokens = response.Split(' ');
            serverID = int.Parse(tokens[0]);
            PORT = int.Parse(tokens[1]);

            // Definiera localEndPoint med Porten vi fick från MasterServern
            localEndPoint = new IPEndPoint(ipAddress, PORT);
        }

        static void Main(string[] args)
        {
            Socket listeningSocket = null;
            Socket[] sockets = new Socket[2];
            ConnectToMasterServer();

            try
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                while (true)
                {
                    listeningSocket.Listen(LISTENERBACKLOG);
                    sockets[0] = listeningSocket.Accept();
                    Console.WriteLine("Player 1 connected");

                    string id1 = "1";
                    byte[] bufferOut1 = encoding.GetBytes(id1);
                    sockets[0].Send(bufferOut1);

                    listeningSocket.Listen(LISTENERBACKLOG);
                    sockets[1] = listeningSocket.Accept();
                    Console.WriteLine("Player 2 connected");

                    string id2 = "2";
                    byte[] bufferOut2 = encoding.GetBytes(id2);
                    sockets[1].Send(bufferOut2);

                    ParameterizedThreadStart threadStart = new ParameterizedThreadStart(PlayDDO);
                    Thread thread = new Thread(threadStart);
                    thread.Start(sockets);
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
    }
}
