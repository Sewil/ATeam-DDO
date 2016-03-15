using System;
namespace DDOLibrary.GameObjects {
    public class Cell
    {
        public CellType CellType { get; }
        public bool HasGold { get { return Gold > 0; } }
        public int Gold { get; set; }
        public int Y { get; }
        public int X { get; }
        public Cell(int y, int x, CellType cellType)
        {
            Gold = 0;
            Y = y;
            X = x;
            CellType = cellType;
        }
        public Potion GetPotion(Map map) {
            lock (map.potions) {
                foreach (var potion in map.potions) {
                    if (potion.X == X && potion.Y == Y) {
                        return potion;
                    }
                }
            }

            return null;
        }
        public Character GetCharacter(Map map) {
            lock (map.players) {
                foreach (var c in map.players) {
                    if (c.X == X && c.Y == Y) {
                        return c;
                    }
                }
            }
            lock (map.monsters) {
                foreach (var c in map.monsters) {
                    if (c.X == X && c.Y == Y) {
                        return c;
                    }
                }
            }

            return null;
        }
        public Monster GetMonster(Map map) {
            return GetCharacter(map) as Monster;
        }
        public Player GetPlayer(Map map) {
            return GetCharacter(map) as Player;
        }
        public bool IsSpawnable(Map map) {
            return IsWalkable(map) && !HasGold && GetPotion(map)==null;
        }
        public bool IsWalkable(Map map) {
            return CellType == CellType.Ground && GetCharacter(map)==null;
        }
    }
}