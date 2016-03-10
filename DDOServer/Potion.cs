using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOServer
{
    public class Potion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public const int HEALTH = 10;
        public ConsoleColor Color { get; set; }
        public int Health { get; set; }
        public Potion(int x, int y)
        {
            X = x;
            Y = y;
            Health = HEALTH;
            Color = ConsoleColor.Blue;
        }
    }
}
