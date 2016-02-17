using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Player {
        public Player() {
            Health = 20;
            Damage = 5;
        }
        int health;
        public ConsoleColor Color { get; set; }
        public int Health {
            get {
                return health;
            }
            set {
                health = value;
                if (health <= 0) {
                    OnDied(this);
                }
            }
        }
        public int Gold { get; set; }
        public int Damage { get; set; }

        public event Action<Player> Died;
        public void OnDied(Player player) {
            player.Health = 20;
            Died?.Invoke(player);
        }
    }
}
