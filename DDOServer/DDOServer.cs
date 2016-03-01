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
    class Client {
        public Socket Socket { get; set; }
        public bool IsLoggedIn { get { return Account != null; } }
        public bool HasSelectedPlayer { get { return SelectedPlayer != null; } }
        public Account Account { get; set; }
        public DDODatabase.Player SelectedPlayer { get; set; }
    }
    public class DDOServer {
        static object locker = new object();
        public static ATeamDB db = new ATeamDB();
        const int LISTENER_BACKLOG = 100;
        static Socket masterServer = null;
        static Socket server = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint serverEndPoint = new IPEndPoint(ipAddress, 8001);
        static IPEndPoint masterServerEndPoint = new IPEndPoint(ipAddress, 8000);
        static Protocol protocol = null;
        static List<Client> clients = new List<Client>();
        static bool gameStarted = false;
        static string mapStr = null;
        static Map map = null;
        static void Main(string[] args) {
            ConnectToMasterServer();
            try {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(serverEndPoint);
                server.Listen(LISTENER_BACKLOG);
                protocol = new Protocol("DDO/1.0", new UTF8Encoding(), 100);

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
                case RequestStatus.LOGIN: return Login(client, request);
                case RequestStatus.GET_MAP: return GetMap(client, request);
                case RequestStatus.GET_PLAYER: return GetPlayerInfo(client, request);
                case RequestStatus.START: return StartGame(client, request);
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
                    DDODatabase.Player[] players = db.Players.Where(p => p.Account == client.Account).ToArray();
                    return new Response(ResponseStatus.OK, JsonConvert.SerializeObject(players), DataType.JSON);
                } else {
                    return new Response(ResponseStatus.NOT_FOUND);
                }
            } else {
                return null;
            }
        }
        static void ListenToClientRequests(object arg) {
            Console.WriteLine("Listening to client requests...");
            while (true) {
                lock (locker) {
                    foreach (var client in clients) {
                        protocol.Socket = client.Socket;
                        var transfer = protocol.Receive();
                        if (transfer != null) {
                            Console.WriteLine(protocol.GetMessage(transfer));
                            var response = HandleClientRequest(client, (Request)transfer);
                            if (response != null) {
                                Console.WriteLine(protocol.GetMessage(response));
                                protocol.Send(response);
                            }

                            break;
                        }
                    }
                }
            }

            //Console.WriteLine("Stopped listening to client requests...");
        }
        static void AcceptClients(object arg) {
            Console.WriteLine("Accepting clients...");
            while (clients.Count < 2) {
                var socket = server.Accept();
                var client = new Client {
                    Socket = socket
                };
                clients.Add(client);
                Console.WriteLine($"Client accepted ({client.Socket.AddressFamily.ToString()})");
            }
            Console.WriteLine("No longer accepting clients. Limit reached.");
        }
        static Response StartGame(Client client, Request request) {
            if (request.Status == RequestStatus.START) {
                if (clients.Count >= 2) {
                    gameStarted = true;
                    new Thread(new ParameterizedThreadStart(PlayDDO)).Start(); // initiate game on separate thread
                    return new Response(ResponseStatus.OK, "");
                } else {
                    return new Response(ResponseStatus.NOT_READY, "");
                }
            } else {
                return null;
            }
        }
        static Response Login(Client client, Request request) {
            if (request.Status == RequestStatus.LOGIN) {
                string message = request.Data;
                string username = message.Split(' ')[0];
                Account account = db.Accounts.SingleOrDefault(u => u.Username == username);
                if (account != null) {
                    string password = message.Split(' ')[1];
                    if (account.Password == password) {
                        client.Account = account;
                        return new Response(ResponseStatus.OK, $"LOGIN ACCEPTED FOR {username}");
                    } else {
                        return new Response(ResponseStatus.UNAUTHORIZED, $"WRONG PASSWORD FOR {username}");
                    }
                } else {
                    return new Response(ResponseStatus.NOT_FOUND, $"{username} DOES NOT EXIST");
                }
            } else {
                return null;
            }
        }
        static Response GetPlayerInfo(Client client, Request request) {
            if (request.Status == RequestStatus.GET_PLAYER) {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    return new Response(ResponseStatus.OK, JsonConvert.SerializeObject(client.SelectedPlayer), DataType.JSON);
                } else {
                    return new Response(ResponseStatus.UNAUTHORIZED);
                }
            } else {
                return null;
            }
        }
        static Response MovePlayer(Client client, Request request) {
            if (request.Status == RequestStatus.MOVE && gameStarted) {
                if (client.IsLoggedIn && client.HasSelectedPlayer) {
                    map.MovePlayer(request.Data, client.SelectedPlayer.Name);
                    return new Response(ResponseStatus.OK, "");
                } else {
                    return new Response(ResponseStatus.UNAUTHORIZED);
                }
            } else {
                return null;
            }
        }
        static Response GetMap(Client client, Request request) {
            if (request.Status == RequestStatus.GET_MAP && gameStarted) {
                return new Response(ResponseStatus.OK, mapStr);
            } else {
                return new Response(ResponseStatus.NOT_READY, "GAME NOT STARTED");
            }
        }
        static void ConnectToMasterServer() {
            /*
            doesn't quite work yet

            masterServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            masterServer.Connect(masterServerEndPoint);
            Protocol p = new Protocol("DDO/1.0", new UTF8Encoding(), 100, masterServer);
            Console.WriteLine("Connected to MasterServer.");

            // Låt master-servern veta att det är en server som ansluter
            p.Send(new Request(RequestStatus.NONE, "server"));

            var tokens = p.Receive().Data.Split(' ');
            serverID = int.Parse(tokens[0]);

            // Definiera localEndPoint med Porten vi fick från MasterServern
            serverEndPoint = new IPEndPoint(ipAddress, int.Parse(tokens[1]));
            */
        }
        static void PlayDDO(object arg) {
            lock (locker) {
                var players = new List<Player>();
                foreach (var client in clients) {
                    var selectedPlayer = client.SelectedPlayer;
                    var player = new Player(selectedPlayer.Name);
                    players.Add(player);
                }
                map = Map.Load(players.ToArray());
                mapStr = map.MapToString();
            }
        }
    }
}