using System.Collections.Generic;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa el estado completo del juego
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        public string roomId;
        public List<Player> players;
        public List<Card> tableCards;
        public int currentTurnIndex;
        public List<Card> lastPlayedCards;
        public string lastPlayerId;
        public bool isGameStarted;
        public GameMode gameMode;
        public List<string> winners;
        public int roundNumber;
        public List<Card> yourHand;        // Mano privada del jugador actual
        public bool isRoomCreator;         // Si el jugador actual es el creador de la sala
        public bool isNewRound;            // Si es una nueva ronda (vuelta completa)

        public GameState()
        {
            roomId = string.Empty;
            players = new List<Player>();
            tableCards = new List<Card>();
            currentTurnIndex = 0;
            lastPlayedCards = new List<Card>();
            lastPlayerId = string.Empty;
            isGameStarted = false;
            gameMode = null;
            winners = new List<string>();
            roundNumber = 1;
            yourHand = new List<Card>();
            isRoomCreator = false;
            isNewRound = false;
        }

        public Player GetCurrentPlayer()
        {
            if (players == null || players.Count == 0 || currentTurnIndex < 0 || currentTurnIndex >= players.Count)
                return null;
            
            return players[currentTurnIndex];
        }

        public bool IsMyTurn(string myConnectionId)
        {
            var currentPlayer = GetCurrentPlayer();
            return currentPlayer != null && currentPlayer.connectionId == myConnectionId;
        }

        public bool IsFirstPlay()
        {
            return lastPlayedCards == null || lastPlayedCards.Count == 0;
        }

        public int GetMyCardCount()
        {
            return yourHand != null ? yourHand.Count : 0;
        }
    }
}

