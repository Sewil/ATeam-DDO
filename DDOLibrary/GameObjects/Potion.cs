using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOLibrary.GameObjects {
    public class Potion
    {
        public static int IdCounter = 0;
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public const int HEALTH = 10;
        public ConsoleColor Color { get; set; }
        public int Health { get; set; }
        public Potion(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
            Health = HEALTH;
            Color = ConsoleColor.Blue;
        }
    }
}
