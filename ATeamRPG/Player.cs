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
        public Cell Cell { get; set; }
        public int X {
            get {
                return Cell.X;
            }
        }
        public int Y {
            get {
                return Cell.Y;
            }
        }
        public int Health { get; set; }
        public int Gold { get; set; }
        public int Damage { get; set; }

        public void OnDied(Cell c) {
            c.Gold += Gold;
            Gold = 0;
        }
    }
}
