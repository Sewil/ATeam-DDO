
using DDOLibrary.GameObjects;

namespace DDOLibrary {
    public class StateChange {
        public Action Action { get; set; }
        public Player Player { get; set; }
        public Potion Potion { get; set; }
        public Monster Monster { get; set; }
        public Cell Cell { get; set; }
        public static StateChange Add(object obj) {
            return GetObject(Action.Add, obj);
        }
        public static StateChange Remove(object obj) {
            return GetObject(Action.Remove, obj);
        }
        public static StateChange Update(object obj) {
            return GetObject(Action.Update, obj);
        }
        private static StateChange GetObject(Action action, object obj) {
            if (obj is Player) {
                return new StateChange { Action = action, Player = (Player)obj };
            } else if (obj is Potion) {
                return new StateChange { Action = action, Potion = (Potion)obj };
            } else if (obj is Monster) {
                return new StateChange { Action = action, Monster = (Monster)obj };
            } else if (obj is Cell) {
                return new StateChange { Action = action, Cell = (Cell)obj };
            } else {
                return null;
            }
        }
    }
}