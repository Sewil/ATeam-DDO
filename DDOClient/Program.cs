using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DDOProtocol;
using DDODatabase;
using DDOServer;
using Newtonsoft.Json;

namespace DDOClient
{
    class Program
    {
        static readonly object locker = new object();
        static List<ChatMessage> chatLog = new List<ChatMessage>();
        const int BUFFERLENGTHMAP = 2000;
        static Socket client = null;
        static IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
        static Protocol protocol = null;
        static Response serverResponse = null;
        static bool gameStarted = false;
        static State state = null;
        static bool chatOpen = false;
        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            GetServerList();
            bool connected = false;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Console.WriteLine("Connecting to server...");
                    ConnectToServer();
                    connected = true;
                    Console.WriteLine("Connected succesfully.");
                    break;
                }
                catch
                {
                    Console.WriteLine("Connection failed. Trying again...");
                }
            }

            if (connected)
            {
                new Thread(new ParameterizedThreadStart(ServerListener)).Start();

                Console.Clear();
                Login();
                if (SelectPlayer())
                {
                    Console.WriteLine("Waiting for players...");
                    serverRequest += GameStarter;
                    while (!gameStarted) { }
                    Console.Clear();
                    serverRequest += ChatLogger;
                    serverRequest += StateWriter;
                    while (true)
                    {
                        var key = Console.ReadKey().Key;

                        if(key == ConsoleKey.C) {
                            serverRequest -= StateWriter;
                            OpenChat();
                            serverRequest += StateWriter;
                        } else {
                            TryPlayerMove(key);
                        }
                        State state = GetState();
                        if (state != null) {
                            Program.state = state;
                            WriteState(state);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Couldn't select player.");
                }
            }
            else
            {
                Console.WriteLine("Couldn't connect to server after 10 tries. :(");
            }

            Console.ReadLine();
        }
        static void RefreshChat() {
            Console.Clear();
            Console.WriteLine("CHAT\n---------------------------");
            foreach (var message in chatLog) {
                Console.WriteLine($"({message.Sent.ToString("HH:mm:ss")}) {message.PlayerName}: {message.Content}");
            }
        }
        static void OpenChat() {
            chatOpen = true;
            while (true) {
                RefreshChat();

                string content = ReadLineWithCancel();
                if (content == null) {
                    break;
                } else {
                    var message = new ChatMessage(DateTime.Now, content, state.Player.Name);
                    chatLog.Add(message);
                    var r = ServerRequest(new Request(RequestStatus.SEND_CHAT_MESSAGE, DataType.JSON, JsonConvert.SerializeObject(message)));
                    if(r.Status != ResponseStatus.OK) {
                        Console.WriteLine("Couldn't send message. Press anywhere to continue...");
                        Console.ReadKey(true);
                    }
                }
            }
            chatOpen = false;
        }
        static string ReadLineWithCancel() {
            var buffer = new StringBuilder();

            var info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Escape && info.Key != ConsoleKey.Enter) {
                Console.Write(info.KeyChar);
                buffer.Append(info.KeyChar);
                info = Console.ReadKey(true);
            }

            if(info.Key == ConsoleKey.Enter) {
                return buffer.ToString();
            } else {
                return null;
            }
        }
        static void GameStarter(Request r) {
            if (r.Status == RequestStatus.START) {
                gameStarted = true;
            }
        }
        static void ChatLogger(Request r) {
            if(r.Status == RequestStatus.SEND_CHAT_MESSAGE && r.DataType == DataType.JSON) {
                var message = JsonConvert.DeserializeObject<ChatMessage>(r.Data);
                chatLog.Add(message);
                if (chatOpen) {
                    RefreshChat();
                }
            }
        }

        static event Action<Request> serverRequest;
        static void OnServerRequest(Request r) {
            serverRequest?.Invoke(r);
        }

        static void ServerListener(object arg) {
            while (client.Connected) {
                var transfer = protocol.Receive();
                if(transfer is Request) {
                    OnServerRequest(transfer as Request);
                } else if(transfer is Response) {
                    serverResponse = transfer as Response;
                }
            }
        }
        static void StateWriter(Request request) {
            if(request.Status == RequestStatus.WRITE_STATE) {
                State state = JsonConvert.DeserializeObject<State>(request.Data);
                Program.state = state;
                WriteState(state);
            }
        }
        static void GetServerList()
        {
            var masterServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            masterServer.Connect(serverEndPoint);
            protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 2000, masterServer);

            protocol.Send(new Request(RequestStatus.NONE, DataType.TEXT, "list"));
            var r = protocol.Receive() as Response;
            var response = r.Data.TrimStart(' ').Split(' ');
            Console.WriteLine("List of servers:");
            foreach (var server in response)
            {
                if (server.Length > 1)
                    Console.WriteLine();
                else
                    Console.WriteLine($"ServerID: {server}");
            }
            Console.Write("Enter server id: ");
            int input = int.Parse(Console.ReadLine());
            int serverPort = int.Parse(response[input + 1]);
            masterServer.Close();
            serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
            Console.Clear();
        }
        static void Login()
        {
            Response response = null;
            do
            {
                Console.WriteLine("LOGIN");
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();

                response = ServerRequest(new Request(RequestStatus.LOGIN, DataType.TEXT, $"{username} {password}"));

                Console.WriteLine(response.Status);
                Console.ReadKey();
                Console.Clear();
            } while (response.Status != ResponseStatus.OK);
        }
        static Response ServerReceive() {
            Response res = serverResponse = null;
            while (res == null) {
                res = serverResponse;
            }
            return res;
        }
        static Response ServerRequest(Request req) {
            Response r = null;
            protocol.Send(req);
            r = ServerReceive();

            return r;
        }
        static bool SelectPlayer()
        {
            Response r = ServerRequest(new Request(RequestStatus.GET_ACCOUNT_PLAYERS));

            if(r.DataType != DataType.JSON) {
                Console.WriteLine("Corrupt response from server. Invalid datatype.");
            } else if(r.Status != ResponseStatus.OK) {
                Console.WriteLine("Couldn't retrieve players from server. Probably because this account doesn't have any players.");
            } else {
                var players = JsonConvert.DeserializeObject<Player[]>(r.Data);
                for (int i = 0; i < players.Length; i++) {
                    Console.WriteLine($"{i} - {players[i].Name}");
                }
                Console.WriteLine("Type in the number of your player: ");
                Player player = players[int.Parse(Console.ReadLine())];

                r = ServerRequest(new Request(RequestStatus.SELECT_PLAYER, DataType.JSON, JsonConvert.SerializeObject(player)));

                if (r.Status == ResponseStatus.OK) {
                    return true;
                } else {
                    Console.WriteLine("Error selecting player.");
                }
            }

            return false;
        }
        static void TryPlayerMove(ConsoleKey key)
        {
            switch (key)
            {
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
            ServerReceive();
        }
        static void WriteState(State state)
        {
            lock (locker) {
                string mapStr = state.MapStr;
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
                            Console.ForegroundColor = ConsoleColor.Green;
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
                Console.WriteLine($"Health: {state.Player.Health}, Gold: {state.Player.Gold}, Damage: {state.Player.Damage}");
            }
        }
        static State GetState()
        {
            var r = ServerRequest(new Request(RequestStatus.GET_STATE));
            if (r.DataType == DataType.JSON && r.Status == ResponseStatus.OK) {
                var state = JsonConvert.DeserializeObject<State>(r.Data);
                return state;
            } else {
                return null;
            }
        }
        static void ConnectToServer()
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(serverEndPoint);
            protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 5000, client);
        }
    }
}