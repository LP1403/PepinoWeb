using System.Collections.Generic;

namespace GameServer.Models
{
    public class PlayedCards
    {
        public List<Card> Cards { get; set; } = new();
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public bool IsPepineado { get; set; } = false;
    }
} 