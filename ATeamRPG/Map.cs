using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    class Cell {
        public bool HasGold {
            get {
                return Gold > 0;
            }
        }
        public int Gold { get; set; }
        public int Y { get; set; }
        public int X { get; set; }
        public Cell(int y, int x) {
            Gold = 0;
            Y = y;
            X = x;
        }
    }
    class Map {
        public const int WIDTH = 10;
        public const int HEIGHT = 10;
        public Cell[,] Cells = new Cell[HEIGHT, WIDTH];

        public Map() {
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    Cells[y, x] = new Cell(y, x);
                }
            }
        }
        public void Draw() {
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (cell.HasGold) {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                    } else {
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                    }
                    Console.Write(" ");
                }
                Console.WriteLine();
            }
        }
        public void PlaceGold() {
            var random = new Random();
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    var cell = Cells[y, x];
                    if (random.NextDouble() <= 0.1) {
                        cell.Gold += random.Next(100, 1001);
                    }
                }
            }
        }

        public void SpawnPlayers(Player playerOne, Player playerTwo) {
            var random = new Random();
            var emptyCells = Cells.Cast<Cell>().Where(c => !c.HasGold).ToList();
            var randomCell = emptyCells[random.Next(0, emptyCells.Count())];
            playerOne.Cell = randomCell;

            emptyCells = Cells.Cast<Cell>().Where(c => c != playerOne.Cell && !c.HasGold).ToList();
            randomCell = emptyCells[random.Next(0, emptyCells.Count())];
            playerTwo.Cell = randomCell;
        }
    }
}
