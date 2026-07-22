namespace PepinoGame.Models
{
    [System.Serializable]
    public class Player
    {
        public string connectionId { get; set; }
        public string name { get; set; }
        public int cardCount { get; set; }
        public bool isConnected { get; set; }
        public bool isCurrentTurn { get; set; }
        public bool isSkipped { get; set; }
        public bool hasWon { get; set; }

        public Player()
        {
            connectionId = string.Empty;
            name = string.Empty;
            cardCount = 0;
            isConnected = true;
            isCurrentTurn = false;
            isSkipped = false;
            hasWon = false;
        }

        public Player(string connectionId, string name)
        {
            this.connectionId = connectionId;
            this.name = name;
            this.cardCount = 0;
            this.isConnected = true;
            this.isCurrentTurn = false;
            this.isSkipped = false;
            this.hasWon = false;
        }

        public override string ToString()
        {
            return $"{name} (Cartas: {cardCount}, Turno: {isCurrentTurn}, Ganó: {hasWon})";
        }
    }
}
