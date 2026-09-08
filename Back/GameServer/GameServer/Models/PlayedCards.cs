using System.Collections.Generic;

namespace GameServer.Models
{
    public class PlayedCards
    {
        public long Sequence { get; set; }
        public string? SkippedPlayerId { get; set; }
        public string? SkippedPlayerName { get; set; }
        public bool IsWildcard { get; set; }
        public List<Card> Cards { get; set; } = new();
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public bool IsPepineado { get; set; } = false;
    }
} 
