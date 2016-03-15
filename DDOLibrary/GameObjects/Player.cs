using System;
using System.Linq;
using System.Collections.Generic;

namespace DDOLibrary.GameObjects {
    public class Player : Character
    {
        public static int IdCounter = 0;
        static Random random = new Random();
        public static Dictionary<char, ConsoleColor> icons = new Dictionary<char, ConsoleColor>() {
            {'♠', ConsoleColor.DarkMagenta },
            {'♦', ConsoleColor.Magenta },
            {'♣', ConsoleColor.DarkGray },
            {'♥', ConsoleColor.Red },
            {'☻', ConsoleColor.White }
        };
        public KeyValuePair<char, ConsoleColor> Icon { get; }
        public const int DAMAGE = 5;
        public Player(int id, int health, int damage, int gold, KeyValuePair<char, ConsoleColor> icon) : base(id, health, damage, gold)
        {
            Color = ConsoleColor.Red;
            Icon = icon;
        }
    }
}