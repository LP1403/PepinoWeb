using System.Collections.Generic;

namespace PepinoGame.Models
{
    [System.Serializable]
    public class GameState
    {
        public string roomId { get; set; }
        public List<Player> players { get; set; }
        public List<Card> tableCards { get; set; }
        public int currentTurnIndex { get; set; }
        public List<Card> lastPlayedCards { get; set; }
        public string lastPlayerId { get; set; }
        public bool isGameStarted { get; set; }
        public GameMode gameMode { get; set; }
        public List<string> winners { get; set; }
        public int roundNumber { get; set; }
        public List<Card> yourHand { get; set; }
        public bool isRoomCreator { get; set; }
        public bool isNewRound { get; set; }

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
