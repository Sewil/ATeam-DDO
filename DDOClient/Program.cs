using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DDODatabase;
using DDOProtocol;
using Newtonsoft.Json;

namespace DDOClient {

    class Program {
        const int BUFFERLENGTHMAP = 2000;
        static Socket client = null;
        static IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001);
        static int? turn = null;
        static Protocol protocol = null;

        static void Main(string[] args) {
            bool connected = false;
            for (int i = 0; i < 10; i++) {
                try {
                    Console.WriteLine("Connecting to server...");
                    ConnectToServer();
                    connected = true;
                    Console.WriteLine("Connected succesfully.");
                    break;
                } catch {
                    Console.WriteLine("Connection failed. Trying again...");
                }
            }

            if (connected) {
                Console.Clear();
                Login();

                Player player = SelectPlayer();
                if (player != null) {
                    Console.WriteLine("Waiting for players...");
                    while (WaitingForPlayers()) {
                        Thread.Sleep(5000);
                    }
                    string map = ReceiveMap();
                    DrawMap(map);
                    Console.WriteLine(player);
                    while (true) {
                        if (turn % 2 == 1) {
                            AwaitInput();
                            map = ReceiveMap();
                        } else if (turn % 2 == 0) {
                            map = ReceiveMap();
                        }
                        player = GetPlayer();
                        turn++;
                        DrawMap(map);
                        Console.WriteLine($"Health: {player.Health}, Gold: {player.Gold}, Damage: {player.Damage}");
                    }
                } else {
                    Console.WriteLine("Couldn't select player.");
                }
            } else {
                Console.WriteLine("Couldn't connect to server after 10 tries. :(");
            }

            Console.ReadLine();
        }
        static void Login() {
            Response response = null;
            do {
                Console.WriteLine("LOGIN");
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();

                protocol.Send(new Request(RequestStatus.LOGIN, DataType.TEXT, $"{username} {password}"));
                response = (Response)protocol.Receive();

                Console.WriteLine(response.Status);
                Console.ReadKey();
                Console.Clear();
            } while (response.Status != ResponseStatus.OK);
        }
        static Player SelectPlayer() {
            protocol.Send(new Request(RequestStatus.GET_ACCOUNT_PLAYERS));
            var r = (Response)protocol.Receive();
            if (r.Status == ResponseStatus.OK && r.DataType == DataType.JSON) {
                var players = JsonConvert.DeserializeObject<Player[]>(r.Data);
                for (int i = 0; i < players.Length; i++) {
                    Console.WriteLine($"{i} - {players[i].Name}");
                }
                Console.WriteLine("Type in the number of your player: ");
                Player player = players[int.Parse(Console.ReadLine())];
                protocol.Send(new Request(RequestStatus.SELECT_PLAYER, DataType.JSON, JsonConvert.SerializeObject(player)));
                if ((protocol.Receive() as Response).Status == ResponseStatus.OK) {
                    return player;
                }
            } else {
                Console.WriteLine("Couldn't retrieve players from server. Probably because this account doesn't have any players.");
            }

            return null;
        }
        static bool WaitingForPlayers() {
            protocol.Send(new Request(RequestStatus.START));
            var response = (Response)protocol.Receive();
            return response.Status != ResponseStatus.OK;
        }
        static void MovePlayer(ConsoleKey key) {
            switch (key) {
                case ConsoleKey.UpArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.TEXT, "↑"));
                    break;

                case ConsoleKey.RightArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.TEXT, "→"));
                    break;

                case ConsoleKey.DownArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.TEXT, "↓"));
                    break;

                case ConsoleKey.LeftArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.TEXT, "←"));
                    break;
            }
        }
        static void DrawMap(string mapStr) {
            int WIDTH = 75;
            int HEIGHT = 20;
            int count = 0;
            char[,] mapCharArray = new char[HEIGHT, WIDTH];
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    mapCharArray[y, x] = mapStr[count];
                    count++;
                }
            }

            Console.Clear();

            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    if (mapCharArray[y, x] == '$') {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("$");
                    } else if (mapCharArray[y, x] == '@') {
                        if (turn % 2 == 1)
                            Console.ForegroundColor = ConsoleColor.Green;
                        else if (turn % 2 == 0)
                            Console.ForegroundColor = ConsoleColor.Red;

                        Console.Write("@");
                    } else if (mapCharArray[y, x] == 'P') {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("P");
                    } else if (mapCharArray[y, x] == 'M') {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("M");
                    } else if (mapCharArray[y, x] == '#') {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    } else if (mapCharArray[y, x] == '.') {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }
                Console.WriteLine();
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }
        static string ReceiveMap() {
            protocol.Send(new Request(RequestStatus.GET_MAP));
            var response = (Response)protocol.Receive(2000);
            if (response.Status == ResponseStatus.OK) {
                return response.Data;
            } else {
                return null;
            }
        }
        static Player GetPlayer() {
            protocol.Send(new Request(RequestStatus.GET_PLAYER));
            var r = (Response)protocol.Receive();
            if (r.Status == ResponseStatus.OK) {
                return JsonConvert.DeserializeObject<Player>(r.Data);
            } else {
                return null;
            }
        }
        static void ConnectToServer() {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(serverEndPoint);
            protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 100, client);
        }
        static bool InputCorrect(ConsoleKey key) {
            return key == ConsoleKey.UpArrow || key == ConsoleKey.RightArrow || key == ConsoleKey.DownArrow || key == ConsoleKey.LeftArrow;
        }
        static void AwaitInput() {
            ConsoleKey key;
            do {
                key = Console.ReadKey().Key;
                MovePlayer(key);
            } while (!InputCorrect(key));
        }
    }
}