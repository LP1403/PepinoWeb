using System.Collections.Generic;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa las cartas jugadas por un jugador
    /// </summary>
    [System.Serializable]
    public class PlayedCards
    {
        public List<Card> cards;
        public string playerId;
        public string playerName;
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

