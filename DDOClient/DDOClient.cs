using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DDOClient
{
    class DDOClient
    {
        const int BUFFERLENGTHMAP = 2000;
        const int BUFFERLENGTH = 100;
        const string IPADDRESS = "127.0.0.1";
        static int PORT;
        static Socket socket = null;
        static IPAddress ipAddress = IPAddress.Parse(IPADDRESS);
        static IPEndPoint masterServerEndPoint = new IPEndPoint(ipAddress, 8000);
        static IPEndPoint targetServerEndPoint;
        static UTF8Encoding encoding = new UTF8Encoding();
        static ConsoleColor playerOneColor = ConsoleColor.Green;
        static ConsoleColor playerTwoColor = ConsoleColor.Red;

        static string playerName = null;
        static string opponent = null;
        static int? turnID = null;

        static void ConnectToServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            targetServerEndPoint = new IPEndPoint(ipAddress, PORT);
            socket.Connect(targetServerEndPoint);
            Console.WriteLine("Connected to server.");
            Console.Write("Enter name: ");
            playerName = Console.ReadLine();
            Console.WriteLine("Welcome to DDO.");
            Console.WriteLine("Waiting for players...");
        }

        static void InitiateGame()
        {
            string request = $"DDO/1.0 START {playerName}";
            byte[] bufferOut = encoding.GetBytes(request);
            socket.Send(bufferOut);
            Console.WriteLine("Request sent: " + request);

            byte[] bufferIn = new Byte[BUFFERLENGTH];
            socket.Receive(bufferIn);
            string response = encoding.GetString(bufferIn).TrimEnd('\0');
            turnID = int.Parse(response);
            Console.WriteLine("Id set");
        }

        static void SendRequest(char direction)
        {
            string response = direction.ToString();
            byte[] bufferOut = encoding.GetBytes(response);
            socket.Send(bufferOut);
        }

        static void MovePlayer(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    SendRequest('↑');
                    break;

                case ConsoleKey.RightArrow:
                    SendRequest('→');
                    break;

                case ConsoleKey.DownArrow:
                    SendRequest('↓');
                    break;

                case ConsoleKey.LeftArrow:
                    SendRequest('←');
                    break;
            }
        }

        static void DrawMap(string mapStr)
        {
            int WIDTH = 75;
            int HEIGHT = 20;
            int count = 0;
            char[,] mapCharArray = new char[HEIGHT, WIDTH];

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    mapCharArray[y, x] = mapStr[count];
                    count++;
                }
            }

            Console.Clear();

            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (mapCharArray[y, x] == '$')
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("$");
                    }

                    else if (mapCharArray[y, x] == '@')
                    {
                        if (turnID % 2 == 1)
                            Console.ForegroundColor = ConsoleColor.Green;
                        else if (turnID % 2 == 0)
                            Console.ForegroundColor = ConsoleColor.Red;

                        Console.Write("@");
                    }

                    else if (mapCharArray[y, x] == 'P')
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("P");
                    }

                    else if (mapCharArray[y, x] == 'M')
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("M");
                    }

                    else if (mapCharArray[y, x] == '#')
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    }

                    else if (mapCharArray[y, x] == '.')
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }
                Console.WriteLine();
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        static string ReceiveMap()
        {
            byte[] bufferIn = new Byte[BUFFERLENGTHMAP];
            socket.Receive(bufferIn);
            string response = encoding.GetString(bufferIn).TrimEnd('\0');
            return response;
        }

        static string ReceivePlayerInfo()
        {
            byte[] bufferIn = new Byte[BUFFERLENGTH];
            socket.Receive(bufferIn);

            string response = encoding.GetString(bufferIn).TrimEnd('\0');
            return response;
        }

        static bool InputCorrect(ConsoleKey key)
        {
            return key == ConsoleKey.UpArrow || key == ConsoleKey.RightArrow || key == ConsoleKey.DownArrow || key == ConsoleKey.LeftArrow;
        }

        static void AwaitInput()
        {
            ConsoleKey key;

            do
            {
                key = Console.ReadKey().Key;
                MovePlayer(key);
            } while (!InputCorrect(key));
        }

        static void GetServerList()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(masterServerEndPoint);

            var request = "list";
            byte[] bufferOut = encoding.GetBytes(request);
            socket.Send(bufferOut);
            byte[] bufferIn = new byte[BUFFERLENGTH];
            socket.Receive(bufferIn);
            var response = encoding.GetString(bufferIn).TrimStart(' ').TrimEnd(' ').TrimEnd('\0').Split(' ');

            Console.WriteLine("List of servers:");
            foreach (var server in response)
            {
                if (server.Length > 1)
                    Console.WriteLine();
                else
                    Console.WriteLine($"ServerID: {server}");
            }

            Console.Write("Enter server id> ");

            int input = int.Parse(Console.ReadLine());
            int serverPort = int.Parse(response[input + 1]);
            PORT = serverPort;

            socket.Close();
            Console.Clear();
        }

        static void Main(string[] args)
        {
            GetServerList();
            ConnectToServer();

            InitiateGame();
            string map = ReceiveMap();
            string playerInfo = ReceivePlayerInfo();
            DrawMap(map);
            Console.WriteLine(playerInfo);

            while (true)
            {
                if (turnID % 2 == 1)
                {
                    AwaitInput();
                    map = ReceiveMap();
                }
                else if (turnID % 2 == 0)
                {
                    map = ReceiveMap();
                }
                playerInfo = ReceivePlayerInfo();
                turnID++;
                DrawMap(map);
                Console.WriteLine(playerInfo);
            }

        }

        //var player = new Player(playerName);
        //Console.CursorVisible = false;
        //Map map = Map.Load(player);
        //player.IsActive = true;
        //map.Draw();
        //do {
        //    map.SpawnMonster(); // Bg-thread?
        //    ConsoleKey key = Console.ReadKey().Key;
        //    Console.Clear();
        //    if (map.Turn % 2 == 0) {
        //        if(key == ConsoleKey.D) {
        //            playerOne.OnDied();
        //        }

        //        map.MovePlayer(key, playerOne);
        //        playerOne.IsActive = false;
        //        playerTwo.IsActive = true;
        //    } else {
        //        map.MovePlayer(key, playerTwo);
        //        playerOne.IsActive = true;
        //        playerTwo.IsActive = false;
        //    }
        //    map.SpawnHealthPotion(); // Bg-thread?
        //    map.Draw();
        //} while (true);
    }
}
