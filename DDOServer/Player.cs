using System;

namespace DDOServer
{
    internal class Player : Character
    {
        public const int DAMAGE = 5;
        public int Id { get; set; }
        public Player(string name, int health, int damage, int gold) : base(name, health, damage, gold)
        {
            Color = ConsoleColor.Red;
            
        }
    }
}