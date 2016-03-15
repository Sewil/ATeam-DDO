using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DDOLibrary;
using DDOLibrary.GameObjects;
using DDOLibrary.Protocol;
using Newtonsoft.Json;

namespace DDOServer {
    public class Program {
        static readonly object locker = new object();
        public static ATeamEntities db = new ATeamEntities();
        const int LISTENER_BACKLOG = 100;
        static Socket server = null;
        static Socket masterServer = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint serverEndPoint = new IPEndPoint(ipAddress, 8001);
        static IPEndPoint masterServerEndPoint = new IPEndPoint(ipAddress, 8000);

        const int MAX_LOGGED_IN_CLIENTS = 5;
        const int MAX_CONNECTED_CLIENTS = 10;
        const int MINIMUM_PLAYERS = 1;
        static List<Client> clients = new List<Client>();

        static bool gameStarted = false;
        static Map map = null;
        static void Main(string[] args) {
            Console.WriteLine("Enter server ip: ");
            ipAddress = IPAddress.Parse(Console.ReadLine());
            serverEndPoint = new IPEndPoint(ipAddress, 8001);
            masterServerEndPoint = new IPEndPoint(ipAddress, 8000);

            ConnectToMasterServer();
            try {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(serverEndPoint);
                server.Listen(LISTENER_BACKLOG);

                new Thread(new ParameterizedThreadStart(AcceptClients)).Start();
                new Thread(new ParameterizedThreadStart(ListenToClientRequests)).Start();
                while (true) { }
            } catch (Exception exception) {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            } finally {
                if (server != null) {
                    server.Close();
                }
            }
        }
        static void AcceptClients(object arg) {
            Console.WriteLine($"{DateTime.Now}\tAccepting clients...");
            while (true) {
                if (clients.Count < MAX_CONNECTED_CLIENTS) {
                    var socket = server.Accept();
                    var client = new Client(new Protocol("DDO/1.0", new UTF8Encoding(), 5000, socket));
                    lock (clients) {
                        clients.Add(client);
                    }
                    Console.WriteLine($"{DateTime.Now}\tClient accepted ({client.IPAddress})");
                }
            }
        }
        static void ListenToClientRequests(object arg) {
            Console.WriteLine($"{DateTime.Now}\tListening to client requests...");
            bool countingDown = false;
            while (true) {
                if (clients.Count > 0) {
                    lock (clients) {
                        foreach (var client in clients) {
                            if (client != null && !client.IsHeard) {
                                client.IsHeard = true;
                                new TaskFactory().StartNew(() => {
                                    var transfer = Receive(client);
                                    HandleClientRequest(client, (Request)transfer);
                                    client.IsHeard = false;
                                });
                            }
                        }

                        if (!countingDown && !gameStarted && clients.Count(c => c.IsLoggedIn) >= MINIMUM_PLAYERS) {
                            countingDown = true;
                            //Console.WriteLine("Starting game in 10 seconds...");
                            //new TaskFactory().StartNew(() => {
                                StartGame();
                                //Thread.Sleep(10000);
                            //});
                        }
                    }
                }
            }
        }
        static void HandleClientRequest(Client client, Request request) {
            switch (request.Status) {
                case RequestStatus.SEND_CHAT_MESSAGE: SendChatMessage(client, request); break;
                case RequestStatus.MOVE: MovePlayer(client, request); break;
                case RequestStatus.LOGIN: Login(client, request); break;
            }
        }

        static void SendChatMessage(Client client, Request request) {
            if (request.Status == RequestStatus.SEND_CHAT_MESSAGE && request.DataType == DataType.JSON && client.IsLoggedIn) {
                Send(client, new Response(ResponseStatus.OK));
                var r = new Request(RequestStatus.SEND_CHAT_MESSAGE, DataType.JSON, request.Data);
                foreach (var c in clients.Where(c => c.IsLoggedIn && c != client)) {
                    Send(c, r);
                }
            } else {
                Send(client, new Response(ResponseStatus.BAD_REQUEST));
            }
        }

