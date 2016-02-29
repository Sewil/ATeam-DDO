using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    public class GamePotion
    {
        public const int HEALTH = 10;
        public ConsoleColor Color { get; set; }
        public int Health { get; set; }
        public GamePotion()
        {
            Health = HEALTH;
            Color = ConsoleColor.Blue;
        }
    }
}
