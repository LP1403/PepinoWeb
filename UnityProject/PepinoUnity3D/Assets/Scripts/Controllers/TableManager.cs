using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Played cards stack on the table using pack prefabs.
    /// </summary>
    public class TableManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform tableContainer;

        [Header("Settings")]
        [SerializeField] private float cardStackSpacing = 0.02f;
        [SerializeField] private Vector3 tableCenter = Vector3.zero;
        [SerializeField] private Vector3 cardLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

        private readonly List<GameObject> tableCards = new List<GameObject>();

        private void Awake()
        {
            if (tableContainer == null)
                tableContainer = transform;
        }

        public void Configure(Transform container, GameObject fallbackPrefab)
        {
            if (container != null) tableContainer = container;
            if (fallbackPrefab != null) cardPrefab = fallbackPrefab;
            if (tableContainer == null) tableContainer = transform;
        }

        public void AddCardsToTable(List<Card> cards, string playerName)
        {
            if (cards == null || cards.Count == 0) return;

            Debug.Log($"[TableManager] {playerName} played {cards.Count} card(s)");

            foreach (var card in cards)
                CreateTableCard(card);
        }

        private void CreateTableCard(Card cardData)
        {
            GameObject cardObj = null;

            if (CardVisualResolver.Instance != null)
                cardObj = CardVisualResolver.Instance.InstantiateCard(cardData, tableContainer);

            if (cardObj == null)
            {
                if (cardPrefab == null)
                {
                    Debug.LogError("[TableManager] No card prefab / resolver available");
                    return;
                }

                cardObj = Instantiate(cardPrefab, tableContainer);
            }

            cardObj.transform.localScale = cardLocalScale;

            foreach (var rb in cardObj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            Vector3 position = tableCenter + Vector3.up * (tableCards.Count * cardStackSpacing);
            cardObj.transform.localPosition = position;
            cardObj.transform.localRotation = CardOrientation.FaceUpOnTable(Random.Range(-14f, 14f));
            cardObj.transform.localScale = cardLocalScale * 1.15f;

            var controller = cardObj.GetComponent<Card3DController>();
            if (controller == null)
                controller = cardObj.AddComponent<Card3DController>();
            controller.Initialize(cardData, preservePackMaterials: true);
            controller.SetInteractable(false);

            tableCards.Add(cardObj);
        }

        public void ClearTable()
        {
            foreach (var card in tableCards)
            {
                if (card != null)
                    Destroy(card);
            }

            tableCards.Clear();
            Debug.Log("[TableManager] Table cleared");
        }

        public void ShowPepineadoEffect()
        {
            if (tableCards.Count == 0) return;

            GameObject lastCard = tableCards[tableCards.Count - 1];
            LeanTween.moveLocalY(lastCard, lastCard.transform.localPosition.y + 0.3f, 0.3f)
                .setEaseOutQuad()
                .setLoopPingPong(1);
            LeanTween.rotateAroundLocal(lastCard, Vector3.up, 360f, 0.6f).setEaseInOutQuad();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 world = tableContainer != null
                ? tableContainer.TransformPoint(tableCenter)
                : tableCenter;
            Gizmos.DrawWireSphere(world, 0.25f);
        }
    }
}
