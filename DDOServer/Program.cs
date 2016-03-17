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
        static readonly object requestLocker = new object();
        public static ATeamEntities db = new ATeamEntities();
        static Socket server = null;
        static Socket masterServer = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint serverEndPoint;
        static IPEndPoint masterServerEndPoint;

        const int WAIT_TIME_MS = 10000;
        const int MAX_LOGGED_IN_CLIENTS = 5;
        const int MAX_CONNECTED_CLIENTS = 10;
        const int MINIMUM_PLAYERS = 1;
        static List<Client> clients = new List<Client>();

        static bool gameStarted = false;
        static Map map = null;
        static void Main(string[] args) {
            if (args.Length > 0) {
                ipAddress = IPAddress.Parse(args[0]);
            }
            masterServerEndPoint = new IPEndPoint(ipAddress, 8000);

            ConnectToMasterServer();
            Console.WriteLine($"{DateTime.Now}\t{db.Database.Connection.State}");
            try {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(serverEndPoint);
                server.Listen(100);

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
                    var client = new Client(new Protocol(new UTF8Encoding(), 5000, socket));
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
                                    client.IsHeard = false;
                                    if(transfer is Request) {
                                        HandleClientRequest(client, (Request)transfer);
                                    } else if(transfer is Response) {
                                        client.LatestResponse = (Response)transfer;
                                    }
                                });
                            }
                        }

                        if (!countingDown && !gameStarted && clients.Count(c => c.IsLoggedIn) >= MINIMUM_PLAYERS) {
                            countingDown = true;
                            Console.WriteLine($"Starting game in {WAIT_TIME_MS/1000} seconds...");
                            new TaskFactory().StartNew(() => {
                                Thread.Sleep(WAIT_TIME_MS);
                                StartGame();
                            });
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
                Respond(client, new Response(ResponseStatus.OK));
                var r = new Request(RequestStatus.SEND_CHAT_MESSAGE, DataType.JSON, request.Data);
                foreach (var c in clients.Where(c => c.IsLoggedIn && c != client)) {
                    Request(c, r);
                }
            } else {
                Respond(client, new Response(ResponseStatus.BAD_REQUEST));
            }
        }

        static void StartGame() {
            if (!gameStarted) {
                gameStarted = true;

                map = Map.Load();

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

                Player[] playerArray = players.ToArray();

                State state = new State(null, map.MapToString(), null, playerArray);

                foreach (var client in loggedInClients) {
                    state.Player = client.Player;
                    string data = JsonConvert.SerializeObject(state);
                    Request(client, new Request(RequestStatus.START, DataType.JSON, data));
                }

                map.Changed += (map, changes) => {
                    UpdateState(changes);
                };

                map.Start(playerArray);
            }
        }

        static void UpdateState(params StateChange[] changes) {
            var state = new State(null, null, changes, null);
            foreach (var client in clients) {
                state.Player = client.Player;
                var data = JsonConvert.SerializeObject(state);
                Request(client, new Request(RequestStatus.UPDATE_STATE, DataType.JSON, data));
            }
        }
        static void Login(Client client, Request request) {
            lock (client) {
                if (request.Status != RequestStatus.LOGIN) {
                    Respond(client, new Response(ResponseStatus.BAD_REQUEST));
                } else if (client.IsLoggedIn) {
                    Respond(client, new Response(ResponseStatus.BAD_REQUEST, DataType.TEXT, "You are already logged in."));
                } else if (clients.Count(c => c.IsLoggedIn) >= MAX_LOGGED_IN_CLIENTS) {
                    Respond(client, new Response(ResponseStatus.LIMIT_REACHED, DataType.TEXT, $"Only {MAX_LOGGED_IN_CLIENTS} users can be logged in at a time."));
                } else {
                    string message = request.Data;
                    string username = message.Split(' ')[0];
                    Account account = db.Account.SingleOrDefault(u => u.Username == username);

                    if (account == null) {
                        Respond(client, new Response(ResponseStatus.NOT_FOUND, DataType.TEXT, "This user does not exist."));
                    } else if (clients.Any(c => c.IsLoggedIn && c.Account == account)) {
                        Respond(client, new Response(ResponseStatus.UNAUTHORIZED, DataType.TEXT, "This user is already logged in."));
                    } else {
                        string password = message.Split(' ')[1];

                        if (password != account.Password) {
                            Respond(client, new Response(ResponseStatus.UNAUTHORIZED, DataType.TEXT, "Incorrect password."));
                        } else {
                            client.Account = account;
                            Respond(client, new Response(ResponseStatus.OK));
                        }
                    }
                }
            }
        }
        static void MovePlayer(Client client, Request request) {
            if (request.Status != RequestStatus.MOVE || !gameStarted || request.DataType != DataType.JSON) {
                Respond(client, new Response(ResponseStatus.BAD_REQUEST));
            } else {
                if (client.IsLoggedIn) {
                    var direction = JsonConvert.DeserializeObject<Direction>(request.Data);
                    Respond(client, new Response(ResponseStatus.OK));
                    map.MovePlayer(direction, client.Player);
                } else {
                    Respond(client, new Response(ResponseStatus.UNAUTHORIZED));
                }
            }
        }
        static Response Request(Client client, Request request) {
            lock (requestLocker) {
                Console.WriteLine($"{DateTime.Now}\t[{serverEndPoint} --> {client.IPAddress}]\t{client.Protocol.GetMessage(request)}");
                client.LatestResponse = null;
                client.Protocol.Send(request);
                while (client.LatestResponse == null) { }
                Response response = client.LatestResponse;
                return response;
            }
        }
        static void Respond(Client client, Response response) {
            client.Protocol.Send(response);
            Console.WriteLine($"{DateTime.Now}\t[{serverEndPoint} --> {client.IPAddress}]\t{client.Protocol.GetMessage(response)}");
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
            Protocol p = new Protocol(new UTF8Encoding(), 100, masterServer);
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