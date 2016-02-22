using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Monster
    {
        public const int HEALTH = 10;
        public const int DAMAGE = 2;
        public Monster()
        {
            Color = ConsoleColor.DarkRed;
            Health = HEALTH;
            Damage = DAMAGE;
        }
        int health;
        public ConsoleColor Color { get; set; }
        public int Health
        {
            get
            {
                return health;
            }
            set
            {
                health = value;
                if (health <= 0)
                {
                    OnDied(this);
                }
            }
        }
        public int Gold { get; set; }
        public int Damage { get; set; }
        public event Action<Monster> Died;
        public void OnDied(Monster monster)
        {
            Died?.Invoke(monster);
        }
    }
}
