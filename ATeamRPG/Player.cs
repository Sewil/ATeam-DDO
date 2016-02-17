using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Player {
        public const int HEALTH = 20;
        public const int DAMAGE = 5;
        public Player() {
            Color = ConsoleColor.Green;
            Health = HEALTH;
            Damage = DAMAGE;
        }
        public bool IsActive { get; set; }
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
            Died?.Invoke(player);
        }
    }
}
