using System;
using System.Collections.Generic;
namespace ATeamRPG {

    class Map {
        long spawnTimeMonster;
        long spawnTimeHealthPotion;
        const int GOLD_ROUND_TURNS = 50;
        const int MONSTER_SPAWN_TURNS = 5;
        public const int MAXSPAWNEDMONSTERS = 10;
        public const int MAXSPAWNEDHEALTHPOTIONS = 10;
        int turn;
        public int monsterCount;
        public int SpawnedMonsters { get; set; }
        public int SpawnedHealthPotions { get; set; }
        public int Turn {
            get {
                return turn;
            }
            set {
                turn = value;
                if (turn % GOLD_ROUND_TURNS == 0) {
                    OnGoldRound(this);
                }
                if(turn%MONSTER_SPAWN_TURNS==0&&monsterCount>=10)
                {
                    OnMonsterSpawn(this);
                    monsterCount++;
                }
            }
        }
        public Player[] players;
        public const int WIDTH = 75;
        public const int HEIGHT = 20;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];
        const double FOREST_CHANCE = 0.5;
        private Map() {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    if (random.NextDouble() <= FOREST_CHANCE) {
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
        public event Action<Map> MonsterSpawn;
        public void OnGoldRound(Map m) {
            GoldRound?.Invoke(m);
        }
        public void OnMonsterSpawn(Map m)
        {
            MonsterSpawn?.Invoke(m);
        }
        public static Map Load(params Player[] players) {
            var map = new Map {
                players = players
            };
            var monsters = new Monster();
            map.MonsterSpawn += (m) =>
            {
                m.SpawnMonsters();
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
        int CountForestNeighbors(int y, int x) {
            int neighbors = 0;
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    int neighborY = i + y;
                    int neighborX = j + x;
                    bool selfCell = i == 0 && j == 0;
                    bool outsideCell = neighborY < 0 || neighborX < 0 || neighborY >= HEIGHT || neighborX >= WIDTH;

                    if (!selfCell && (outsideCell || Cells[neighborY, neighborX].CellType == CellType.Forest)) {
                        neighbors++;
                    }
                }
            }

            return neighbors;
        }
        Cell[,] DoSimulationStep() {
            var newCells = new Cell[HEIGHT, WIDTH];
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    int neighbours = CountForestNeighbors(y, x);
                    bool borderCell = y == 0 || y == HEIGHT - 1 || x == 0 || x == WIDTH - 1;

                    if (borderCell || neighbours > 4) {
                        newCells[y, x] = new Cell(y, x, CellType.Forest);
                    } else if (neighbours <= 4) {
                        newCells[y, x] = new Cell(y, x, CellType.Ground);
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
                    } else if (cell.HasHealthPotion) {
                        Console.ForegroundColor = cell.HealthPotion.Color;
                        Console.Write("P");
                    } else if (cell.HasMonster) {
                        Console.ForegroundColor = cell.Monster.Color;
                        Console.Write("M");
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
                if (emptyCells.Count > 0) {
                    var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
                    randomCell.Player = player;
                } else {
                    throw new Exception("Error spawning player. No free cells.");
                }
            }
        }

        public void SpawnHealthPotion()
        {
            // Place in infinite loop if we want to thread it in background
            if (DateTime.Now.Ticks > spawnTimeHealthPotion)
            {
                var random = new Random();

                var availableCells = new List<Cell>();
                foreach (var cell in Cells)
                {
                    if (cell.Spawnable && SpawnedHealthPotions < MAXSPAWNEDHEALTHPOTIONS)
                        availableCells.Add(cell);
                }
                var randomCell = availableCells[random.Next(0, availableCells.Count)];
                randomCell.HealthPotion = new HealthPotion();
                SpawnedHealthPotions += 1;
                spawnTimeHealthPotion = DateTime.Now.Ticks + 50000000;
            }
        }
        public void SpawnMonster()
        {
            // Place in infinite loop if we want to thread it in background
            if (DateTime.Now.Ticks > spawnTimeMonster)
            {
                var random = new Random();

                var availableCells = new List<Cell>();
                foreach (var cell in Cells)
                {
                    if (cell.Spawnable && SpawnedMonsters < MAXSPAWNEDMONSTERS)
                        availableCells.Add(cell);
                }
                var randomCell = availableCells[random.Next(0, availableCells.Count)];
                randomCell.Monster = new Monster();
                SpawnedMonsters += 1;
                spawnTimeMonster = DateTime.Now.Ticks + 50000000;
            }
        }

        public void SpawnMonsters(params Monster[] monsters)
        {
            var random = new Random();
            foreach(var monster in monsters)
            {

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
                } else if (newCell.Walkable) {
                    newCell.Player = currentCell.Player;
                    currentCell.Player = null;

                    if (newCell.HasGold) {
                        newCell.Player.Gold += newCell.Gold;
                        newCell.Gold = 0;
                    } else if (newCell.HasHealthPotion) {
                        if (newCell.Player.Health < 20)
                            newCell.Player.Health += newCell.HealthPotion.Health;
                    }
                }
            }
        }
    }
}
