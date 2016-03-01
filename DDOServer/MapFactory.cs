using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOServer
{
    class MapFactory
    {
        static double chanceToStartAlive = 0.5;
        public const int WIDTH = 75;
        public const int HEIGHT = 20;
        public bool[,] Map = new bool[20, 75];
        public MapFactory()
        {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (random.NextDouble() <= chanceToStartAlive)
                    {
                        Map[y, x] = true;
                    }
                }
            }
            for (int i = 0; i < random.Next(1, 4); i++)
            {
                Map = DoSimulationStep(Map);
            }
        }
        void Draw()
        {
            for (int y = 0; y < Map.GetLength(0); y++)
            {
                for (int x = 0; x < Map.GetLength(1); x++)
                {
                    if (Map[y, x] == true)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("#");
                    }
                    else if (Map[y, x] == false)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(".");
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
        }
        static bool[,] DoSimulationStep(bool[,] oldMap)
        {
            bool[,] newMap = new bool[20, 75];
            for (int x = 0; x < oldMap.GetLength(0); x++)
            {
                for (int y = 0; y < oldMap.GetLength(1); y++)
                {
                    int neighbours = CountAliveNeighbours(oldMap, x, y);
                    if (neighbours < 4)
                        newMap[x, y] = false;
                    else if (neighbours > 4)
                        newMap[x, y] = true;
                }
            }
            return newMap;
        }
        static int CountAliveNeighbours(bool[,] map, int x, int y)
        {
            int count = 0;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int neighbour_x = x + i;
                    int neighbour_y = y + j;
                    if (i == 0 && j == 0)
                    {
                        //Do nothing.
                    }
                    else if (neighbour_x < 0 || neighbour_y < 0 ||neighbour_x >= map.GetLength(0) || neighbour_y >= map.GetLength(1))
                    {
                        count = count + 1;
                    }
                    else if (map[neighbour_x, neighbour_y])
                    {
                        count = count + 1;
                    }
                }
            }
            return count;
        }
    }
}
