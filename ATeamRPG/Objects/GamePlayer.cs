using System;

namespace ATeamRPG
{
    public class GamePlayer : GameCharacter
    {
        public int? Id { get; }
        public const int HEALTH = 20;
        public const int DAMAGE = 5;
        public bool IsActive { get; set; }
        public GamePlayer(int id, string name)
        {
            Id = id;
            Name = name;
            Color = ConsoleColor.Red;
            Health = HEALTH;
            Damage = DAMAGE;
        }
    }
}
