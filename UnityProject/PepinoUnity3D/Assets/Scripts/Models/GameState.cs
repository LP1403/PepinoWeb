using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa el estado completo del juego
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        [JsonProperty("roomId")]
        public string roomId;
        
        [JsonProperty("players")]
        public List<Player> players;
        
        [JsonProperty("tableCards")]
        public List<Card> tableCards;
        
        [JsonProperty("currentTurnIndex")]
        public int currentTurnIndex;
        
        [JsonProperty("lastPlayedCards")]
        public List<Card> lastPlayedCards;
        
        [JsonProperty("lastPlayerId")]
        public string lastPlayerId;
        
        [JsonProperty("isGameStarted")]
        public bool isGameStarted;
        
        [JsonProperty("gameMode")]
        public GameMode gameMode;
        
        [JsonProperty("winners")]
        public List<string> winners;
        
        [JsonProperty("roundNumber")]
        public int roundNumber;
        
        [JsonProperty("yourHand")]
        public List<Card> yourHand;        // Mano privada del jugador actual
        
        [JsonProperty("isRoomCreator")]
        public bool isRoomCreator;         // Si el jugador actual es el creador de la sala
        
        [JsonProperty("isNewRound")]
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

