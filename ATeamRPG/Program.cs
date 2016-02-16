using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Program
    {
        //Working????
        static void Main(string[] args)
        {

            Console.WriteLine("Input the name of Player 1: ");
            var nameOne = Console.ReadLine();
            var playerOne = new Player(nameOne);
            Console.WriteLine("Input the name of Player 2: ");
            var nameTwo = Console.ReadLine();
            var playerTwo = new Player(nameTwo);
            Map map = new Map();
            ConsoleKey key = Console.ReadKey().Key;
            MovePlayer(key, playerOne);
        }
        
        static void JoelLovesToBurn()
        {

        }

        static void MovePlayer(ConsoleKey key, Player player)
        {
            switch(key)
            {
                case ConsoleKey.UpArrow:
                    if (player.YPosition > 0)
                        player.YPosition--;
                    break;

                case ConsoleKey.RightArrow:
                    if (player.XPosition < Map.WIDTH - 1)
                        player.XPosition++;
                    break;

                case ConsoleKey.DownArrow:
                    if (player.YPosition < Map.HEIGHT - 1)
                        player.YPosition++;
                    break;

                case ConsoleKey.LeftArrow:
                    if (player.XPosition > 0)
                        player.XPosition--;
                    break;
            }
        }
    }
}
