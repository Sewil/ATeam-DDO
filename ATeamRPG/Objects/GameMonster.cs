using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    public class GameMonster : GameCharacter
    {
        public const int HEALTH = 10;
        public const int DAMAGE = 2;
        public GameMonster()
        {
            Color = ConsoleColor.DarkRed;
            Health = HEALTH;
            Damage = DAMAGE;
        }
    }
}
