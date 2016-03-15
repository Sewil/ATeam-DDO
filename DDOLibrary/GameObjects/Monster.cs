using System;

namespace DDOLibrary.GameObjects {
    public class Monster : Character
    {
        public static int IdCounter = 0;
        static Random random = new Random();
        public Monster(int id, int health, int damage, int gold) : base(id, health, damage, gold)
        {
            Color = ConsoleColor.DarkRed;
        }
    }
}
