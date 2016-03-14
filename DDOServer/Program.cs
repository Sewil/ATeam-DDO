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

namespace DDOServer {
    public class State {
        public string MapStr { get; }
        public List<Player> Players { get; }
        public List<Monster> Monsters { get; set; }
        public List<Potion> Potions { get; set; }
        public DDODatabase.Player Player { get; set; }
        public State(string mapStr, List<Player> players, List<Monster> monsters, List<Potion> potions, DDODatabase.Player player) {
            MapStr = mapStr;
            Players = players;
            Monsters = monsters;
            Potions = potions;
            Player = player;
        }
    }
    public class ChatMessage {
        public DateTime Sent { get; }
        public string Content { get; }
        public string Name { get; }

        public ChatMessage(DateTime sent, string content, string name) {
            Sent = sent;
            Content = content;
            Name = name;
        }
    }
    public class Client {
        public bool IsHeard { get; set; }
        public Protocol Protocol { get; }
        public bool IsLoggedIn { get { return Account != null; } }
        public bool HasSelectedPlayer { get { return SelectedPlayer != null; } }
        public Account Account { get; set; }
        public DDODatabase.Player SelectedPlayer { get; set; }
        public string IPAddress {
            get {
                if (Protocol != null && Protocol.Socket != null) {
                    return Protocol.Socket.RemoteEndPoint.ToString();
                } else {
                    return null;
                }
            }
        }
        public Client(Protocol protocol) {
            Protocol = protocol;
        }
    }
    public class Program {
        static readonly object locker = new object();
        public static ATeamDB db = new ATeamDB();
        const int LISTENER_BACKLOG = 100;
        static Socket server = null;
        static Socket masterServer = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint serverEndPoint = new IPEndPoint(ipAddress, 8001);
        static IPEndPoint masterServerEndPoint = new IPEndPoint(ipAddress, 8000);

        const int MAX_LOGGED_IN_CLIENTS = 5;
        const int MINIMUM_PLAYERS = 1;
        const int MAX_CONNECTED_CLIENTS = 10;
        static List<Client> clients = new List<Client>();
        static IEnumerable<Client> LoggedInClients {
            get {
                lock (clients) {
                    return clients.Where(c => c.IsLoggedIn);
                }
            }
        }
        static IEnumerable<Client> PlayerClients {
            get {
                lock (clients) {
                    return clients.Where(c => c.HasSelectedPlayer);
                }
            }
        }

