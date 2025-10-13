using GameServer.Models;

namespace GameServer.Services
{
    public static class CardService
    {
        private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
        private static readonly int[] Values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }; // A=1, J=11, Q=12, K=13

        public static List<Card> CreateSpanishDeck()
        {
            var deck = new List<Card>();
            foreach (var suit in Suits)
            {
                foreach (var value in Values)
                {
                    deck.Add(new Card(suit, value));
                }
            }
            Console.WriteLine($"🃏 Mazo español creado: {deck.Count} cartas");
            return deck;
        }

        public static List<Card> CreateMultipleDecks(int deckCount)
        {
            var allCards = new List<Card>();
            for (int i = 0; i < deckCount; i++)
            {
                var deck = CreateSpanishDeck();
                foreach (var card in deck)
                {
                    card.Id = $"{card.Id}-deck{i}";
                }
                allCards.AddRange(deck);
            }
            Console.WriteLine($"🃏 {deckCount} mazos creados: {allCards.Count} cartas totales");
            return allCards;
        }

        // Método de prueba para verificar cartas
        public static void TestCardGeneration()
        {
            Console.WriteLine("🧪 Probando generación de cartas...");
            var deck = CreateSpanishDeck();
            Console.WriteLine($"📊 Mazo generado: {deck.Count} cartas");
            
            if (deck.Count > 0)
            {
                var sampleCards = deck.Take(5).Select(c => $"{c.Value}{c.Suit}").ToList();
                Console.WriteLine($"📋 Muestra de cartas: {string.Join(", ", sampleCards)}");
            }
            
            var shuffled = ShuffleDeck(deck);
            Console.WriteLine($"🔀 Mazo barajado: {shuffled.Count} cartas");
            
            var (hands, remaining) = DealAllCards(shuffled, 2);
            Console.WriteLine($"🎴 Repartido entre 2 jugadores: {hands.Count} manos, {remaining.Count} restantes");
            
            for (int i = 0; i < hands.Count; i++)
            {
                Console.WriteLine($"👤 Jugador {i + 1}: {hands[i].Count} cartas");
                if (hands[i].Count > 0)
                {
                    var sample = hands[i].Take(3).Select(c => $"{c.Value}{c.Suit}").ToList();
                    Console.WriteLine($"   📋 Muestra: {string.Join(", ", sample)}");
                }
            }
        }

        public static List<Card> ShuffleDeck(List<Card> deck)
        {
            var shuffled = new List<Card>(deck);
            var random = new Random();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            return shuffled;
        }

        public static GameMode CalculateGameMode(int playerCount)
        {
            int deckCount, maxWinners;

            if (playerCount <= 4)
            {
                deckCount = Math.Min(2, Math.Max(1, (int)Math.Ceiling(40.0 / playerCount)));
                maxWinners = 2;
            }
            else
            {
                deckCount = Math.Min(3, Math.Max(1, (int)Math.Ceiling(40.0 / playerCount)));
                maxWinners = 3;
            }

            int totalCards = deckCount * 40;
            int cardsPerPlayer = totalCards / playerCount;

            return new GameMode
            {
                DeckCount = deckCount,
                MaxWinners = maxWinners,
                CardsPerPlayer = cardsPerPlayer
            };
        }

        public static (List<List<Card>> hands, List<Card> remainingDeck) DealAllCards(List<Card> deck, int numPlayers)
        {
            var hands = new List<List<Card>>();
            for (int i = 0; i < numPlayers; i++)
            {
                hands.Add(new List<Card>());
            }

            var remainingDeck = new List<Card>(deck);
            int currentPlayer = 0;

            // Repartir todas las cartas equitativamente (1 carta por jugador hasta agotar el mazo)
            while (remainingDeck.Count > 0)
            {
                var card = remainingDeck[remainingDeck.Count - 1];
                remainingDeck.RemoveAt(remainingDeck.Count - 1);
                hands[currentPlayer].Add(card);
                currentPlayer = (currentPlayer + 1) % numPlayers;
            }

            Console.WriteLine($"🎴 Cartas repartidas (total: {deck.Count} cartas):");
            for (int i = 0; i < hands.Count; i++)
            {
                Console.WriteLine($"   👤 Jugador {i}: {hands[i].Count} cartas");
                if (hands[i].Count > 0)
                {
                    var sample = hands[i].Take(3).Select(c => $"{c.Value}{c.Suit}").ToList();
                    Console.WriteLine($"      📋 Muestra: {string.Join(", ", sample)}");
                }
            }

            return (hands, remainingDeck);
        }

        public static int GetCardValue(Card card)
        {
            if (card.Value == 2) return 0; // Comodín
            if (card.Value == 1) return 13; // El 1 es el más alto
            return card.Value; // 3-12 mantienen su valor
        }

        public static bool ValidatePlay(List<Card> selectedCards, List<Card> lastPlayedCards, bool isFirstPlay)
        {
            if (selectedCards.Count == 0) return false;

            // Verificar que todas las cartas tengan el mismo valor
            var firstValue = selectedCards[0].Value;
            if (!selectedCards.All(c => c.Value == firstValue)) return false;

            // Si es la primera jugada, cualquier carta es válida
            if (isFirstPlay || lastPlayedCards == null || lastPlayedCards.Count == 0) return true;

            // Verificar que la cantidad de cartas sea la misma
            if (selectedCards.Count != lastPlayedCards.Count) return false;

            // Verificar que el valor sea mayor O IGUAL (para PEPINEADO)
            var lastValue = GetCardValue(lastPlayedCards[0]);
            var currentValue = GetCardValue(selectedCards[0]);

            return currentValue >= lastValue;
        }

        public static bool IsPepineado(List<Card> selectedCards, List<Card> lastPlayedCards)
        {
            if (lastPlayedCards == null || lastPlayedCards.Count == 0) return false;
            if (selectedCards.Count != lastPlayedCards.Count) return false;

            var selectedValue = selectedCards[0].Value;
            var lastValue = lastPlayedCards[0].Value;

            return selectedValue == lastValue && selectedCards.All(c => c.Value == selectedValue);
        }

        public static int FindPepinoOroPlayer(List<List<Card>> hands)
        {
            Console.WriteLine($"🥒 Buscando Pepino de Oro (3♦) entre {hands.Count} jugadores...");
            
            for (int i = 0; i < hands.Count; i++)
            {
                Console.WriteLine($"🔍 Revisando mano del jugador {i} ({hands[i].Count} cartas):");
                var pepinoOroCards = hands[i].Where(c => c.IsPepinoOro).ToList();
                
                // Log de todas las cartas del jugador para debugging
                var allCards = hands[i].Select(c => $"{c.Value}{c.Suit}").ToList();
                Console.WriteLine($"   📋 Todas las cartas: {string.Join(", ", allCards)}");
                
                if (pepinoOroCards.Any())
                {
                    Console.WriteLine($"🥒 ¡ENCONTRADO! Jugador {i} tiene {pepinoOroCards.Count} Pepino(s) de Oro: {string.Join(", ", pepinoOroCards.Select(c => $"{c.Value}{c.Suit}"))}");
                    return i;
                }
                else
                {
                    Console.WriteLine($"   ❌ Jugador {i} NO tiene Pepino de Oro");
                }
            }
            
            Console.WriteLine($"🥒 No se encontró Pepino de Oro, iniciando con jugador 0");
            return 0;
        }
    }
}
