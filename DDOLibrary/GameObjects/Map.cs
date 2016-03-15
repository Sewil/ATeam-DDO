﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace DDOLibrary.GameObjects {
    public class Map {
        const int GOLD_ROUND_TURNS = 50;
        const int MAX_MONSTERS = 10;
        const int MAX_POTIONS = 10;
        const long SPAWN_TIME_MONSTER_MS = 15000;
        const long SPAWN_TIME_POTION_MS = 30000;
        const long SPAWN_TIME_GOLD = 60000;
        static Random random = new Random();

        public List<Player> players = new List<Player>();
        public List<Potion> potions = new List<Potion>();
        public List<Monster> monsters = new List<Monster>();
        public const int WIDTH = 75;
        public const int HEIGHT = 20;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];
        const double FOREST_CHANCE = 0.5;
        private Map(Player[] players) {
            this.players = players.ToList();
        }
        void StartThreads() {
            new Thread(new ParameterizedThreadStart(MonsterSpawner)).Start();
            new Thread(new ParameterizedThreadStart(MonsterMover)).Start();
            new Thread(new ParameterizedThreadStart(PotionSpawner)).Start();
            new Thread(new ParameterizedThreadStart(GoldSpawner)).Start();
        }
        public static Map Load(params Player[] players) {
            var map = new Map(players);
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    if (random.NextDouble() <= FOREST_CHANCE) {
                        map.Cells[y, x] = new Cell(y, x, CellType.Forest);
                    } else {
                        map.Cells[y, x] = new Cell(y, x, CellType.Ground);
                    }
                }
            }
            for (int i = 0; i < random.Next(1, 4); i++) {
                map.Cells = map.DoSimulationStep();
            }
            map.SpawnCharacters(players);
            map.StartThreads();
            return map;
        }
        public static Map Load(Cell[,] cells, params Player[] players) {
            var map = new Map(players);
            map.Cells = cells;
            map.StartThreads();
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
        public event Action<Map> Changed;
        public void OnChanged() {
            Changed?.Invoke(this);
        }
        public void Write() {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    if (Cells[y, x].GetPlayer(this) != null) {
                        var player = Cells[y, x].GetPlayer(this);
                        Console.ForegroundColor = player.Icon.Value;
                        Console.Write(player.Icon.Key);
                    } else if (Cells[y, x].HasGold) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("$");
                    } else if (Cells[y, x].GetPotion(this) != null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("P");
                    } else if (Cells[y, x].GetMonster(this) != null) {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("M");
                    } else if (Cells[y, x].CellType == CellType.Forest) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    } else if (Cells[y, x].CellType == CellType.Ground) {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }

                Console.WriteLine();
            }
        }

        public void GoldSpawner(object arg) {
            double goldChance = 0.1;
            int[] goldRange = { 100, 1000 };
            double lastSpawn = 0.0;

            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastSpawn > SPAWN_TIME_GOLD) {
                    lock (Cells) {
                        foreach (var cell in Cells) {
                            if (cell.IsWalkable(this) && random.NextDouble() <= goldChance) {
                                cell.Gold += random.Next(goldRange[0], goldRange[1] + 1);
                            }
                        }

                        lastSpawn = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
                        OnChanged();
                    }
                }
            }
        }
        public void SpawnCharacters(params Character[] characters) {
            foreach (var character in characters) {
                List<Cell> emptyCells = new List<Cell>();
                lock (Cells) {
                    foreach (var c in Cells) {
                        if (c.IsSpawnable(this)) {
                            emptyCells.Add(c);
                        }
                    }
                }
                if (emptyCells.Count > 0) {
                    var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
                    character.X = randomCell.X;
                    character.Y = randomCell.Y;
                } else if (character is Player) {
                    throw new Exception("Error spawning character. No empty cells.");
                }
            }
        }

        public string MapToString() {
            string value = string.Empty;

            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (cell.GetPlayer(this) != null) {
                        var player = cell.GetPlayer(this);
                        value += player.Icon.Key;
                    } else if (cell.HasGold) {
                        value += "$";
                    } else if (cell.GetPotion(this) != null) {
                        value += "P";
                    } else if (cell.GetMonster(this) != null) {
                        value += "M";
                    } else if (cell.CellType == CellType.Forest) {
                        value += "#";
                    } else if (cell.CellType == CellType.Ground) {
                        value += ".";
                    }
                }

                value += Environment.NewLine;
            }

            return value;
        }

        public void PotionSpawner(object arg) {
            double lastSpawn = 0.0;
            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastSpawn > SPAWN_TIME_POTION_MS && potions.Count < MAX_POTIONS) {
                    var availableCells = new List<Cell>();
                    lock (Cells) {
                        foreach (var cell in Cells) {
                            if (cell.IsSpawnable(this)) {
                                availableCells.Add(cell);
                            }
                        }
                    }

                    var randomCell = availableCells[random.Next(0, availableCells.Count)];
                    var potion = new Potion(Potion.IdCounter++, randomCell.X, randomCell.Y);
                    potions.Add(potion);
                    lastSpawn = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

                    OnChanged();
                }
            }
        }
        public void MonsterSpawner(object arg) {
            double lastSpawn = 0.0;
            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastSpawn > SPAWN_TIME_MONSTER_MS && monsters.Count < MAX_MONSTERS) {
                    var monster = new Monster(Monster.IdCounter++, random.Next(20, 40), random.Next(5, 12), random.Next(0, 1000));
                    SpawnCharacters(monster);
                    lock (monsters) {
                        monsters.Add(monster);
                    }
                    lastSpawn = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

                    OnChanged();
                }
            }
        }
        public void MonsterMover(object arg) {
            int[] moveTimeRange = new int[] { 1000, 5000 };
            double moveTime = random.Next(moveTimeRange[0], moveTimeRange[1] + 1);
            double lastMove = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
            Array directions = Enum.GetValues(typeof(Direction));

            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastMove > moveTime && monsters.Count > 0) {
                    Direction direction = (Direction)directions.GetValue(random.Next(directions.Length));
                    lock (monsters) {
                        var monster = monsters[random.Next(monsters.Count)];
                        MoveMonster(direction, monster);
                    }

                    moveTime = random.Next(moveTimeRange[0], moveTimeRange[1] + 1);
                    lastMove = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
                }
            }
        }
        public Cell NewCell(Direction direction, Character character) {
            var currentCell = Cells[character.Y, character.X];
            Cell newCell = null;
            switch (direction) {
                case Direction.Up:
                    if (currentCell.Y > 0) {
                        newCell = Cells[currentCell.Y - 1, currentCell.X];
                    }
                    break;
                case Direction.Right:
                    if (currentCell.X < WIDTH - 1) {
                        newCell = Cells[currentCell.Y, currentCell.X + 1];
                    }
                    break;
                case Direction.Down:
                    if (currentCell.Y < HEIGHT - 1) {
                        newCell = Cells[currentCell.Y + 1, currentCell.X];
                    }
                    break;
                case Direction.Left:
                    if (currentCell.X > 0) {
                        newCell = Cells[currentCell.Y, currentCell.X - 1];
                    }
                    break;
            }

            return newCell;
        }
        public void MoveMonster(Direction direction, Monster monster) {
            Cell currentCell = Cells[monster.Y, monster.X];
            Cell newCell = NewCell(direction, monster);

            if (newCell != null) {
                var newPlayer = newCell.GetPlayer(this);
                var newPotion = newCell.GetPotion(this);
                var newMonster = newCell.GetMonster(this);

                if (newPlayer != null) {
                    newPlayer.Health -= monster.Damage;
                    if (newPlayer.Health <= 0) {
                        monster.Gold += newPlayer.Gold;
                        newPlayer.Gold = 0;
                        SpawnCharacters(newPlayer);
                        monster.X = newCell.X;
                        monster.Y = newCell.Y;
                    }
                    OnChanged();
                } else if (newPotion != null) {
                    monster.Health += newPotion.Health;
                    potions.Remove(newPotion);
                    newPotion = null;
                    monster.X = newCell.X;
                    monster.Y = newCell.Y;
                    OnChanged();
                } else if (newCell.IsWalkable(this)) {
                    monster.Gold += newCell.Gold;
                    newCell.Gold = 0;
                    monster.X = newCell.X;
                    monster.Y = newCell.Y;
                    OnChanged();
                }
            }
        }
        public void MovePlayer(Direction direction, Player player) {
            Cell currentCell = Cells[player.Y, player.X];
            Cell newCell = NewCell(direction, player);

            if (newCell != null) {
                var newPlayer = newCell.GetPlayer(this);
                var newPotion = newCell.GetPotion(this);
                var newMonster = newCell.GetMonster(this);

                if (newPlayer != null) {
                    newPlayer.Health -= player.Damage;
                    if (newPlayer.Health <= 0) {
                        player.Gold += newPlayer.Gold;
                        newPlayer.Gold = 0;
                        SpawnCharacters(newPlayer);
                        player.X = newCell.X;
                        player.Y = newCell.Y;
                    }
                    OnChanged();
                } else if (newMonster != null) {
                    newMonster.Health -= player.Damage;
                    if (newMonster.Health <= 0) {
                        player.Gold = newMonster.Gold;
                        monsters.Remove(newMonster);
                        newMonster = null;
                        player.X = newCell.X;
                        player.Y = newCell.Y;
                    }
                    OnChanged();
                } else if (newPotion != null) {
                    player.Health += newPotion.Health;
                    potions.Remove(newPotion);
                    newPotion = null;
                    player.X = newCell.X;
                    player.Y = newCell.Y;
                    OnChanged();
                } else if (newCell.IsWalkable(this)) {
                    player.Gold += newCell.Gold;
                    newCell.Gold = 0;
                    player.X = newCell.X;
                    player.Y = newCell.Y;
                    OnChanged();
                }
            }
        }
    }
}