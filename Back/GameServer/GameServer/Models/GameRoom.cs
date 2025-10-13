using System.Numerics;
using GameServer.Services;

namespace GameServer.Models
{
    public class GameRoom
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<Player> Players { get; set; } = new();
        public List<Card> TableCards { get; set; } = new();
        public List<Card> Deck { get; set; } = new();
        public bool IsGameStarted { get; set; } = false;
        public int CurrentTurnIndex { get; set; } = 0;
        public List<Card> LastPlayedCards { get; set; } = new();
        public string? LastPlayerId { get; set; }
        public GameMode? GameMode { get; set; }
        public List<string> Winners { get; set; } = new();
        public int RoundNumber { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? GameStartedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatorName { get; set; }

        public bool IsFull => Players.Count >= 8;
        public bool CanStartGame => Players.Count >= 2 && !IsGameStarted;
        public bool IsGameActive => IsGameStarted;
    }
}
