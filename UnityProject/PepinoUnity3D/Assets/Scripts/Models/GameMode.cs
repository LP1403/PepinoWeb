using Newtonsoft.Json;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa la configuración del modo de juego
    /// </summary>
    [System.Serializable]
    public class GameMode
    {
        [JsonProperty("deckCount")]
        public int deckCount;          // 1, 2 o 3 mazos
        
        [JsonProperty("maxWinners")]
        public int maxWinners;         // 2 para ≤4 jugadores, 3 para >4 jugadores
        
        [JsonProperty("cardsPerPlayer")]
        public int cardsPerPlayer;     // Calculado automáticamente

        public GameMode()
        {
            deckCount = 1;
            maxWinners = 2;
            cardsPerPlayer = 0;
        }

        public GameMode(int deckCount, int maxWinners, int cardsPerPlayer)
        {
            this.deckCount = deckCount;
            this.maxWinners = maxWinners;
            this.cardsPerPlayer = cardsPerPlayer;
        }

        public int GetTotalCards()
        {
            return deckCount * 40; // Baraja española tiene 40 cartas
        }

        public override string ToString()
        {
            return $"{deckCount} Mazo(s) - {cardsPerPlayer} cartas por jugador";
        }
    }
}

