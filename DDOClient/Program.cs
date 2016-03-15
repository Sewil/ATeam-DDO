using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DDOLibrary;
using DDOLibrary.GameObjects;
using DDOLibrary.Protocol;
using Newtonsoft.Json;

namespace DDOClient
{
    class Program
    {
        static readonly object locker = new object();
        static List<ChatMessage> chatLog = new List<ChatMessage>();
        const int BUFFERLENGTHMAP = 2000;
        static Socket client = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint mserverEndPoint = new IPEndPoint(ipAddress, 8000);
        static Protocol protocol = null;
        static Response serverResponse = null;
        static bool gameStarted = false;
        static State state = null;
        static bool chatOpen = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Enter server/master server ip: ");
            ipAddress = IPAddress.Parse(Console.ReadLine());
            mserverEndPoint = new IPEndPoint(ipAddress, 8000);

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
                protocol.MsgSize = 100000;
                Login();
                Console.WriteLine("Waiting for players...");
                serverRequest += StartGame;
                while (!gameStarted) { }
                protocol.MsgSize = 5000;
                Console.Clear();
                serverRequest += LogChat;
                serverRequest += UpdateMap;
                while (true) {
                    var key = Console.ReadKey().Key;

                    if (key == ConsoleKey.C) {
                        serverRequest -= UpdateMap;
                        OpenChat();
                        serverRequest += UpdateMap;
                    } else {
                        TryPlayerMove(key);
                    }
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
                Console.WriteLine($"({message.Sent.ToString("HH:mm:ss")}) {message.Name}: {message.Content}");
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
        static void StartGame(Request request) {
            if (request.Status == RequestStatus.START) {
                state = JsonConvert.DeserializeObject<State>(request.Data);
                Write();
                gameStarted = true;
            }
        }
        static void LogChat(Request request) {
            if(request.Status == RequestStatus.SEND_CHAT_MESSAGE && request.DataType == DataType.JSON) {
                var message = JsonConvert.DeserializeObject<ChatMessage>(request.Data);
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
        static void UpdateMap(Request request) {
            try {
                if (request.Status == RequestStatus.UPDATE_STATE && request.DataType == DataType.JSON) {
                    state = JsonConvert.DeserializeObject<State>(request.Data);
                    Write();
                }
            } catch {}
        }
        static void Write() {
            Console.Clear();
            var player = state.Player;
            string mapString = state.MapString;
            for (int i = 0; i < mapString.Length; i++) {
                string cell = mapString[i].ToString();
                if(cell == Environment.NewLine) {
                    Console.WriteLine();
                } else {
                    Console.Write(cell);
                }
            }
            Console.WriteLine($"Name: {player.Name} Health: {player.Health} Gold: {player.Gold} Damage: {player.Damage}");
        }
        static void GetServerList()
        {
            var masterServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            masterServer.Connect(mserverEndPoint);
            Protocol protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 2000, masterServer);

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
            mserverEndPoint = new IPEndPoint(ipAddress, serverPort);
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
        static void TryPlayerMove(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.JSON, JsonConvert.SerializeObject(Direction.Up)));
                    break;

                case ConsoleKey.RightArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.JSON, JsonConvert.SerializeObject(Direction.Right)));
                    break;

                case ConsoleKey.DownArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.JSON, JsonConvert.SerializeObject(Direction.Down)));
                    break;

                case ConsoleKey.LeftArrow:
                    protocol.Send(new Request(RequestStatus.MOVE, DataType.JSON, JsonConvert.SerializeObject(Direction.Left)));
                    break;
            }
            ServerReceive();
        }
        static void ConnectToServer()
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(mserverEndPoint);
            protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 5000, client);
        }
    }
}