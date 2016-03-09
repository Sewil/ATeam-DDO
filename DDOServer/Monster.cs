using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOServer
{
    internal class Monster : Character
    {
        public Monster(string name, int health, int damage, int gold) : base(name, health, damage, gold)
        {
            Color = ConsoleColor.DarkRed;
        }
    }
}
