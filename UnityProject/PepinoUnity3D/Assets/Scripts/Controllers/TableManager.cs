using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Maneja las cartas en la mesa (cartas jugadas)
    /// </summary>
    public class TableManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform tableContainer;

        [Header("Settings")]
        [SerializeField] private float cardStackSpacing = 0.05f;
        [SerializeField] private Vector3 tableCenter = Vector3.zero;

        private List<GameObject> tableCards = new List<GameObject>();

        /// <summary>
        /// Añade cartas a la mesa
        /// </summary>
        public void AddCardsToTable(List<Card> cards, string playerName)
        {
            if (cards == null || cards.Count == 0) return;

            Debug.Log($"[TableManager] {playerName} jugó {cards.Count} cartas");

            foreach (var card in cards)
            {
                CreateTableCard(card);
            }
        }

        /// <summary>
        /// Crea una carta en la mesa
        /// </summary>
        private void CreateTableCard(Card cardData)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[TableManager] ¡Card Prefab no asignado!");
                return;
            }

            GameObject cardObj = Instantiate(cardPrefab, tableContainer);
            
            // Posición en la pila (cada carta ligeramente más alta)
            Vector3 position = tableCenter + Vector3.up * (tableCards.Count * cardStackSpacing);
            cardObj.transform.position = position;

            // Rotación aleatoria para efecto natural
            cardObj.transform.rotation = Quaternion.Euler(0, Random.Range(-10f, 10f), 0);

            tableCards.Add(cardObj);
        }

        /// <summary>
        /// Limpia la mesa
        /// </summary>
        public void ClearTable()
        {
            foreach (var card in tableCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            tableCards.Clear();
            
            Debug.Log("[TableManager] Mesa limpiada");
        }

        /// <summary>
        /// Muestra efecto de PEPINEADO
        /// </summary>
        public void ShowPepineadoEffect()
        {
            Debug.Log("[TableManager] 🥒 ¡PEPINEADO!");
            
            // Animar las últimas cartas jugadas
            if (tableCards.Count > 0)
            {
                GameObject lastCard = tableCards[tableCards.Count - 1];
                
                // Efecto de rebote
                LeanTween.moveY(lastCard, lastCard.transform.position.y + 1f, 0.3f)
                    .setEaseOutQuad()
                    .setLoopPingPong(1);

                // Rotar
                LeanTween.rotateY(lastCard, 360f, 0.6f).setEaseInOutQuad();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizar el centro de la mesa en el editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(tableCenter, 0.5f);
        }
    }
}

