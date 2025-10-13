using GameServer.Models;
using GameServer.Services;

namespace GameServer.Services
{
    public class GameRoomManager
    {
        private readonly Dictionary<string, GameRoom> _rooms = new();

        public GameRoom GetOrCreateRoom(string roomId)
        {
            if (!_rooms.ContainsKey(roomId))
            {
                _rooms[roomId] = new GameRoom { Id = roomId };
            }
            return _rooms[roomId];
        }

        public GameRoom? GetRoom(string roomId)
        {
            return _rooms.ContainsKey(roomId) ? _rooms[roomId] : null;
        }

        public bool StartGame(string roomId)
        {
            var room = GetOrCreateRoom(roomId);
            if (!room.CanStartGame)
                return false;

            var gameLogic = new GameLogicService();
            gameLogic.StartGame(room);
            return true;
        }

        public void RemovePlayer(string connectionId)
        {
            var room = _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
            if (room != null)
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (player != null)
                {
                    room.Players.Remove(player);
                    
                    // Si el juego está activo y el jugador era el turno actual, mover al siguiente
                    if (room.IsGameActive && room.CurrentTurnIndex < room.Players.Count)
                    {
                        var gameLogic = new GameLogicService();
                        gameLogic.MoveToNextTurn(room, false);
                    }

                    // Si la sala está vacía, removerla
                    if (room.Players.Count == 0)
                    {
                        _rooms.Remove(room.Id);
                    }
                    // Si el juego está activo y solo queda un jugador, terminar el juego
                    else if (room.IsGameActive && room.Players.Count == 1)
                    {
                        room.IsGameStarted = false;
                    }
                }
            }
        }

        public List<GameRoom> GetActiveRooms()
        {
            return _rooms.Values.Where(r => r.Players.Count > 0).ToList();
        }

        public void CleanupEmptyRooms()
        {
            var emptyRooms = _rooms.Values.Where(r => r.Players.Count == 0).ToList();
            foreach (var room in emptyRooms)
            {
                _rooms.Remove(room.Id);
            }
        }

        public GameRoom? GetRoomByPlayerConnection(string connectionId)
        {
            return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
        }

        public GameRoom? GetRoomByConnectionId(string connectionId)
        {
            return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
        }

        public void RemoveRoom(string roomId)
        {
            if (_rooms.ContainsKey(roomId))
            {
                _rooms.Remove(roomId);
            }
        }
    }
}
