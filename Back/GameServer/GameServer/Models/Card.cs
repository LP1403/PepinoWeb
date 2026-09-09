namespace GameServer.Models;

public class Card
{
    public string Suit { get; set; } // "♠", "♥", "♦", "♣"
    public int Value { get; set; }   // 1-12 (A=1, J=11, Q=12, K=13)
    public string Id { get; set; }   // Identificador único
    public bool IsPepinoOro { get; set; } // true si es 3♦
    public int DeckIndex { get; set; } // Índice visual del mazo: 0, 1 o 2

    public Card(string suit, int value, int deckIndex = 0)
    {
        Suit = suit;
        Value = value;
        DeckIndex = deckIndex;
        Id = $"{suit}-{value}-deck{deckIndex}-{Guid.NewGuid()}";
        IsPepinoOro = suit == "♦" && value == 3;
    }
}
