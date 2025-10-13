namespace GameServer.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Card> Hand { get; set; } = new();
        public bool IsConnected { get; set; } = true;
        public bool IsCurrentTurn { get; set; } = false;
        public bool IsSkipped { get; set; } = false;
        public bool HasWon { get; set; } = false;
    }
}
