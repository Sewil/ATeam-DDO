﻿using System;
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
        public static List<string> Log = new List<string>();
        static readonly object requestLocker = new object();
        public static ATeamEntities db = new ATeamEntities();
        static Socket server = null;
        static Socket masterServer = null;
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint serverEndPoint;
        static IPEndPoint masterServerEndPoint;

        const int WAIT_TIME_MS = 2000;
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
            LogEntry($"{db.Database.Connection.State}");

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(serverEndPoint);
            server.Listen(100);

            new Thread(new ParameterizedThreadStart(AcceptClients)).Start();
            bool countingDown = false;
            while (true) {
                lock (clients) {
                    if (!countingDown && !gameStarted && clients.Count(c => c.IsLoggedIn) >= MINIMUM_PLAYERS) {
                        countingDown = true;
                        LogEntry($"Starting game in {WAIT_TIME_MS / 1000} seconds...");
                        new TaskFactory().StartNew(() => {
                            Thread.Sleep(WAIT_TIME_MS);
                            StartGame();
                        });
                    }
                }
            }
        }
        static void LogEntry(string message) {
            string logEntry = $"{DateTime.Now}\t{message}";
            Log.Add(logEntry);
            Console.WriteLine(logEntry);
        }
        static void AcceptClients(object arg) {
            LogEntry($"Accepting clients...");
            while (true) {
                if (clients.Count < MAX_CONNECTED_CLIENTS) {
                    var socket = server.Accept();
                    Client client = new Client(new Protocol(new UTF8Encoding(), 5000, socket));
                    LogEntry($"{client.IPAddress} connected");

                    lock (clients) {
                        clients.Add(client);
                    }
                    new TaskFactory().StartNew(() => {
                        while (true) {
                            if (client.Protocol.Socket.Connected) {
                                if (!client.IsHeard) {
                                    client.IsHeard = true;
                                    new TaskFactory().StartNew(() => {
                                        Message message = Receive(client);
                                        client.IsHeard = false;
                                        new TaskFactory().StartNew(() => {
                                            if (message is Request) {
                                                HandleClientRequest(client, (Request)message);
                                            } else if (message is Response) {
                                                client.LatestResponse = (Response)message;
                                            }
                                        });
                                    });
                                }
                            } else {
                                clients.Remove(client);
                                LogEntry($"{client.IPAddress} disconnected");
                                break;
                            }
                        }
                    });
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
                Request req = new Request(RequestStatus.SEND_CHAT_MESSAGE, DataType.JSON, request.Data);
                foreach (Client c in clients.Where(c => c.IsLoggedIn && c != client)) {
                    Request(c, req);
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

            foreach (var client in clients.Where(c => c.IsLoggedIn)) {
                state.Player = client.Player;
                string data = JsonConvert.SerializeObject(state);
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
                LogEntry($"[{serverEndPoint} --> {client.IPAddress}] {client.Protocol.GetMessage(request)}");
                client.LatestResponse = null;
                client.Protocol.Send(request);
                while (client.LatestResponse == null) { }
                Response response = client.LatestResponse;
                return response;
            }
        }
        static void Respond(Client client, Response response) {
            client.Protocol.Send(response);
            LogEntry($"[{serverEndPoint} --> {client.IPAddress}] {client.Protocol.GetMessage(response)}");
        }
        static Message Receive(Client client, int msgSizeOverride = 0) {
            if (msgSizeOverride == 0) {
                msgSizeOverride = client.Protocol.MsgSize;
            }

            Message message = client.Protocol.Receive();
            LogEntry($"[{serverEndPoint} <-- {client.IPAddress}] {client.Protocol.GetMessage(message)}");

            return message;
        }
        static void ConnectToMasterServer() {
            masterServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            masterServer.Connect(masterServerEndPoint);
            LogEntry("Connected to MasterServer");
            Protocol protocol = new Protocol(new UTF8Encoding(), 100, masterServer);

            protocol.Send(new Request(RequestStatus.NONE, DataType.TEXT, "im a server i promise"));

            var response = (Response)protocol.Receive();
            if (response.Status == ResponseStatus.OK) {
                serverEndPoint = new IPEndPoint(ipAddress, int.Parse(response.Data));
            }
        }
    }
}