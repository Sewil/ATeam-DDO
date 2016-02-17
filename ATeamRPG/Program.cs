using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {
            var playerOne = new Player {
                Color = ConsoleColor.Red
            };
            var playerTwo = new Player {
                Color = ConsoleColor.Blue
            };

            Console.CursorVisible = false;
            Map map = Map.Load(playerOne, playerTwo);
            int turns = 0;
            do {
                map.Draw();
                ConsoleKey key = Console.ReadKey().Key;
                Console.Clear();
                if (turns % 2 == 0) {
                    map.MovePlayer(key, playerOne);
                } else {
                    map.MovePlayer(key, playerTwo);
                }
                turns++;
            } while (true);
        }
    }
}
