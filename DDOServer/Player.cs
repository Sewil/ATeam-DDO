using System;

namespace DDOServer {
    class Player : Character {
        public const int HEALTH = 20;
        public const int DAMAGE = 5;
        public bool IsActive { get; set; }
        public Player(string name) {
            Name = name;
            Color = ConsoleColor.Red;
            Health = HEALTH;
            Damage = DAMAGE;
        }
    }
}