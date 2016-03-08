using System;

namespace DDOServer
{
    public class Cell
    {
        public CellType CellType { get; }
        public HealthPotion HealthPotion { get; set; }
        public bool HasHealthPotion { get { return HealthPotion != null; } }
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
        public Character GetCharacter(Character[] characters) {
            foreach (var c in characters) {
                if (c.X == X && c.Y == Y) {
                    return c;
                }
            }

            return null;
        }
        public Monster GetMonster(Monster[] monsters) {
            return GetCharacter(monsters) as Monster;
        }
        public Player GetPlayer(Player[] players) {
            return GetCharacter(players) as Player;
        }
        public bool IsSpawnable(params Character[] characters) {
            return IsWalkable(characters) && !HasGold && !HasHealthPotion;
        }
        public bool IsWalkable(params Character[] characters) {
            return CellType == CellType.Ground && !HasCharacter(characters);
        }
        public bool HasCharacter(params Character[] characters) {
            return GetCharacter(characters) != null;
        }
        public bool HasPlayer(params Player[] players) {
            return HasCharacter(players);
        }
        public bool HasMonster(params Monster[] monsters) {
            return HasCharacter(monsters);
        }
    }
}