        static bool gameStarted = false;
        static Map map = null;
        static void Main(string[] args) {
            Console.WriteLine("Enter server ip: ");
            ipAddress = IPAddress.Parse(Console.ReadLine());

            ConnectToMasterServer();
            try {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(serverEndPoint);
                server.Listen(LISTENER_BACKLOG);

                new Thread(new ParameterizedThreadStart(AcceptClients)).Start();
                new Thread(new ParameterizedThreadStart(ListenToClientRequests)).Start();
                while (true) {
                }
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

                        if (!countingDown && !gameStarted && PlayerClients.Count() >= MINIMUM_PLAYERS) {
                            countingDown = true;
                            Console.WriteLine("Starting game in 10 seconds...");
                            new TaskFactory().StartNew(() => {
                                Thread.Sleep(10000);
                                StartGame();
                            });
                        }
                    }
                }
            }
        }
        static void HandleClientRequest(Client client, Request request) {
            switch (request.Status) {
                case RequestStatus.SendChatMessage: SendChatMessage(client, request); break;
                case RequestStatus.Move: MovePlayer(client, request); break;
                case RequestStatus.Login: Login(client, request); break;
                case RequestStatus.GetState: GetState(client, request); break;
                case RequestStatus.GetPlayer: GetPlayerInfo(client, request); break;
                case RequestStatus.GetAccountPlayers: GetAccountPlayers(client, request); break;
                case RequestStatus.SelectPlayer: SelectPlayer(client, request); break;
            }
        }

        static void SendChatMessage(Client client, Request request) {
            if (request.Status == RequestStatus.SendChatMessage && request.DataType == DataType.Json && client.HasSelectedPlayer) {
                Send(client, new Response(ResponseStatus.OK));
                var r = new Request(RequestStatus.SendChatMessage, DataType.Json, request.Data);
                foreach (var c in PlayerClients.Where(c => c != client)) {
                    Send(c, r);
                }
            } else {
                Send(client, new Response(ResponseStatus.BadRequest));
            }
        }

        static void SelectPlayer(Client client, Request request) {
            if (request.Status == RequestStatus.SelectPlayer && request.DataType == DataType.Json) {
                if (client.IsLoggedIn) {
                    client.SelectedPlayer = JsonConvert.DeserializeObject<DDODatabase.Player>(request.Data);
                    Send(client, new Response(ResponseStatus.OK));
                } else {
                    Send(client, new Response(ResponseStatus.Unauthorized));
                }
            } else {
                Send(client, new Response(ResponseStatus.BadRequest));
            }
        }

        static void GetAccountPlayers(Client client, Request request) {
            if (request.Status == RequestStatus.GetAccountPlayers && client.IsLoggedIn) {
                if (db.Players.Any(p => p.AccountId == client.Account.Id)) {
                    var players = db.Players.Where(p => p.AccountId == client.Account.Id).Select(p => new {
                        AccountId = p.AccountId, Damage = p.Damage, Gold = p.Gold, Health = p.Health, Id = p.Id, Name = p.Name
                    }).ToArray();
                    Send(client, new Response(ResponseStatus.OK, DataType.Json, JsonConvert.SerializeObject(players)));
                } else {
                    Send(client, new Response(ResponseStatus.NotFound));
                }
            }
        }
        
        static void StartGame() {
            lock (locker) {
                if (!gameStarted) {
                    foreach (var client in LoggedInClients) {
                        Send(client, new Request(RequestStatus.Start));
                    }
                    
                    var players = new List<Player>();
                    lock (clients) {
                        var loggedInClients = LoggedInClients.ToArray();
                        for (int i = 0; i < loggedInClients.Length; i++) {
                            var client = loggedInClients[i];

                            var sp = client.SelectedPlayer;
                            var player = new Player(sp.Name, sp.Health, sp.Damage, sp.Gold, Player.icons.ElementAt(i));
                            players.Add(player);
                        }
                    }
                    SetPlayersIds(players);
                    map = Map.Load(players.ToArray());

                    gameStarted = true;

                    map.Changed += (m) => {
                        SendStates();
                        Thread.Sleep(500);
                    };
                }
            }
        }

        static void SetPlayersIds(List<Player> playerList)
        {
            int count = 1;
            if(count < 5)
            foreach (var player in playerList)
            {
                player.Id = count++;
            }
        }
        static void SendStates(Client excludedClient = null) {
            State state = new State(map.MapToString(), map.players, map.monsters, map.potions, null);
            lock (locker) {
                foreach (var c in PlayerClients.Where(c => c != excludedClient)) {
                    state.Player = c.SelectedPlayer;
                    Send(c, new Request(RequestStatus.WriteState, DataType.Json, JsonConvert.SerializeObject(state)));
                }
            }
        }
        static void Login(Client client, Request request) {
            lock (locker) {
                if (request.Status != RequestStatus.Login) {
                    Send(client, new Response(ResponseStatus.BadRequest));
                } else if (client.IsLoggedIn) {
                    Send(client, new Response(ResponseStatus.BadRequest, DataType.Text, "You are already logged in."));
                } else if (LoggedInClients.Count() >= MAX_LOGGED_IN_CLIENTS) {
                    Send(client, new Response(ResponseStatus.LimitReached, DataType.Text, $"Only {MAX_LOGGED_IN_CLIENTS} users can be logged in at a time."));
                } else {
                    string message = request.Data;
                    string username = message.Split(' ')[0];
                    Account account = db.Accounts.SingleOrDefault(u => u.Username == username);


                    if (account == null) {
                        Send(client, new Response(ResponseStatus.NotFound, DataType.Text, "This user does not exist."));
                    } else if (LoggedInClients.Any(c => c.Account == account)) {
                        Send(client, new Response(ResponseStatus.Unauthorized, DataType.Text, "This user is already logged in."));
                    } else {
                        string password = message.Split(' ')[1];

                        if (password != account.Password) {
                            Send(client, new Response(ResponseStatus.Unauthorized, DataType.Text, "Incorrect password."));
                        } else {
                            client.Account = account;
                            Send(client, new Response(ResponseStatus.OK));
                        }
                    }
                }
            }
        }
        static void GetPlayerInfo(Client client, Request request) {
            if (request.Status == RequestStatus.GetPlayer) {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    Send(client, new Response(ResponseStatus.OK, DataType.Json, JsonConvert.SerializeObject(client.SelectedPlayer)));
                } else {
                    Send(client, new Response(ResponseStatus.Unauthorized));
                }
            } else {
                Send(client, new Response(ResponseStatus.BadRequest));
            }
        }
        static void MovePlayer(Client client, Request request) {
            if (request.Status != RequestStatus.Move || !gameStarted || request.DataType != DataType.Json) {
                Send(client, new Response(ResponseStatus.BadRequest));
            } else {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    var direction = JsonConvert.DeserializeObject<Direction>(request.Data);
                    map.MovePlayer(direction, client.SelectedPlayer);
                    Send(client, new Response(ResponseStatus.OK));
                } else {
                    Send(client, new Response(ResponseStatus.Unauthorized));
                }
            }
        }
        static void GetState(Client client, Request request) {
            lock (locker) {
                if (request.Status == RequestStatus.GetState && gameStarted) {
                    State state = new State(map.MapToString(), map.players, map.monsters, map.potions, client.SelectedPlayer);
                    Send(client, new Response(ResponseStatus.OK, DataType.Json, JsonConvert.SerializeObject(state)));
                    SendStates(client);
                } else {
                    Send(client, new Response(ResponseStatus.NotReady, DataType.Text, "GAME NOT STARTED"));
                }
            }
        }
        static void Send(Client client, Message message) {
            client.Protocol.Send(message);
            Console.WriteLine($"{DateTime.Now}\t[{serverEndPoint} --> {client.IPAddress}]\t{client.Protocol.GetMessage(message)}");
        }
        static Message Receive(Client client, int msgSizeOverride = 0) {
            if(msgSizeOverride == 0) {
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
            p.Send(new Request(RequestStatus.None, DataType.Text, "im a server i promise"));

            var r = p.Receive() as Response;
            if (r.Status == ResponseStatus.OK) {
                serverEndPoint = new IPEndPoint(ipAddress, int.Parse(r.Data));
            }
        }
    }
}