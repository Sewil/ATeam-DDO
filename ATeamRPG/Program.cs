using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Program
    {
        static Player player1;
        static Player player2;
        //Working????
        static void Main(string[] args)
        {
            Start();
        }

        static void Start()
        {
            string name;
            Console.WriteLine("Welcome to the game");
            Console.WriteLine("Insert desired name: ");
            Console.WriteLine("Player 1 : ");

            name = Console.ReadLine();
            player1 = new Player(name);
            Console.WriteLine("Player 2 : ");
            name = Console.ReadLine();
            player2 = new Player(name);

            Console.WriteLine(player1.Name + player2.Name);
        }
        static void JoelLovesToBurn()
        {

        }
    }
}
