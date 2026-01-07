using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Config;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Maneja la disposición y visualización de las cartas en la mano del jugador
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform handContainer;

        [Header("Settings")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float cardSpacing = 1.5f;
        [SerializeField] private float arcRadius = 8f;
        [SerializeField] private float arcAngle = 30f; // Ángulo total del arco

        private List<Card3DController> cardControllers = new List<Card3DController>();

        /// <summary>
        /// Actualiza la mano con nuevas cartas
        /// </summary>
        public void UpdateHand(List<Card> newHand)
        {
            // Limpiar cartas existentes
            ClearHand();

            if (newHand == null || newHand.Count == 0)
            {
                Debug.Log("[HandManager] No hay cartas para mostrar");
                return;
            }

            Debug.Log($"[HandManager] Creando {newHand.Count} cartas en la mano");

            // Crear nuevas cartas
            for (int i = 0; i < newHand.Count; i++)
            {
                CreateCard(newHand[i], i, newHand.Count);
            }

            // Reorganizar en arco
            ArrangeCardsInArc();
        }

        /// <summary>
        /// Crea una carta 3D
        /// </summary>
        private void CreateCard(Card cardData, int index, int totalCards)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[HandManager] ¡Card Prefab no asignado!");
                return;
            }

            GameObject cardObj = Instantiate(cardPrefab, handContainer);
            Card3DController controller = cardObj.GetComponent<Card3DController>();

            if (controller == null)
            {
                controller = cardObj.AddComponent<Card3DController>();
            }

            controller.Initialize(cardData);
            cardControllers.Add(controller);
        }

        /// <summary>
        /// Organiza las cartas en un arco
        /// </summary>
        private void ArrangeCardsInArc()
        {
            int cardCount = cardControllers.Count;
            if (cardCount == 0) return;

            // Calcular el ángulo entre cada carta
            float angleStep = arcAngle / Mathf.Max(1, cardCount - 1);
            float startAngle = -arcAngle / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                // Calcular posición en el arco
                float angle = startAngle + (angleStep * i);
                float angleRad = angle * Mathf.Deg2Rad;

                // Posición en forma de arco
                float x = Mathf.Sin(angleRad) * arcRadius;
                float y = -Mathf.Cos(angleRad) * arcRadius + arcRadius; // Offset para centrar
                float z = -i * 0.01f; // Pequeño offset en Z para el sorting

                Vector3 position = new Vector3(x, y, z);
                
                // Rotación para que las cartas miren hacia el centro
                Quaternion rotation = Quaternion.Euler(0, 0, -angle);

                // Aplicar transformación
                cardControllers[i].transform.localPosition = position;
                cardControllers[i].transform.localRotation = rotation;
                cardControllers[i].UpdateOriginalPosition(cardControllers[i].transform.position);
            }
        }

        /// <summary>
        /// Organiza las cartas en línea recta (alternativa al arco)
        /// </summary>
        private void ArrangeCardsInLine()
        {
            int cardCount = cardControllers.Count;
            if (cardCount == 0) return;

            float totalWidth = (cardCount - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                Vector3 position = new Vector3(startX + (i * cardSpacing), 0, -i * 0.01f);
                cardControllers[i].transform.localPosition = position;
                cardControllers[i].UpdateOriginalPosition(cardControllers[i].transform.position);
            }
        }

        /// <summary>
        /// Remueve una carta de la mano
        /// </summary>
        public void RemoveCard(Card card)
        {
            var controller = cardControllers.Find(c => c.CardData.id == card.id);
            if (controller != null)
            {
                cardControllers.Remove(controller);
                controller.DestroyWithAnimation();
                
                // Reorganizar cartas restantes
                ArrangeCardsInArc();
            }
        }

        /// <summary>
        /// Limpia todas las cartas de la mano
        /// </summary>
        public void ClearHand()
        {
            foreach (var controller in cardControllers)
            {
                if (controller != null)
                {
                    Destroy(controller.gameObject);
                }
            }
            cardControllers.Clear();
        }

        /// <summary>
        /// Obtiene todas las cartas seleccionadas
        /// </summary>
        public List<Card> GetSelectedCards()
        {
            List<Card> selected = new List<Card>();
            foreach (var controller in cardControllers)
            {
                if (controller.IsSelected)
                {
                    selected.Add(controller.CardData);
                }
            }
            return selected;
        }

        /// <summary>
        /// Deselecciona todas las cartas
        /// </summary>
        public void DeselectAllCards()
        {
            foreach (var controller in cardControllers)
            {
                controller.SetSelected(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizar el arco en el editor
            if (handContainer != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = handContainer.position;
                
                // Dibujar el arco
                int segments = 20;
                float angleStep = arcAngle / segments;
                float startAngle = -arcAngle / 2f;

                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                    float angle2 = (startAngle + angleStep * (i + 1)) * Mathf.Deg2Rad;

                    Vector3 point1 = center + new Vector3(
                        Mathf.Sin(angle1) * arcRadius,
                        -Mathf.Cos(angle1) * arcRadius + arcRadius,
                        0
                    );

                    Vector3 point2 = center + new Vector3(
                        Mathf.Sin(angle2) * arcRadius,
                        -Mathf.Cos(angle2) * arcRadius + arcRadius,
                        0
                    );

                    Gizmos.DrawLine(point1, point2);
                }
            }
        }
    }
}

