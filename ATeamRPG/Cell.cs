using System;

namespace ATeamRPG {
    class Cell {
        public CellType CellType { get; set; }
        public Potion Potion { get; set; }
        public Character Character { get; set; }

        public bool HasPlayer {
            get {
                return Character != null;
            }
        }
        public bool Walkable {
            get {
                return CellType == CellType.Ground && !HasPlayer;
            }
        }
        public bool Goldable {
            get {
                return !HasPlayer && CellType == CellType.Ground;
            }
        }
        public bool Spawnable {
            get {
                return !HasGold && !HasPlayer && CellType == CellType.Ground;
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

        public Cell(int y, int x, CellType cellType) {
            Gold = 0;
            Y = y;
            X = x;
            CellType = cellType;

            CharacterArrived += (c, ch) => {
                c.Character = ch;
                c.Character.Died += c.Character_Died;
                if (c.Gold > 0) {
                    ch.Gold += Gold;
                    c.Gold = 0;
                }
            };

            CharacterLeft += (c) => {
                c.Character.Died -= c.Character_Died;
                c.Character = null;
            };
        }

        private void Character_Died(Character character) {
            Gold += character.Gold;
            character.Gold = 0;
            OnCharacterLeft();
        }

        public event Action<Cell, Character> CharacterArrived;
        public void OnCharacterArrived(Character ch) {
            CharacterArrived?.Invoke(this, ch);
        }

        public event Action<Cell> CharacterLeft;
        public void OnCharacterLeft() {
            CharacterLeft?.Invoke(this);
        }
    }
}
