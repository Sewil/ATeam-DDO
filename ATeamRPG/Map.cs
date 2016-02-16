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
        public const int WIDTH = 10;
        public const int HEIGHT = 10;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];

        public Map() {
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    Cells[y, x] = new Cell(y, x);
                }
            }
        }
        public void Draw() {
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (cell.HasGold) {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                    } else {
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                    }
                    Console.Write(" ");
                }
                Console.WriteLine();
            }
        }
        public void PlaceGold() {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (random.NextDouble() <= 0.1) {
                        cell.Gold += random.Next(100, 1001);
                    }
                }
            }
        }

        public void SpawnPlayers(params Player[] players) {
            var random = new Random();
            foreach (var player in players) {
                var emptyCells = Cells.Cast<Cell>().Where(c => !c.IsEmpty).ToList();
                var randomCell = emptyCells[random.Next(0, emptyCells.Count())];
                randomCell.Player = player;
            }
        }
        void MovePlayer(ConsoleKey key, Cell cell) {
            Cell newCell = null;
            switch (key) {
                case ConsoleKey.UpArrow:
                    if (cell.Y > 0) {
                        newCell = Cells[cell.X, cell.Y - 1];
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (cell.X < WIDTH - 1) {
                        newCell = Cells[cell.X + 1, cell.Y];
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (cell.Y < HEIGHT - 1) {
                        newCell = Cells[cell.X, cell.Y + 1];
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cell.X > 0) {
                        newCell = Cells[cell.X - 1, cell.Y];
                    }
                    break;
            }
            if (newCell.HasPlayer) {
                newCell.Player.Health -= cell.Player.Damage;
            } else {
                newCell.Player = cell.Player;
                cell.Player = null;
            }
        }
    }
}
