using System;

namespace ATeamRPG
{
    class Cell
    {
        public CellType CellType { get; private set; }
        Character Character { get; set; }
        public Monster Monster { get { return Character as Monster; } set { Character = value; } }
        public Player Player { get { return Character as Player; } set { Character = value; } }
        public bool HasPlayer { get { return HasCharacter && Character is Player; } }
        public bool HasMonster { get { return HasCharacter && Character is Monster; } }
        public HealthPotion HealthPotion { get; set; }
        public bool HasCharacter { get { return Character != null; } }
        public bool HasHealthPotion { get { return HealthPotion != null; } }
        public bool IsWalkable { get { return CellType == CellType.Ground && !HasCharacter; } }
        public bool IsGoldable { get { return CellType == CellType.Ground && !HasCharacter; } }
        public bool IsSpawnable { get { return IsWalkable && !HasGold && !HasHealthPotion; } }
        public bool HasGold { get { return Gold > 0; } }
        public int Gold { get; set; }
        public int Y { get; set; }
        public int X { get; set; }
        public Cell(int y, int x, CellType cellType)
        {
            Gold = 0;
            Y = y;
            X = x;
            CellType = cellType;
            CharacterArrived += (c, ch) =>
            {
                ch.Died += c.Character_Died;
                if (c.Gold > 0) {
                    ch.Gold += Gold;
                    c.Gold = 0;
                }
                if (c.HasHealthPotion)
                {
                    if (ch.Health < 20)
                    {
                        ch.Health += c.HealthPotion.Health;
                        c.HealthPotion = null;
                    }
                }
                c.Character = ch;
            };
            CharacterLeft += (c) =>
            {
                c.Character.Died -= c.Character_Died;
                c.Character = null;
            };
        }
        private void Character_Died(Character character)
        {
            Gold += character.Gold;
            character.Gold = 0;
            OnCharacterLeft();
        }
        public event Action<Cell, Character> CharacterArrived;
        public void OnCharacterArrived(Character ch)
        {
            CharacterArrived?.Invoke(this, ch);
        }
        public event Action<Cell> CharacterLeft;
        public void OnCharacterLeft()
        {
            CharacterLeft?.Invoke(this);
        }
    }
}