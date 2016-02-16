using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {

            ConsoleColor[] colors = (ConsoleColor[])ConsoleColor.GetValues(typeof(ConsoleColor));
            Console.WriteLine("Input the name of Player 1: ");
            var nameOne = Console.ReadLine();
            var playerOne = new Player(nameOne);
            Console.WriteLine("Choose a colour among these: black blue red green yellow");
            string input = Console.ReadLine();
            int col = 0;
            // Ja det är fult. Jag är trött. Stäm mej.
            if (input == "black") { col = 0; }
            else if (input == "blue") { col = 9; }
            else if (input == "red") { col = 12; }
            else if (input == "green") { col = 10; }
            else if (input == "yellow") { col = 14; }
            playerOne.Colour = (ConsoleColor)col;
            Console.WriteLine("Input the name of Player 2: ");
            var nameTwo = Console.ReadLine();
            Console.CursorVisible = false;
            var playerTwo = new Player(nameTwo);
            playerTwo.Colour = (ConsoleColor)9;
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
