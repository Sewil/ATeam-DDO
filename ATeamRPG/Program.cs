using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {

            Console.WriteLine("Input the name of Player 1: ");
            var nameOne = Console.ReadLine();
            var playerOne = new Player(nameOne);
            Console.WriteLine("Input the name of Player 2: ");
            var nameTwo = Console.ReadLine();
            Console.CursorVisible = false;
            var playerTwo = new Player(nameTwo);
            Map map = new Map();
            map.PlaceGold();
            map.SpawnPlayers(playerOne, playerTwo);
            map.Draw();
            do {
                ConsoleKey key = Console.ReadKey().Key;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                map.Draw();
            } while (true);
        }
    }
}
