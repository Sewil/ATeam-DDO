using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATeamRPG {
    public class Game {
        public static ATeamDB db = new ATeamDB();
        public GameMap GameMap { get; }
        public Game(GameMap map) {
            GameMap = map;
        }
        public static Game Start(params int[] playerIds) {
            
            var players = new List<GamePlayer>();
            foreach (var playerId in playerIds) {
                var player = db.Player.Single(p => p.Id == playerId);
                var gamePlayer = new GamePlayer(playerId, "olle5") {
                    Damage = player.Damage,
                    Gold = player.Gold,
                    Health = player.Health
                };

                players.Add(gamePlayer);
            }
            int mapId = 5;
            var gameMap = GameMap.Load(mapId, players.ToArray());
            if (!db.Map.Any()) {
                var newMap = new Map { Id = gameMap.Id, Cell = UpdateCells(gameMap) };
                db.Map.Add(newMap);
                db.SaveChanges();
            }

            return new Game(gameMap);
        }
        public void UpdateState() {
            UpdateCells(GameMap);
            db.SaveChanges();
        }

        public static List<Cell> UpdateCells(GameMap gameMap) {
            var cells = new List<Cell>();
            foreach (var gameCell in gameMap.Cells) {
                var cell = new Cell {
                    MapId = gameMap.Id,
                    X = gameCell.X,
                    Y = gameCell.Y,
                    PlayerId = gameCell.HasPlayer ? gameCell.Player.Id : null,
                    Gold = gameCell.Gold,
                    IsGround = Convert.ToByte(gameCell.CellType == CellType.Ground)
                };

                cells.Add(cell);

                var original = db.Cell.Find(cell.MapId, cell.X, cell.Y);

                if (original != null) {
                    db.Entry(original).CurrentValues.SetValues(cell);
                }
            }

            db.SaveChanges();

            return cells;
        }
    }
}
