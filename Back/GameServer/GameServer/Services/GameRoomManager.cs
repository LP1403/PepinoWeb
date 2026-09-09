using System.Collections.Concurrent;
using GameServer.Models;

namespace GameServer.Services;

public class GameRoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> rooms = new();
    public GameRoom GetOrCreateRoom(string id) => rooms.GetOrAdd(id, key => new GameRoom { Id = key });
    public GameRoom? GetRoom(string id) => rooms.GetValueOrDefault(id);
    public List<GameRoom> GetActiveRooms() => rooms.Values.ToList();
    public void RemoveRoom(string id) => rooms.TryRemove(id, out _);
    public bool RemoveIfEmpty(GameRoom room)
    {
        return room.Players.Count == 0 && ((ICollection<KeyValuePair<string, GameRoom>>)rooms).Remove(new KeyValuePair<string, GameRoom>(room.Id, room));
    }
}