        static void StartGame() {
            if (!gameStarted) {
                gameStarted = true;
                Client[] loggedInClients;
                lock (clients) {
                    loggedInClients = clients.Where(c => c.IsLoggedIn).ToArray();
                }

                var players = new List<Player>();
                for (int i = 0; i < loggedInClients.Length; i++) {
                    var client = loggedInClients[i];

                    var player = new Player(Player.IdCounter++, 20, 20, 0, Player.icons.ElementAt(i));
                    players.Add(player);
                    client.Player = player;
                }

                var playerArray = players.ToArray();
                map = Map.Load(playerArray);

                var state = new State(map.MapToString(), null);
                foreach (var client in loggedInClients) {
                    state.Player = client.Player;
                    var data = JsonConvert.SerializeObject(state);
                    Send(client, new Request(RequestStatus.START, DataType.JSON, data));
                }

                map.Changed += (m) => {
                    UpdateState();
                    Thread.Sleep(500);
                };
            }
        }

        static void UpdateState(Client excludedClient = null) {
            var state = new State(map.MapToString(), null);
            foreach (var client in clients) {
                state.Player = client.Player;
                var data = JsonConvert.SerializeObject(state);
                Send(client, new Request(RequestStatus.UPDATE_STATE, DataType.JSON, data));
            }
        }
        static void Login(Client client, Request request) {
            lock (client) {
                if (request.Status != RequestStatus.LOGIN) {
                    Send(client, new Response(ResponseStatus.BAD_REQUEST));
                } else if (client.IsLoggedIn) {
                    Send(client, new Response(ResponseStatus.BAD_REQUEST, DataType.TEXT, "You are already logged in."));
                } else if (clients.Count(c => c.IsLoggedIn) >= MAX_LOGGED_IN_CLIENTS) {
                    Send(client, new Response(ResponseStatus.LIMIT_REACHED, DataType.TEXT, $"Only {MAX_LOGGED_IN_CLIENTS} users can be logged in at a time."));
                } else {
                    string message = request.Data;
                    string username = message.Split(' ')[0];
                    Account account = db.Account.SingleOrDefault(u => u.Username == username);

                    if (account == null) {
                        Send(client, new Response(ResponseStatus.NOT_FOUND, DataType.TEXT, "This user does not exist."));
                    } else if (clients.Any(c => c.IsLoggedIn && c.Account == account)) {
                        Send(client, new Response(ResponseStatus.UNAUTHORIZED, DataType.TEXT, "This user is already logged in."));
                    } else {
                        string password = message.Split(' ')[1];

                        if (password != account.Password) {
                            Send(client, new Response(ResponseStatus.UNAUTHORIZED, DataType.TEXT, "Incorrect password."));
                        } else {
                            client.Account = account;
                            Send(client, new Response(ResponseStatus.OK));
                        }
                    }
                }
            }
        }
        static void MovePlayer(Client client, Request request) {
            if (request.Status != RequestStatus.MOVE || !gameStarted || request.DataType != DataType.JSON) {
                Send(client, new Response(ResponseStatus.BAD_REQUEST));
            } else {
                if (client.IsLoggedIn) {
                    var direction = JsonConvert.DeserializeObject<Direction>(request.Data);
                    map.MovePlayer(direction, client.Player);
                    Send(client, new Response(ResponseStatus.OK));
                } else {
                    Send(client, new Response(ResponseStatus.UNAUTHORIZED));
                }
            }
        }
        static void Send(Client client, Message message) {
            client.Protocol.Send(message);
            Console.WriteLine($"{DateTime.Now}\t[{serverEndPoint} --> {client.IPAddress}]\t{client.Protocol.GetMessage(message)}");
        }
        static Message Receive(Client client, int msgSizeOverride = 0) {
            if (msgSizeOverride == 0) {
                msgSizeOverride = client.Protocol.MsgSize;
            }

            Message transfer = client.Protocol.Receive();
            Console.WriteLine($"{DateTime.Now}\t[{serverEndPoint} <-- {client.IPAddress}]\t{client.Protocol.GetMessage(transfer)}");

            return transfer;
        }
        static void ConnectToMasterServer() {
            masterServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            masterServer.Connect(masterServerEndPoint);
            Protocol p = new Protocol("DDO/1.0", new UTF8Encoding(), 100, masterServer);
            Console.WriteLine($"{DateTime.Now}\tConnected to MasterServer");

            // Låt master-servern veta att det är en server som ansluter
            p.Send(new Request(RequestStatus.NONE, DataType.TEXT, "im a server i promise"));

            var r = p.Receive() as Response;
            if (r.Status == ResponseStatus.OK) {
                serverEndPoint = new IPEndPoint(ipAddress, int.Parse(r.Data));
            }
        }
    }
}