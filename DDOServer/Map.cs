using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace DDOServer
{
    public enum MoveDirection {
        UP, RIGHT, DOWN, LEFT
    }
    internal class Map
    {
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
        private Map(Player[] players)
        {
            this.players = players.ToList();
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

            new Thread(new ParameterizedThreadStart(MonsterSpawner)).Start();
            new Thread(new ParameterizedThreadStart(PotionSpawner)).Start();
            new Thread(new ParameterizedThreadStart(GoldSpawner)).Start();
        }
        public static Map Load(params Player[] players)
        {
            var map = new Map(players);
            map.SpawnCharacters(players);
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
                    var character = cell.GetCharacter(this);
                    var potion = cell.GetPotion(this);

                    if (cell.HasGold)
                    {
                        mapStr += "$";
                    }
                    else if (character != null && character is Player)
                    {
                        var player = cell.GetPlayer(this);
                        mapStr += player.Icon.Key;
                    }
                    else if (potion != null)
                    {
                        mapStr += "P";
                    }
                    else if (character != null && character is Monster)
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
                    }
                }
            }
        }
        public void SpawnCharacters(params Character[] characters)
        {
            foreach (var character in characters)
            {
                List<Cell> emptyCells = new List<Cell>();
                lock (Cells) {
                    foreach (var c in Cells) {
                        if (c.IsSpawnable(this)) {
                            emptyCells.Add(c);
                        }
                    }
                }
                if (emptyCells.Count > 0)
                {
                    var randomCell = emptyCells[random.Next(0, emptyCells.Count)];
                    character.X = randomCell.X;
                    character.Y = randomCell.Y;
                }
                else if(character is Player)
                {
                    throw new Exception("Error spawning character. No empty cells.");
                }
            }
        }
        public void PotionSpawner(object arg)
        {
            double lastSpawn = 0.0;
            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastSpawn > SPAWN_TIME_POTION_MS && potions.Count < MAX_POTIONS)
                {
                    var availableCells = new List<Cell>();
                    lock (Cells) {
                        foreach (var cell in Cells) {
                            if (cell.IsSpawnable(this)) {
                                availableCells.Add(cell);
                            }
                        }
                    }

                    var randomCell = availableCells[random.Next(0, availableCells.Count)];
                    var potion = new Potion(randomCell.X, randomCell.Y);
                    potions.Add(potion);
                    lastSpawn = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
                }
            }
        }
        public void MonsterSpawner(object arg)
        {
            double lastSpawn = 0.0;
            while (true) {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds - lastSpawn > SPAWN_TIME_MONSTER_MS && monsters.Count < MAX_MONSTERS) {
                    var monster = new Monster("Bu", random.Next(20, 40), random.Next(5, 12), random.Next(0, 1000));
                    SpawnCharacters(monster);
                    lock (monsters) {
                        monsters.Add(monster);
                    }
                    lastSpawn = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
                }
            }
        }
        public void MovePlayer(MoveDirection direction, DDODatabase.Player dbPlayer)
        {
            Player player = null;
            foreach (var p in players) {
                if(p.Name == dbPlayer.Name) {
                    player = p;
                    break;
                }
            }   
            var currentCell = Cells[player.Y, player.X];
            Cell newCell = null;
            switch (direction)
            {
                case MoveDirection.UP:
                    if (currentCell.Y > 0)
                    {
                        newCell = Cells[currentCell.Y - 1, currentCell.X];
                    }
                    break;
                case MoveDirection.RIGHT:
                    if (currentCell.X < WIDTH - 1)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X + 1];
                    }
                    break;
                case MoveDirection.DOWN:
                    if (currentCell.Y < HEIGHT - 1)
                    {
                        newCell = Cells[currentCell.Y + 1, currentCell.X];
                    }
                    break;
                case MoveDirection.LEFT:
                    if (currentCell.X > 0)
                    {
                        newCell = Cells[currentCell.Y, currentCell.X - 1];
                    }
                    break;
            }
            if (newCell != null)
            {
                var newPlayer = newCell.GetPlayer(this);
                var newPotion = newCell.GetPotion(this);
                var newMonster = newCell.GetMonster(this);

                if (newPlayer != null)
                {
                    newPlayer.Health -= player.Damage;
                    if(newPlayer.Health <= 0) {
                        player.Gold += newPlayer.Gold;
                        newPlayer.Gold = 0;
                        SpawnCharacters(newPlayer);
                        player.X = newCell.X;
                        player.Y = newCell.Y;
                    }
                }
                else if (newMonster != null)
                {
                    newMonster.Health -= player.Damage;
                    if(newMonster.Health <= 0) {
                        player.Gold = newMonster.Gold;
                        monsters.Remove(newMonster);
                        newMonster = null;
                        player.X = newCell.X;
                        player.Y = newCell.Y;
                    }
                }
                else if (newPotion != null)
                {
                    player.Health += newPotion.Health;
                    potions.Remove(newPotion);
                    newPotion = null;
                    player.X = newCell.X;
                    player.Y = newCell.Y;
                }
                else if (newCell.IsWalkable(this))
                {
                    player.Gold += newCell.Gold;
                    newCell.Gold = 0;
                    player.X = newCell.X;
                    player.Y = newCell.Y;
                }
            }

            dbPlayer.Health = player.Health;
            dbPlayer.Gold = player.Gold;
            dbPlayer.Damage = player.Damage;
        }
    }
}
