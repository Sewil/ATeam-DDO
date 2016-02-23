using System;
using System.Data.SqlClient;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {
            var playerOne = new Player("Spelare1");
            var playerTwo = new Player("Spelare2");
            Console.CursorVisible = false;
            Map map = Map.Load(playerOne, playerTwo);
            playerOne.IsActive = true;
            playerTwo.IsActive = false;
            map.Draw();
            do {
                map.SpawnMonster(); // Bg-thread?
                ConsoleKey key = Console.ReadKey().Key;
                Console.Clear();
                if (map.Turn % 2 == 0) {
                    if(key == ConsoleKey.D) {
                        playerOne.OnDied();
                    }

                    map.MovePlayer(key, playerOne);
                    playerOne.IsActive = false;
                    playerTwo.IsActive = true;
                } else {
                    map.MovePlayer(key, playerTwo);
                    playerOne.IsActive = true;
                    playerTwo.IsActive = false;
                }
                map.SpawnHealthPotion(); // Bg-thread?
                map.Draw();
            } while (true);
        }
    }
}
