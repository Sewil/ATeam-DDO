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
            var playerTwo = new Player(nameTwo);
            Console.CursorVisible = false;
            Map map = new Map();
            map.PlaceGold();
            map.SpawnPlayers(playerOne, playerTwo);
            Console.Clear();
            map.Draw();
            do {
                ConsoleKey key = Console.ReadKey().Key;
                map.MovePlayer(key, playerOne);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                map.Draw();
            } while (true);
        }
    }
}
