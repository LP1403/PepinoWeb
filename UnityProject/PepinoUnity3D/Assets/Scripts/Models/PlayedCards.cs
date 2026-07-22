using System.Collections.Generic;

namespace PepinoGame.Models
{
    [System.Serializable]
    public class PlayedCards
    {
        public List<Card> cards { get; set; }
        public string playerId { get; set; }
        public string playerName { get; set; }
        public bool isPepineado { get; set; }

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
            return $"{playerName} jugó {cards?.Count ?? 0} carta(s){(isPepineado ? " [PEPINEADO!]" : "")}";
        }
    }
}
