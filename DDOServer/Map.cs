using System;
using System.Collections.Generic;
using System.Threading;

namespace DDOServer
{
    class Map
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
        public event Action<Map> MonsterSpawn;
        public void OnGoldRound(Map m)
        {
            GoldRound?.Invoke(m);
        }
        public void OnMonsterSpawn(Map m)
        {
            MonsterSpawn?.Invoke(m);
        }    
        public static Map Load(params Player[] players)
        {
            var map = new Map
            {
                players = players
            };
            var monsters = new Monster();
            map.MonsterSpawn += (m) =>
            {
                m.SpawnMonsters();
            };
            map.GoldRound += (m) =>
            {
                m.PlaceGold();
            };
            foreach (var player in map.players)
            {
                player.Died += (p) =>
                {
                    map.SpawnPlayers(p as Player); // respawn on death
                    (p as Player).Health = Player.HEALTH;
                };
            }
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
                    else if (cell.HasPlayer)
                    {
                        mapStr += "@";
                    }
                    else if (cell.HasHealthPotion)
                    {
                        mapStr += "P";
                    }
                    else if (cell.HasMonster)
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
                if (cell.IsGoldable && random.NextDouble() <= goldChance)
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
                foreach (var cell in Cells)
                {
                    if (cell.IsSpawnable)
                    {
                        emptyCells.Add(cell);
                    }
                }
                if (emptyCells.Count > 0)
                {
                    var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
                    randomCell.Player = player;
                }
                else
                {
                    throw new Exception("Error spawning player. No free cells.");
                }
            }
        }
        public void SpawnHealthPotion()
        {
            if (DateTime.Now.Ticks > spawnTimeHealthPotion)
            {
                var random = new Random();
                var availableCells = new List<Cell>();
                foreach (var cell in Cells)
                {
                    if (cell.IsSpawnable && SpawnedHealthPotions < MAXSPAWNEDHEALTHPOTIONS)
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
            if (DateTime.Now.Ticks > spawnTimeMonster)
            {
                var rnd = new Random();
                var emptyCells = new List<Cell>();
                foreach (var cell in Cells)
                {
                    if (cell.IsSpawnable && SpawnedMonsters < MAXSPAWNEDMONSTERS)
                        emptyCells.Add(cell);
                }
                var rndCell = emptyCells[rnd.Next(0, emptyCells.Count)];
                rndCell.Monster = new Monster();
                SpawnedMonsters += 1;
                spawnTimeMonster = DateTime.Now.Ticks + 50000000;
            }
        }
        public void SpawnMonsters(params Monster[] monsters)
        {
            var random = new Random();
            foreach (var monster in monsters)
            {

            }
        }
        Cell FindPlayer(string playerName)
        {
            foreach (var cell in Cells)
            {
                if (cell.HasPlayer)
                {
                    if (cell.Player.Name == playerName)
                    {
                        return cell;
                    }
                }
            }
            return null;
        }
        public void MovePlayer(char direction, string playerName)
        {
            var currentCell = FindPlayer(playerName);
            Cell newCell = null;
            switch (direction)
            {
                case '↑':
                    if (currentCell.Y > 0)
                    {
                        newCell = Cells[currentCell.Y - 1, currentCell.X];
                    }
                    break;
                case '→':
                    if (currentCell.X < WIDTH - 1)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X + 1];
                    }
                    break;
                case '↓':
                    if (currentCell.Y < HEIGHT - 1)
                    {
                        newCell = Cells[currentCell.Y + 1, currentCell.X];
                    }
                    break;
                case '←':
                    if (currentCell.X > 0)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X - 1];
                    }
                    break;
            }
            if (newCell != null)
            {
                Turn++;
                if (newCell.HasPlayer)
                {
                    newCell.Player.Health -= currentCell.Player.Damage;
                }
                else if (newCell.HasMonster)
                {
                    newCell.Monster.Health -= currentCell.Player.Damage;
                }
                else if (newCell.IsWalkable)
                {
                    var p = currentCell.Player;
                    currentCell.OnCharacterLeft();
                    newCell.OnCharacterArrived(p);
                }
            }
        }
    }
}
