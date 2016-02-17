using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Cell {
        Player player;
        public Player Player {
            get {
                return player;
            }
            set {
                if (value != null) {
                    player = value;
                    player.Died += Player_Died;
                } else {
                    player.Died -= Player_Died;
                    player = value;
                }
            }
        }
        public void Player_Died(Player p) {
            Gold += p.Gold;
            p.Gold = 0;
            Player = null;
        }
        public bool HasPlayer {
            get {
                return Player != null;
            }
        }
        public bool IsEmpty {
            get {
                return !HasGold && !HasPlayer;
            }
        }
        public bool HasGold {
            get {
                return Gold > 0;
            }
        }
        public int Gold { get; set; }
        public int Y { get; set; }
        public int X { get; set; }
        public Cell(int y, int x) {
            Gold = 0;
            Y = y;
            X = x;
        }
    }
    class Map {
        const int GOLD_ROUND_TURNS = 20;
        int turn;
        public int Turn {
            get {
                return turn;
            }
            set {
                turn = value;
                if(turn % GOLD_ROUND_TURNS == 0) {
                    OnGoldRound(this);
                }
            }
        }
        public Player[] players;
        public const int WIDTH = 10;
        public const int HEIGHT = 10;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];

        private Map() {
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    Cells[y, x] = new Cell(y, x);
                }
            }
        }
        public event Action<Map> GoldRound;
        public void OnGoldRound(Map m) {
            GoldRound?.Invoke(m);
        }
        public static Map Load(params Player[] players) {
            var map = new Map {
                players = players
            };
            map.GoldRound += (m) => {
                m.PlaceGold();
            };
            foreach (var player in map.players) {
                player.Died += (p) => {
                    map.SpawnPlayers(p); // respawn on death
                    p.Health = Player.HEALTH;
                };
            }
            map.PlaceGold();
            map.SpawnPlayers(players);
            return map;
        }
        public void Draw() {
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    if (cell.HasGold) {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.Write(" ");
                    } else if (cell.HasPlayer) {
                        if (cell.Player.IsActive) {
                            Console.ForegroundColor = cell.Player.Color;
                        } else {
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Console.Write("P");
                    } else {
                        Console.Write(" ");
                    }

                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine($"Turn: {Turn}");
            for (int i = 0; i < players.Length; i++) {
                var player = players[i];
                Console.WriteLine($"Player {i + 1} gold: {player.Gold}, health: {player.Health}");
            }
        }

        double goldChance = 0.1;
        int[] goldRange = { 100, 1000 };
        public void PlaceGold() {
            var random = new Random();
            foreach (var cell in Cells) {
                if (random.NextDouble() <= goldChance && !cell.HasPlayer) {
                    cell.Gold += random.Next(goldRange[0], goldRange[1] + 1);
                }
            }
        }

        public void SpawnPlayers(params Player[] players) {
            var random = new Random();
            foreach (var player in players) {
                List<Cell> emptyCells = new List<Cell>();
                foreach (var cell in Cells) {
                    if (cell.IsEmpty) {
                        emptyCells.Add(cell);
                    }
                }
                var randomCell = emptyCells[random.Next(0, emptyCells.Count())];
                randomCell.Player = player;
            }
        }
        Cell FindPlayer(Player player) {
            foreach (var cell in Cells) {
                if (cell.Player == player) {
                    return cell;
                }
            }

            return null;
        }
        public void MovePlayer(ConsoleKey key, Player player) {
            var currentCell = FindPlayer(player);
            Cell newCell = null;
            switch (key) {
                case ConsoleKey.UpArrow:
                    if (currentCell.Y > 0) {
                        newCell = Cells[currentCell.Y - 1, currentCell.X];
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (currentCell.X < WIDTH - 1) {
                        newCell = Cells[currentCell.Y, currentCell.X + 1];
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (currentCell.Y < HEIGHT - 1) {
                        newCell = Cells[currentCell.Y + 1, currentCell.X];
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (currentCell.X > 0) {
                        newCell = Cells[currentCell.Y, currentCell.X - 1];
                    }
                    break;
            }

            if (newCell != null) {
                Turn++;
                if (newCell.HasPlayer) {
                    newCell.Player.Health -= currentCell.Player.Damage;
                } else {
                    newCell.Player = currentCell.Player;
                    currentCell.Player = null;

                    if (newCell.HasGold) {
                        newCell.Player.Gold += newCell.Gold;
                        newCell.Gold = 0;
                    }
                }
            }
        }
    }
}
