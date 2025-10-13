using GameServer.Models;

namespace GameServer.Interfaces
{
    public interface IGameRoomManager
    {
        GameRoom GetOrCreateRoom(string roomId);
        GameRoom GetRoom(string roomId);
        GameRoom GetRoomByPlayerId(string playerId);
        void RemoveRoom(string roomId);
        List<GameRoom> GetAllRooms();
    }
}
