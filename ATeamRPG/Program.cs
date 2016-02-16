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
            Map map = new Map();
            map.PlaceGold();
            map.Draw();
            map.SpawnPlayers(playerOne, playerTwo);
            do {
                ConsoleKey key = Console.ReadKey().Key;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                map.Draw();
            } while (true);
        }
        static void MovePlayer(ConsoleKey key, Map map, Player player) {
            switch (key) {
                case ConsoleKey.UpArrow:
                    if (player.Y > 0)
                        player.Cell = map.Cells[player.X, player.Y - 1];
                    break;
                case ConsoleKey.RightArrow:
                    if (player.X < Map.WIDTH - 1)
                        player.Cell = map.Cells[player.X + 1, player.Y];
                    break;

                case ConsoleKey.DownArrow:
                    if (player.Y < Map.HEIGHT - 1)
                        player.Cell = map.Cells[player.X, player.Y + 1];
                    break;

                case ConsoleKey.LeftArrow:
                    if (player.X > 0)
                        player.Cell = map.Cells[player.X - 1, player.Y];
                    break;
            }
        }
    }
}
