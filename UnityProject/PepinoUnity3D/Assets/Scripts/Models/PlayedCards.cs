using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa las cartas jugadas por un jugador
    /// </summary>
    [System.Serializable]
    public class PlayedCards
    {
        [JsonProperty("cards")]
        public List<Card> cards;
        
        [JsonProperty("playerId")]
        public string playerId;
        
        [JsonProperty("playerName")]
        public string playerName;
        
        [JsonProperty("isPepineado")]
        public bool isPepineado;       // Si es la misma jugada que la anterior

        public PlayedCards()
        {
            cards = new List<Card>();
            playerId = string.Empty;
            playerName = string.Empty;
            isPepineado = false;
        }

        public PlayedCards(List<Card> cards, string playerId, string playerName, bool isPepineado)
        {
            this.cards = cards;
            this.playerId = playerId;
            this.playerName = playerName;
            this.isPepineado = isPepineado;
        }

        public override string ToString()
        {
            string cardsStr = cards != null ? string.Join(", ", cards) : "0";
            return $"{playerName} jugó {cards?.Count ?? 0} carta(s){(isPepineado ? " [PEPINEADO!]" : "")}";
        }
    }
}

