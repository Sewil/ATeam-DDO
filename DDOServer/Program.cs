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
        public string MapStr { get; set; }
        public DDODatabase.Player Player { get; set; }
    }
    public class Message {
        public DateTime Sent { get; set; }
        public string Content { get; set; }
        public string PlayerName { get; set; }
    }
    public class Client {
        public bool IsHeard { get; set; }
        public Socket Socket { get; set; }
        public bool IsLoggedIn { get { return Account != null; } }
        public bool HasSelectedPlayer { get { return SelectedPlayer != null; } }
        public Account Account { get; set; }
        public DDODatabase.Player SelectedPlayer { get; set; }
        public string IPAddress {
            get {
                if (Socket != null) {
                    return Socket.RemoteEndPoint.ToString();
                } else {
                    return null;
                }
            }
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

        const int MAX_LOGGED_IN_CLIENTS = 2;
        const int MAX_CONNECTED_CLIENTS = 10;
        static List<Client> clients = new List<Client>();
        static IEnumerable<Client> LoggedInClients { get { return clients.Where(c => c.IsLoggedIn); } }
        static IEnumerable<Client> PlayerClients { get { return clients.Where(c => c.HasSelectedPlayer); } }

        static bool gameStarted = false;
        static Map map = null;
        static void Main(string[] args) {
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
        static Response HandleClientRequest(Client client, Request request) {
            switch (request.Status) {
                case RequestStatus.MOVE: return MovePlayer(client, request);
                case RequestStatus.LOGIN: return Login(client, request);
                case RequestStatus.GET_STATE: return GetState(client, request);
                case RequestStatus.GET_PLAYER: return GetPlayerInfo(client, request);
                case RequestStatus.GET_ACCOUNT_PLAYERS: return GetAccountPlayers(client, request);
                case RequestStatus.SELECT_PLAYER: return SelectPlayer(client, request);
                default: return null;
            }
        }

        static Response SelectPlayer(Client client, Request request) {
            if (request.Status == RequestStatus.SELECT_PLAYER && request.DataType == DataType.JSON) {
                if (client.IsLoggedIn) {
                    client.SelectedPlayer = JsonConvert.DeserializeObject<DDODatabase.Player>(request.Data);
                    return new Response(ResponseStatus.OK);
                } else {
                    return new Response(ResponseStatus.UNAUTHORIZED);
                }
            } else {
                return new Response(ResponseStatus.BAD_REQUEST);
            }
        }

        static Response GetAccountPlayers(Client client, Request request) {
            if (request.Status == RequestStatus.GET_ACCOUNT_PLAYERS && client.IsLoggedIn) {
                if (db.Players.Any(p => p.AccountId == client.Account.Id)) {
                    var players = db.Players.Where(p => p.AccountId == client.Account.Id).Select(p => new {
                        AccountId = p.AccountId, Damage = p.Damage, Gold = p.Gold, Health = p.Health, Id = p.Id, Name = p.Name
                    }).ToArray();
                    return new Response(ResponseStatus.OK, DataType.JSON, JsonConvert.SerializeObject(players));
                } else {
                    return new Response(ResponseStatus.NOT_FOUND);
                }
            } else {
                return null;
            }
        }
        static void ListenToClientRequests(object arg) {
            Console.WriteLine($"{DateTime.Now}\tListening to client requests...");
            while (true) {
                if (clients.Count > 0) {
                    var clientsTemp = clients.ToArray();

                    foreach (var client in clientsTemp) {
                        if (client != null && !client.IsHeard) {
                            client.IsHeard = true;
                            new TaskFactory().StartNew(() => {
                                var protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 500, client.Socket);
                                var transfer = protocol.Receive();
                                Console.Write($"{DateTime.Now}\t[{serverEndPoint} <-- {client.IPAddress}]\t");
                                Console.WriteLine(protocol.GetMessage(transfer));
                                var response = HandleClientRequest(client, (Request)transfer);
                                if (response != null) {
                                    Console.Write($"{DateTime.Now}\t[{serverEndPoint} --> {client.IPAddress}]\t");
                                    Console.WriteLine(protocol.GetMessage(response));
                                    protocol.Send(response);

                                    if(!gameStarted && PlayerClients.Count() >= 2) {
                                        Console.WriteLine("starting game...");
                                        StartGame();
                                    }
                                }
                                client.IsHeard = false;
                            });
                        }
                    }
                }
            }

            //Console.WriteLine("Stopped listening to client requests...");
        }
        static void AcceptClients(object arg) {
            Console.WriteLine($"{DateTime.Now}\tAccepting clients...");
            while (true) {
                if (clients.Count < MAX_CONNECTED_CLIENTS) {
                    var socket = server.Accept();
                    var client = new Client { Socket = socket };
                    clients.Add(client);
                    Console.WriteLine($"{DateTime.Now}\tClient accepted ({client.IPAddress})");
                }
            }
        }
        static void StartGame() {
            int count = 0;
            lock (locker) {
                if (!gameStarted) {
                    foreach (var client in LoggedInClients) {
                        var protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 100, client.Socket);
                        protocol.Send(new Request(RequestStatus.START));
                    }
                    var clientsTemp = clients;
                    var players = new List<Player>();
                    foreach (var client in clientsTemp) {
                        var sp = client.SelectedPlayer;
                        var player = new Player(sp.Name, sp.Health, sp.Damage, sp.Gold++);
                        players.Add(player);
                    }
                    SetPlayersIds(players);
                    map = Map.Load(players.ToArray());
                    gameStarted = true;
                }
            }
        }

        static void SetPlayersIds(List<Player> players)
        {
            int count = 1;
            foreach (var player in players)
            {
                if(count < 6)
                player.Id = count++;
            }
        }
        static Response Login(Client client, Request request) {
            Response r = null;
            lock (locker) {
                if (request.Status != RequestStatus.LOGIN) {
                    r = new Response(ResponseStatus.BAD_REQUEST);
                } else if (client.IsLoggedIn) {
                    r = new Response(ResponseStatus.BAD_REQUEST, DataType.TEXT, "CLIENT ALREADY LOGGED IN");
                } else if (LoggedInClients.Count() >= MAX_LOGGED_IN_CLIENTS) {
                    r = new Response(ResponseStatus.LIMIT_REACHED, DataType.TEXT, "REACHED LOGGED IN USERS LIMIT");
                } else {
                    string message = request.Data;
                    string username = message.Split(' ')[0];
                    Account account = db.Accounts.SingleOrDefault(u => u.Username == username);

                    if (account == null) {
                        return new Response(ResponseStatus.NOT_FOUND, DataType.TEXT, $"USER \"{username}\" DOES NOT EXIST");
                    } else {
                        string password = message.Split(' ')[1];

                        if (password != account.Password) {
                            return new Response(ResponseStatus.UNAUTHORIZED, DataType.TEXT, $"WRONG PASSWORD FOR USER \"{username}\"");
                        } else {
                            client.Account = account;
                            return new Response(ResponseStatus.OK);
                        }
                    }
                }
            }

            return r;
        }
        static Response GetPlayerInfo(Client client, Request request) {
            if (request.Status == RequestStatus.GET_PLAYER) {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    return new Response(ResponseStatus.OK, DataType.JSON, JsonConvert.SerializeObject(client.SelectedPlayer));
                } else {
                    return new Response(ResponseStatus.UNAUTHORIZED);
                }
            } else {
                return null;
            }
        }
        static Response MovePlayer(Client client, Request request) {
            if (request.Status != RequestStatus.MOVE || !gameStarted) {
                return new Response(ResponseStatus.BAD_REQUEST);
            } else {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    map.MovePlayer(request.Data, client.SelectedPlayer.Name);
                    return new Response(ResponseStatus.OK);
                } else {
                    return new Response(ResponseStatus.UNAUTHORIZED);
                }
            }
        }
        static Response GetState(Client client, Request request) {
            if (request.Status == RequestStatus.GET_STATE && gameStarted) {
                return new Response(ResponseStatus.OK, DataType.TEXT, JsonConvert.SerializeObject(new State { MapStr = map.MapToString(), Player = client.SelectedPlayer }));
            } else {
                return new Response(ResponseStatus.NOT_READY, DataType.TEXT, "GAME NOT STARTED");
            }
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