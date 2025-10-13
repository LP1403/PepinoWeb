using GameServer.Models;

namespace GameServer.Services
{
    public class GameLogicService
    {
        public void StartGame(GameRoom room)
        {
            if (room.Players.Count < 2)
                return;

            // Calcular modo de juego
            room.GameMode = CardService.CalculateGameMode(room.Players.Count);

            // Crear y barajar mazos
            var allCards = CardService.CreateMultipleDecks(room.GameMode.DeckCount);
            var shuffledDeck = CardService.ShuffleDeck(allCards);

            // Repartir todas las cartas
            var (hands, remainingDeck) = CardService.DealAllCards(shuffledDeck, room.Players.Count);

            // Asignar manos a jugadores
            for (int i = 0; i < room.Players.Count; i++)
            {
                room.Players[i].Hand = hands[i];
            }

            // Encontrar quien tiene el Pepino de Oro
            var pepinoOroIndex = CardService.FindPepinoOroPlayer(hands);
            room.CurrentTurnIndex = pepinoOroIndex;
            room.Players[pepinoOroIndex].IsCurrentTurn = true;

            // Actualizar estado del juego
            room.IsGameStarted = true;
            room.Deck = remainingDeck;
            room.LastPlayedCards.Clear();
            room.LastPlayerId = null;
            room.GameStartedAt = DateTime.UtcNow;
        }

        public bool IsValidCardPlay(List<Card> selectedCards, List<Card> lastPlayedCards, bool isFirstPlay, bool isNewRound = false)
        {
            return CardService.ValidatePlay(selectedCards, lastPlayedCards, isFirstPlay, isNewRound);
        }

        public void PlayCards(GameRoom room, Player player, List<Card> cards)
        {
            if (room.CurrentTurnIndex >= room.Players.Count || room.Players[room.CurrentTurnIndex].ConnectionId != player.ConnectionId)
                return;

            var isFirstPlay = room.LastPlayedCards.Count == 0;
            var isNewRound = room.LastPlayerId == player.ConnectionId && room.LastPlayedCards.Count > 0;
            
            if (!IsValidCardPlay(cards, room.LastPlayedCards, isFirstPlay, isNewRound))
                return;

            // Remover cartas de la mano del jugador
            foreach (var card in cards)
            {
                player.Hand.RemoveAll(c => c.Id == card.Id);
            }

            // Verificar si el jugador ganó
            if (player.Hand.Count == 0)
            {
                player.HasWon = true;
                room.Winners.Add(player.ConnectionId);
            }

            // Verificar si es PEPINEADO
            var isPepineado = CardService.IsPepineado(cards, room.LastPlayedCards);

            // Agregar cartas a la mesa
            room.TableCards.AddRange(cards);
            room.LastPlayedCards = cards;
            room.LastPlayerId = player.ConnectionId;

            // Mover al siguiente turno
            MoveToNextTurn(room, isPepineado);
        }

        public void MoveToNextTurn(GameRoom room, bool skipNext)
        {
            // Limpiar estado del turno actual
            room.Players[room.CurrentTurnIndex].IsCurrentTurn = false;
            room.Players[room.CurrentTurnIndex].IsSkipped = false;

            // Calcular siguiente jugador
            int nextIndex = room.CurrentTurnIndex;
            int skipCount = skipNext ? 2 : 1; // PEPINEADO salta 2 jugadores

            for (int i = 0; i < skipCount; i++)
            {
                do
                {
                    nextIndex = (nextIndex + 1) % room.Players.Count;
                } while (room.Players[nextIndex].HasWon); // Saltar ganadores
            }

            // Si el siguiente jugador está saltado por PEPINEADO, marcarlo
            if (skipNext)
            {
                room.Players[nextIndex].IsSkipped = true;
            }

            room.CurrentTurnIndex = nextIndex;
            room.Players[nextIndex].IsCurrentTurn = true;
        }

        public bool IsGameOver(GameRoom room)
        {
            if (room.GameMode == null) return false;
            return room.Winners.Count >= room.GameMode.MaxWinners;
        }

        public void ResetGame(GameRoom room)
        {
            room.TableCards.Clear();
            room.IsGameStarted = false;
            room.CurrentTurnIndex = 0;
            room.LastPlayedCards.Clear();
            room.LastPlayerId = null;
            room.Winners.Clear();
            room.RoundNumber++;
            room.GameStartedAt = null;
            
            foreach (var player in room.Players)
            {
                player.Hand.Clear();
                player.IsCurrentTurn = false;
                player.IsSkipped = false;
                player.HasWon = false;
            }
        }
    }
}
