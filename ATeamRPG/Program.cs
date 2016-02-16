using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Program
    {
        static bool[,] map;

        static void Main(string[] args)
        {

            Console.WriteLine("Input the name of Player 1: ");
            var nameOne = Console.ReadLine();
            var playerOne = new Player(nameOne);
            Console.WriteLine("Input the name of Player 2: ");
            var nameTwo = Console.ReadLine();
            var playerTwo = new Player(nameTwo);
            Map map = new Map();
            map.PlaceGold();
            map.Draw();
            do {
                ConsoleKey key = Console.ReadKey().Key;
            } while (true);
        }

        static void CreateMap()
        {
            map = MapFactory.GenerateMap();
        }

        static void DrawMap()
        {
            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    if (map[y, x] == true)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    }
                    else if (map[y, x] == false)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
        }

    static void JoelLovesToBurn()
    {

    }

    static void MovePlayer(ConsoleKey key, Player player)
    {
        switch (key)
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
