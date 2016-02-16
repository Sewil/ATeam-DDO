using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG
{
    class Cell
    {
        public int Y { get; set; }
        public int X { get; set; }
        public Cell(int y, int x)
        {
            Y = y;
            X = x;
        }
    }
    class Map
    {
        const int WIDTH = 10;
        const int HEIGHT = 10;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];

        public Map()
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    Cells[y, x] = new Cell(y, x);
                }
            }
        }

        public void SpawnPlayers(Player playerOne, Player playerTwo)
        {
            playerOne.XPosition = 0;
            playerOne.YPosition = 0;

            playerTwo.XPosition = 10;
            playerTwo.YPosition = 10;
        }
    }
}
