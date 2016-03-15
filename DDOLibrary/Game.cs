using DDOLibrary.GameObjects;

namespace DDOLibrary {
    public class Game {
        public Cell[,] Cells { get; set; }
        public Player[] Players { get; set; }
        public Player Player { get; set; }
    }
}
