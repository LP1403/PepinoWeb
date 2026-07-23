using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Discard pile (last play) + decorative draw pile. Animates cards flying onto the felt.
    /// </summary>
    public class TableManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform tableContainer;

        [Header("Settings")]
        [SerializeField] private float cardStackSpacing = 0.03f;
        [SerializeField] private Vector3 tableCenter = new Vector3(0.1f, 0.05f, 0.08f);
        [SerializeField] private Vector3 drawPileOffset = new Vector3(-0.38f, 0.05f, 0.06f);
        [SerializeField] private Vector3 cardLocalScale = new Vector3(5.5f, 5.5f, 5.5f);
        [SerializeField] private float flyDuration = 0.45f;

        private readonly List<GameObject> tableCards = new List<GameObject>();
        private readonly List<string> displayedIds = new List<string>();
        private GameObject decorativeDeckRoot;
        private bool decorativeReady;

        private void Awake()
        {
            if (tableContainer == null)
                tableContainer = transform;
        }

        public void Configure(Transform container, GameObject fallbackPrefab)
        {
            Configure(container, fallbackPrefab, tableCenter, drawPileOffset);
        }

        public void Configure(
            Transform container,
            GameObject fallbackPrefab,
            Vector3 tableCenter,
            Vector3 drawPileOffset)
        {
            if (container != null) tableContainer = container;
            if (fallbackPrefab != null) cardPrefab = fallbackPrefab;
            if (tableContainer == null) tableContainer = transform;
            this.tableCenter = tableCenter;
            this.drawPileOffset = drawPileOffset;
            cardLocalScale = new Vector3(5.5f, 5.5f, 5.5f);

            // Force discard rebuild on next Sync (stale IdsMatch would keep tiny cards)
            ClearPlayedCards();

            if (decorativeDeckRoot != null)
            {
                Object.Destroy(decorativeDeckRoot);
                decorativeDeckRoot = null;
            }

            decorativeReady = false;
        }

        public void EnsureDecorativeDeck()
        {
            if (decorativeReady) return;
            decorativeReady = true;

            if (decorativeDeckRoot != null) return;
            if (CardVisualResolver.Instance == null && cardPrefab == null) return;

            decorativeDeckRoot = new GameObject("DecorativeDrawPile");
            decorativeDeckRoot.transform.SetParent(tableContainer, false);
            decorativeDeckRoot.transform.localPosition = tableCenter + drawPileOffset;

            for (int i = 0; i < 5; i++)
            {
                GameObject cardObj = null;
                if (CardVisualResolver.Instance != null)
                    cardObj = CardVisualResolver.Instance.InstantiateCard(new Card("♠", 1), decorativeDeckRoot.transform);

                if (cardObj == null && cardPrefab != null)
                    cardObj = Instantiate(cardPrefab, decorativeDeckRoot.transform);

                if (cardObj == null) break;

                KillPhysics(cardObj);
                foreach (var col in cardObj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                cardObj.transform.localPosition = Vector3.up * (i * 0.014f);
                cardObj.transform.localRotation = CardOrientation.FaceDownOnTable(Random.Range(-4f, 4f));
                cardObj.transform.localScale = cardLocalScale * 0.92f;

                var controller = cardObj.GetComponent<Card3DController>();
                if (controller != null)
                    controller.SetInteractable(false);

                PepinoCardSkin.ApplyBack(cardObj);
            }
        }

        public void SyncDiscardPile(List<Card> lastPlayed, bool animate = true)
        {
            EnsureDecorativeDeck();

            if (IdsMatch(lastPlayed))
                return;

            ClearPlayedCards();

            if (lastPlayed == null || lastPlayed.Count == 0)
                return;

            Vector3 fromWorld = GetFlyFromPosition();
            for (int i = 0; i < lastPlayed.Count; i++)
            {
                if (lastPlayed[i] == null) continue;
                CreateTableCard(lastPlayed[i], animate ? fromWorld : (Vector3?)null, i * 0.06f);
            }
        }

        private Vector3 GetFlyFromPosition()
        {
            var cam = Camera.main;
            if (cam == null)
                return tableContainer.TransformPoint(tableCenter + Vector3.up * 0.4f);

            return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.1f, 1.35f));
        }

        private bool IdsMatch(List<Card> lastPlayed)
        {
            if (lastPlayed == null || lastPlayed.Count == 0)
                return displayedIds.Count == 0;

            if (lastPlayed.Count != displayedIds.Count)
                return false;

            for (int i = 0; i < lastPlayed.Count; i++)
            {
                string id = lastPlayed[i]?.id ?? "";
                if (id != displayedIds[i])
                    return false;
            }

            return true;
        }

        private void CreateTableCard(Card cardData, Vector3? flyFromWorld, float delay)
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

            KillPhysics(cardObj);

            float lateral = (tableCards.Count % 3 - 1) * 0.055f;
            Vector3 localTarget = tableCenter
                                  + new Vector3(lateral, tableCards.Count * cardStackSpacing, 0f);
            Quaternion localRot = CardOrientation.FaceUpOnTable(Random.Range(-10f, 10f));

            if (flyFromWorld.HasValue)
            {
                cardObj.transform.position = flyFromWorld.Value;
                cardObj.transform.rotation = Quaternion.LookRotation(
                    Camera.main != null ? Camera.main.transform.forward : Vector3.forward);
                cardObj.transform.localScale = cardLocalScale * 0.7f;

                Vector3 worldTarget = tableContainer.TransformPoint(localTarget);
                Quaternion worldRot = tableContainer.rotation * localRot;

                LeanTween.move(cardObj, worldTarget, flyDuration)
                    .setDelay(delay)
                    .setEaseOutCubic()
                    .setOnComplete(() =>
                    {
                        if (cardObj == null) return;
                        cardObj.transform.SetParent(tableContainer, true);
                        cardObj.transform.localPosition = localTarget;
                        cardObj.transform.localRotation = localRot;
                    });
                LeanTween.rotate(cardObj, worldRot.eulerAngles, flyDuration)
                    .setDelay(delay)
                    .setEaseOutCubic();
                LeanTween.scale(cardObj, cardLocalScale, flyDuration)
                    .setDelay(delay)
                    .setEaseOutBack();
            }
            else
            {
                cardObj.transform.localPosition = localTarget;
                cardObj.transform.localRotation = localRot;
                cardObj.transform.localScale = cardLocalScale;
            }

            var controller = cardObj.GetComponent<Card3DController>();
            if (controller == null)
                controller = cardObj.AddComponent<Card3DController>();
            controller.Initialize(cardData, preservePackMaterials: true);
            controller.SetInteractable(false);
            PepinoCardSkin.ApplyFaceUp(cardObj, cardData);

            tableCards.Add(cardObj);
            displayedIds.Add(cardData.id ?? "");
        }

        private void ClearPlayedCards()
        {
            foreach (var card in tableCards)
            {
                if (card != null)
                    Destroy(card);
            }

            tableCards.Clear();
            displayedIds.Clear();
        }

        public void ClearTable()
        {
            ClearPlayedCards();
            Debug.Log("[TableManager] Table cleared");
        }

        public void ShowPepineadoEffect()
        {
            if (tableCards.Count == 0) return;

            GameObject lastCard = tableCards[tableCards.Count - 1];
            LeanTween.moveLocalY(lastCard, lastCard.transform.localPosition.y + 0.25f, 0.28f)
                .setEaseOutQuad()
                .setLoopPingPong(1);
            LeanTween.rotateAroundLocal(lastCard, Vector3.up, 360f, 0.55f).setEaseInOutQuad();
        }

        private static void KillPhysics(GameObject cardObj)
        {
            foreach (var rb in cardObj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
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
