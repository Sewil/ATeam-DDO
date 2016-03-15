using System;

namespace DDOLibrary.GameObjects {
    public abstract class Character
    {
        static Random random = new Random();
        static string[] names = { "Olle", "Kalle", "Molle", "Pelle", "Jalle", "Spökfan" };
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int DefaultHealth { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public int Health { get; set; }
        public ConsoleColor Color { get; set; }
        public int Gold { get; set; }
        public Character(int id, int health, int damage, int gold) {
            Id = id;
            Name = names[random.Next(names.Length)];
            DefaultHealth = health;
            Health = DefaultHealth;
            Damage = damage;
            Gold = gold;
        }
    }
}