using System;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {
            Console.CursorVisible = false;
            Game game = Game.Start(2, 3);
            GameMap map = game.GameMap;
            game.UpdateState();
            var playerOne = map.Players[0];
            var playerTwo = map.Players[1];
            map.Draw();
            do {
                map.SpawnMonster(); // Bg-thread?
                ConsoleKey key = Console.ReadKey().Key;
                Console.Clear();
                if (map.Turn % 2 == 0) {
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
