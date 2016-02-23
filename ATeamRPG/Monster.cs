using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Monster : Character
    {
        public const int HEALTH = 10;
        public const int DAMAGE = 2;
        public Monster()
        {
            Color = ConsoleColor.DarkRed;
            Health = HEALTH;
            Damage = DAMAGE;
        }
    }
}
