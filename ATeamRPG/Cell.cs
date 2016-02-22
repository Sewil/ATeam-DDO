namespace ATeamRPG {
    class Cell {
        public CellType CellType { get; set; }
        Player player;
        Monster monster;
        Map map;
        public Monster Monster
        {   get { return monster; }
            set
            {
                if(value!=null)
                {
                    monster = value;
                    monster.Died += Monster_Died;                    
                }
                else
                {
                    monster.Died -= Monster_Died;
                    monster=value;                    
                }
            }
        }
        public Player Player {
            get {
                return player;
            }
            set {
                if (value != null) {
                    player = value;
                    player.Died += Player_Died;
                } else {
                    player.Died -= Player_Died;
                    player = value;
                }
            }
        }
        public void Player_Died(Player p) {
            Gold += p.Gold;
            p.Gold = 0;
            Player = null;
        }
        public void Monster_Died(Monster m)
        {
            Gold += m.Gold;
            Monster = null;
            map.monsterCount--;
        }
        public bool HasPlayer {
            get {
                return Player != null;
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
        }
    }
}
