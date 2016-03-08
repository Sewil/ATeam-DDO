using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOServer
{
    public class HealthPotion
    {
        public const int HEALTH = 10;
        public ConsoleColor Color { get; set; }
        public int Health { get; set; }
        public HealthPotion()
        {
            Health = HEALTH;
            Color = ConsoleColor.Blue;
        }
    }
}
