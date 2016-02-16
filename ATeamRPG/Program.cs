using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Program {
        static bool[,] map;

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
            } while (true);
        }

        static void CreateMap() {
            map = MapFactory.GenerateMap();
        }

        static void DrawMap() {
            for (int y = 0; y < map.GetLength(0); y++) {
                for (int x = 0; x < map.GetLength(1); x++) {
                    if (map[y, x] == true) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    } else if (map[y, x] == false) {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
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
