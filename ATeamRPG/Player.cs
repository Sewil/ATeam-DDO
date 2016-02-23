using System;

namespace ATeamRPG {
    class Player : Character {
        public const int HEALTH = 20;
        public const int DAMAGE = 5;
        public Player(string name) {
            Name = name;
            Color = ConsoleColor.Red;
            Health = HEALTH;
            Damage = DAMAGE;
        }
        public bool IsActive { get; set; }
        
    }
}
