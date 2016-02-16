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
            Console.WriteLine("A Team");
            ConsoleKey key = Console.ReadKey().Key;
            MovePlayer(key);
        }

        static void Poo()
        {

        }
        static void JoelLovesToBurn()
        {

        }

        static void MovePlayer(ConsoleKey key)
        {
            switch(key)
            {
                case ConsoleKey.UpArrow:
                    if (player.YPosition > 0)
                        player.YPosition--;
                    break;

                case ConsoleKey.RightArrow:
                    if (player.XPosition < )
                        player.XPosition;
                    break;

                case ConsoleKey.DownArrow:
                    if (player.YPosition < )
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
