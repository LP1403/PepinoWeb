using System;

namespace PepinoGame.Models
{
    /// <summary>
    /// Representa una carta del juego de Pepino (baraja española).
    /// Properties (not fields) so System.Text.Json / SignalR deserializes correctly.
    /// </summary>
    [System.Serializable]
    public class Card
    {
        public string suit { get; set; }
        public int value { get; set; }
        public string id { get; set; }
        public bool isPepinoOro { get; set; }

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

        public int GetComparisonValue()
        {
            if (value == 2) return 0;
            if (value == 1) return 13;
            return value;
        }

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

        public override string ToString() => $"{value}{suit}";
    }
}
