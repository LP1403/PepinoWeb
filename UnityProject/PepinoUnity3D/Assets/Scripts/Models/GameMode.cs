namespace PepinoGame.Models
{
    [System.Serializable]
    public class GameMode
    {
        public int deckCount { get; set; }
        public int maxWinners { get; set; }
        public int cardsPerPlayer { get; set; }

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

        public int GetTotalCards() => deckCount * 48;

        public override string ToString()
        {
            return $"{deckCount} Mazo(s) - {cardsPerPlayer} cartas por jugador";
        }
    }
}
