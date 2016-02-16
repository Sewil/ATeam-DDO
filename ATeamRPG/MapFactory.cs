using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class MapFactory
    {
        static int chanceToStartAlive = 50;
        static Random rand = new Random();

        static bool[,] InitializeMap(bool[,] map)
        {
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 75; x++)
                {
                    if (rand.Next(1, 101) < chanceToStartAlive)
                    {
                        map[y, x] = true;
                    }
                }
            }
            return map;
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
                    else if (neighbour_x < 0 || neighbour_y < 0 ||
                        neighbour_x >= map.GetLength(0) || neighbour_y >= map.GetLength(1))
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

        public static bool[,] GenerateMap()
        {
            bool[,] cellmap = new bool[20, 75];
            cellmap = InitializeMap(cellmap);
            for (int i = 0; i < rand.Next(1, 4); i++)
                cellmap = DoSimulationStep(cellmap);

            return cellmap;
        }
    }
}
