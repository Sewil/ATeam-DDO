using DDOLibrary.GameObjects;

namespace DDOLibrary {
    public class State {
        public string MapString { get; set; }
        public Player[] Players { get; set; }
        public StateChange[] Changes { get; set; }
        public Player Player { get; set; }
        public State(Player player, string mapString, StateChange[] changes, Player[] players) {
            Player = player;
            MapString = mapString;
            Changes = changes;
            Players = players;
        }
    }
}
