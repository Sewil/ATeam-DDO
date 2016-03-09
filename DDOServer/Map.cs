using System;
using System.Collections.Generic;

namespace DDOServer
{
    internal class Map
    {
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
        public int Turn
        {
            get
            {
                return turn;
            }
            set
            {
                turn = value;
                if (turn % GOLD_ROUND_TURNS == 0)
                {
                    OnGoldRound(this);
                }
                if (turn % MONSTER_SPAWN_TURNS == 0 && monsterCount >= 10)
                {
                    OnMonsterSpawn(new Monster("Scary",10,10,0));
                    monsterCount++;
                }
            }
        }
        public Player[] players;

        public List<Monster> monsters = new List<Monster>();
        public const int WIDTH = 75;
        public const int HEIGHT = 20;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];
        const double FOREST_CHANCE = 0.5;
        private Map()
        {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (random.NextDouble() <= FOREST_CHANCE)
                    {
                        Cells[y, x] = new Cell(y, x, CellType.Forest);
                    }
                    else
                    {
                        Cells[y, x] = new Cell(y, x, CellType.Ground);
                    }
                }
            }
            for (int i = 0; i < random.Next(1, 4); i++)
            {
                Cells = DoSimulationStep();
            }
        }
        public event Action<Map> GoldRound;
        public event Action<Map,Monster> MonsterSpawn;
        public void OnGoldRound(Map m)
        {
            GoldRound?.Invoke(m);
        }
        public void OnMonsterSpawn(Monster m)
        {
            MonsterSpawn?.Invoke(this, m);
        }    
        public static Map Load(params Player[] players)
        {
            var map = new Map
            {
                players = players
            };
            map.MonsterSpawn += (m,ms) =>
            {
                m.SpawnMonster(ms);
            };
            map.GoldRound += (m) =>
            {
                m.PlaceGold();
            };
            map.PlaceGold();
            map.SpawnPlayers(players);

            return map;
        }
        int CountForestNeighbors(int y, int x)
        {
            int neighbors = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int neighborY = i + y;
                    int neighborX = j + x;
                    bool selfCell = i == 0 && j == 0;
                    bool outsideCell = neighborY < 0 || neighborX < 0 || neighborY >= HEIGHT || neighborX >= WIDTH;
                    if (!selfCell && (outsideCell || Cells[neighborY, neighborX].CellType == CellType.Forest))
                    {
                        neighbors++;
                    }
                }
            }
            return neighbors;
        }
        Cell[,] DoSimulationStep()
        {
            var newCells = new Cell[HEIGHT, WIDTH];
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    int neighbours = CountForestNeighbors(y, x);
                    bool borderCell = y == 0 || y == HEIGHT - 1 || x == 0 || x == WIDTH - 1;
                    if (borderCell || neighbours > 4)
                    {
                        newCells[y, x] = new Cell(y, x, CellType.Forest);
                    }
                    else if (neighbours <= 4)
                    {
                        newCells[y, x] = new Cell(y, x, CellType.Ground);
                    }
                }
            }
            return newCells;
        }
        public string MapToString()
        {
            string mapStr = null;
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    var cell = Cells[y, x];
                    if (cell.HasGold)
                    {
                        mapStr += "$";
                    }
                    else if (cell.HasPlayer(players))
                    {
                        var player = cell.GetPlayer(players);
                        mapStr += player.Icon.Key;
                    }
                    else if (cell.HasHealthPotion)
                    {
                        mapStr += "P";
                    }
                    else if (cell.HasMonster(monsters.ToArray()))
                    {
                        mapStr += "M";
                    }
                    else if (cell.CellType == CellType.Forest)
                    {
                        mapStr += "#";
                    }
                    else if (cell.CellType == CellType.Ground)
                    {
                        mapStr += ".";
                    }
                }
            }
            return mapStr;
        }
        double goldChance = 0.1;
        int[] goldRange = { 100, 1000 };
        public void PlaceGold()
        {
            var random = new Random();
            foreach (var cell in Cells)
            {
                if (cell.IsWalkable(players) && cell.IsWalkable(monsters.ToArray()) && random.NextDouble() <= goldChance)
                {
                    cell.Gold += random.Next(goldRange[0], goldRange[1] + 1);
                }
            }
        }
        public void SpawnPlayers(params Player[] players)
        {
            var random = new Random();
            foreach (var player in players)
            {
                List<Cell> emptyCells = new List<Cell>();
                foreach (var c in Cells)
                {
                    if (c.IsSpawnable(this.players) && c.IsSpawnable(monsters.ToArray()))
                    {
                        emptyCells.Add(c);
                    }
                }
                if (emptyCells.Count > 0)
                {
                    var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
                    player.X = randomCell.X;
                    player.Y = randomCell.Y;
                }
                else
                {
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
                foreach (var c in Cells)
                {
                    if (c.IsSpawnable(players) && c.IsSpawnable(monsters.ToArray()) && SpawnedHealthPotions < MAXSPAWNEDHEALTHPOTIONS)
                        availableCells.Add(c);
                }
                var randomCell = availableCells[random.Next(0, availableCells.Count)];
                randomCell.HealthPotion = new HealthPotion();
                SpawnedHealthPotions += 1;
                spawnTimeHealthPotion = DateTime.Now.Ticks + 50000000;
            }
        }
        public void SpawnMonster(Monster m)
        {
            // Place in infinite loop if we want to thread it in background
            if (DateTime.Now.Ticks > spawnTimeMonster)
            {
                var rnd = new Random();
                var emptyCells = new List<Cell>();
                foreach (var c in Cells)
                {
                    if (c.IsSpawnable(players) && c.IsSpawnable(monsters.ToArray()) && monsters.Count < MAXSPAWNEDMONSTERS)
                        emptyCells.Add(c);
                }
                var rndCell = emptyCells[rnd.Next(0, emptyCells.Count)];
                monsters.Add(m);
                SpawnedMonsters += 1;
                spawnTimeMonster = DateTime.Now.Ticks + 50000000;
            }
        }
        public void MovePlayer(string direction, string playerName)
        {
            Player player = null;
            foreach (var p in players) {
                if(p.Name == playerName) {
                    player = p;
                    break;
                }
            }   
            var currentCell = Cells[player.Y, player.X];
            Cell newCell = null;
            switch (direction)
            {
                case "↑":
                    if (currentCell.Y > 0)
                    {
                        newCell = Cells[currentCell.Y - 1, currentCell.X];
                    }
                    break;
                case "→":
                    if (currentCell.X < WIDTH - 1)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X + 1];
                    }
                    break;
                case "↓":
                    if (currentCell.Y < HEIGHT - 1)
                    {
                        newCell = Cells[currentCell.Y + 1, currentCell.X];
                    }
                    break;
                case "←":
                    if (currentCell.X > 0)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X - 1];
                    }
                    break;
            }
            if (newCell != null)
            {
                Turn++;
                if (newCell.HasPlayer(players))
                {
                    var p = newCell.GetPlayer(players);
                    p.Health -= player.Damage;
                    if(p.Health <= 0) {
                        newCell.Gold += p.Gold;
                        SpawnPlayers(p);
                    }
                }
                else if (newCell.HasMonster(monsters.ToArray()))
                {
                    newCell.GetMonster(monsters.ToArray()).Health -= currentCell.GetPlayer(players).Damage;
                }
                else if (newCell.IsWalkable(players) && newCell.IsWalkable(monsters.ToArray()))
                {
                    player.X = newCell.X;
                    player.Y = newCell.Y;
                    player.Gold += newCell.Gold;
                    newCell.Gold = 0;
                }
            }
        }
    }
}
