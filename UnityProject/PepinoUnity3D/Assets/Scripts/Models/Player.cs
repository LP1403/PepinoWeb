using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa un jugador en el juego de Pepino
    /// </summary>
    [System.Serializable]
    public class Player
    {
        [JsonProperty("connectionId")]
        public string connectionId;
        
        [JsonProperty("name")]
        public string name;
        
        [JsonProperty("cardCount")]
        public int cardCount;          // Solo la cantidad de cartas (para otros jugadores)
        
        [JsonProperty("isConnected")]
        public bool isConnected;
        
        [JsonProperty("isCurrentTurn")]
        public bool isCurrentTurn;
        
        [JsonProperty("isSkipped")]
        public bool isSkipped;         // Para el efecto "PEPINEADO"
        
        [JsonProperty("hasWon")]
        public bool hasWon;

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

