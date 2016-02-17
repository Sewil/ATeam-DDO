using System;
using System.Collections.Generic;
namespace ATeamRPG {

    class Map {
        const int GOLD_ROUND_TURNS = 50;
        int turn;
        public int Turn {
            get {
                return turn;
            }
            set {
                turn = value;
                if (turn % GOLD_ROUND_TURNS == 0) {
                    OnGoldRound(this);
                }
            }
        }
        public Player[] players;
        public const int WIDTH = 75;
        public const int HEIGHT = 20;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];
        double chanceToStartAlive = 0.5;
        private Map() {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    if (random.NextDouble() <= chanceToStartAlive) {
                        Cells[y, x] = new Cell(y, x, CellType.Forest);
                    } else {
                        Cells[y, x] = new Cell(y, x, CellType.Ground);
                    }
                }
            }
            for (int i = 0; i < random.Next(1, 4); i++) {
                Cells = DoSimulationStep();
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
        int CountAliveNeighbours(int y, int x) {
            int count = 0;
            for (int i = -1; i < 2; i++) {
                for (int j = -1; j < 2; j++) {
                    int neighborY = y + i;
                    int neighborX = x + j;

                    if (i == 0 && j == 0) {
                    } else if (
                        (
                            neighborY < 0
                            || neighborX < 0
                            || neighborY >= HEIGHT
                            || neighborX >= WIDTH
                        )
                        || Cells[neighborY, neighborX].CellType == CellType.Forest
                    ) {
                        count++;
                    }
                }
            }
            return count;
        }
        Cell[,] DoSimulationStep() {
            var newCells = new Cell[HEIGHT, WIDTH];
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    int neighbours = CountAliveNeighbours(y, x);

                    if (neighbours <= 4) {
                        newCells[y, x] = new Cell(y, x, CellType.Ground);
                    } else if (neighbours > 4) {
                        newCells[y, x] = new Cell(y, x, CellType.Forest);
                    }
                }
            }
            return newCells;
        }
        public void Draw() {
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (cell.HasGold) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("$");
                    } else if (cell.HasPlayer) {
                        if (cell.Player.IsActive) {
                            Console.ForegroundColor = cell.Player.Color;
                        } else {
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Console.Write("@");
                    } else if (cell.CellType == CellType.Forest) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    } else if (cell.CellType == CellType.Ground) {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
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
                if (cell.Goldable && random.NextDouble() <= goldChance) {
                    cell.Gold += random.Next(goldRange[0], goldRange[1] + 1);
                }
            }
        }

        public void SpawnPlayers(params Player[] players) {
            var random = new Random();
            foreach (var player in players) {
                List<Cell> emptyCells = new List<Cell>();
                foreach (var cell in Cells) {
                    if (cell.Spawnable) {
                        emptyCells.Add(cell);
                    }
                }
                var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
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
                } else if(newCell.Walkable) {
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
