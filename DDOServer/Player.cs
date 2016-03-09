using System;
using System.Linq;
using System.Collections.Generic;

namespace DDOServer
{
    public class Player : Character
    {
        static Random random = new Random();
        public static Dictionary<char, ConsoleColor> icons = new Dictionary<char, ConsoleColor>() {
            {'☻', ConsoleColor.White },
            {'♥', ConsoleColor.Red },
            {'♦', ConsoleColor.Magenta },
            {'♣', ConsoleColor.DarkGray },
            {'♠', ConsoleColor.DarkMagenta }
        };
        public KeyValuePair<char,ConsoleColor> Icon { get; }
        public const int DAMAGE = 5;
        public int Id { get; set; }
        public Player(string name, int health, int damage, int gold, KeyValuePair<char, ConsoleColor> icon) : base(name, health, damage, gold)
        {
            Color = ConsoleColor.Red;
            Icon = icon;
        }
    }
}