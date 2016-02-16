using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Player {
        public Player(string name) {
            Name = name;
            Health = 20;
            Damage = 5;
         }
        public Cell PlayerPosition { get; set; }
        public string Name { get; set; }
        int health;
        public ConsoleColor Colour { get; set; }
        public int Health {
            get {
                return health;
            }
            set {
                health = value;
                if(health <= 0) {
                    OnDied(this);
                }
            }
        }
        public int Gold { get; set; }
        public int Damage { get; set; }

        public event Action<Player> Died;
        public void OnDied(Player player) {
            Died?.Invoke(player);
        }
    }
}
