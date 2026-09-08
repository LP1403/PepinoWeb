using GameServer.Models;

namespace GameServer.Services;

public static class CardService
{
    public static List<Card> CreateSpanishDeck() => new[] { "♠", "♥", "♦", "♣" }
        .SelectMany(s => Enumerable.Range(1, 12).Select(v => new Card(s, v))).ToList();

    public static List<Card> CreateMultipleDecks(int count)
    {
        if (count is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(count));
        return Enumerable.Range(0, count).SelectMany(_ => CreateSpanishDeck()).ToList();
    }

    public static List<Card> ShuffleDeck(List<Card> cards)
    {
        var result = cards.ToList();
        for (int i = result.Count - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    public static GameMode CalculateGameMode(int players) => MakeMode(1, players);
    public static GameMode MakeMode(int decks, int players) => new()
    {
        DeckCount = decks, MaxWinners = players <= 4 ? 2 : 3,
        CardsPerPlayer = decks * 48 / Math.Max(1, players)
    };

    public static (List<List<Card>> hands, List<Card> remainingDeck) DealAllCards(List<Card> deck, int players)
    {
        if (players < 1) throw new ArgumentOutOfRangeException(nameof(players));
        var hands = Enumerable.Range(0, players).Select(_ => new List<Card>()).ToList();
        for (int i = 0; i < deck.Count; i++) hands[i % players].Add(deck[i]);
        return (hands, new());
    }

    public static int GetCardValue(Card card) => card.Value == 2 ? 0 : card.Value == 1 ? 13 : card.Value;
    public static bool ValidatePlay(List<Card> cards, List<Card>? last, bool first, bool newRound = false)
    {
        if (cards.Count == 0 || cards.Any(c => c.Value is < 1 or > 12) ||
            cards.Any(c => c.Value != cards[0].Value)) return false;
        if (first || newRound || last == null || last.Count == 0 || cards[0].Value == 2) return true;
        return cards.Count == last.Count && GetCardValue(cards[0]) >= GetCardValue(last[0]);
    }
    public static bool IsPepineado(List<Card> cards, List<Card>? last) =>
        cards.Count > 0 && cards[0].Value != 2 && last is { Count: > 0 } && cards.Count == last.Count &&
        cards.All(c => c.Value == last[0].Value);
    public static int FindPepinoOroPlayer(List<List<Card>> hands) =>
        Math.Max(0, hands.FindIndex(h => h.Any(c => c.Suit == "♦" && c.Value == 3)));
    public static void TestCardGeneration() { _ = CreateSpanishDeck(); }
}
