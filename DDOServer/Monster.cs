using System;

namespace DDOServer {
    public class Monster : Character
    {
        static Random random = new Random();
        public Monster(string name, int health, int damage, int gold) : base(name, health, damage, gold)
        {
            Color = ConsoleColor.DarkRed;
        }
    }
}
