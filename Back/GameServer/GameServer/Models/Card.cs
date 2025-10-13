namespace GameServer.Models;

public class Card
{
    public string Suit { get; set; } // "♠", "♥", "♦", "♣"
    public int Value { get; set; }   // 1-12 (A=1, J=11, Q=12, K=13)
    public string Id { get; set; }   // Identificador único
    public bool IsPepinoOro { get; set; } // true si es 3♦

    public Card(string suit, int value)
    {
        Suit = suit;
        Value = value;
        Id = $"{suit}-{value}-{Guid.NewGuid()}";
        IsPepinoOro = suit == "♦" && value == 3;
    }
}