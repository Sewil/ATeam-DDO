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
        public string Name { get; set; }
        public int XPosition { get; set; }
        public int YPosition { get; set; }
        public int Health { get; set; }
        public int Gold { get; set; }
        public int Damage { get; set; }

        public void OnDied(Cell c) {
            c.Gold += Gold;
            Gold = 0;
        }
    }
}
