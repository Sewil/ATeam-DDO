using System;

namespace DDOServer
{
    public abstract class Character
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int DefaultHealth { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public int Health { get; set; }
        public ConsoleColor Color { get; set; }
        public int Gold { get; set; }
        public Character(string name, int health, int damage, int gold) {
            DefaultHealth = health;
            Health = DefaultHealth;
            Damage = damage;
            Name = name;
            Gold = gold;
        }
    }
}