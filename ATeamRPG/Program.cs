using System;

namespace ATeamRPG {
    class Program {
        static void Main(string[] args) {
            var playerOne = new Player();
            var playerTwo = new Player();

            Console.CursorVisible = false;
            Map map = Map.Load(playerOne, playerTwo);
            playerOne.IsActive = true;
            playerTwo.IsActive = false;
            map.Draw();
            do {
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
                map.SpawnMonster(); // Bg-thread?
                map.SpawnHealthPotion(); // Bg-thread?
                map.Draw();
            } while (true);
        }
    }
}
