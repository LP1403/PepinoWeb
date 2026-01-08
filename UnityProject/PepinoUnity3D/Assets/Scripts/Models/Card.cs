using System;
using UnityEngine;
using Newtonsoft.Json;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa una carta del juego de Pepino (baraja española)
    /// </summary>
    [System.Serializable]
    public class Card
    {
        [JsonProperty("suit")]
        public string suit;          // "♠", "♥", "♦", "♣"
        
        [JsonProperty("value")]
        public int value;            // 1-12 (A=1, J=11, Q=12, K=13)
        
        [JsonProperty("id")]
        public string id;            // Identificador único
        
        [JsonProperty("isPepinoOro")]
        public bool isPepinoOro;     // true si es 3♦

        public Card() { }

        public Card(string suit, int value)
        {
            this.suit = suit;
            this.value = value;
            this.id = $"{suit}-{value}-{Guid.NewGuid()}";
            this.isPepinoOro = suit == "♦" && value == 3;
        }

        public Card(string suit, int value, string id)
        {
            this.suit = suit;
            this.value = value;
            this.id = id;
            this.isPepinoOro = suit == "♦" && value == 3;
        }

        /// <summary>
        /// Obtiene el valor numérico para comparación
        /// Comodín (2) = 0, As (1) = 13, resto mantiene su valor
        /// </summary>
        public int GetComparisonValue()
        {
            if (value == 2) return 0;  // Comodín
            if (value == 1) return 13; // As es el más alto
            return value;
        }

        /// <summary>
        /// Obtiene el nombre de la carta en español
        /// </summary>
        public string GetCardName()
        {
            string valueName = value switch
            {
                1 => "As",
                2 => "Comodín",
                3 => "Tres",
                4 => "Cuatro",
                5 => "Cinco",
                6 => "Seis",
                7 => "Siete",
                8 => "Ocho",
                9 => "Nueve",
                10 => "Sota",
                11 => "Caballo",
                12 => "Rey",
                _ => value.ToString()
            };

            string suitName = suit switch
            {
                "♠" => "Espadas",
                "♥" => "Copas",
                "♦" => "Oros",
                "♣" => "Bastos",
                _ => suit
            };

            return $"{valueName} de {suitName}";
        }

        /// <summary>
        /// Obtiene la profesión asociada al palo
        /// </summary>
        public string GetProfession()
        {
            return suit switch
            {
                "♠" => "Policía",
                "♥" => "Médico",
                "♦" => "Soldado",
                "♣" => "Bufón",
                _ => "Desconocido"
            };
        }

        public override string ToString()
        {
            return $"{value}{suit}";
        }
    }
}

