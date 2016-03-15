using DDOLibrary.GameObjects;

namespace DDOLibrary {
    public class State {
        public string MapString { get; set; }
        public Player Player { get; set; }
        public State(string mapString, Player player) {
            MapString = mapString;
            Player = player;
        }
    }
}
