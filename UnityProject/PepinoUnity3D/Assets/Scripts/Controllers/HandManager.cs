using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Config;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Local player hand — UNO-style fan at the bottom of the view, faces toward the player.
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform handContainer;

        [Header("Settings")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float cardSpacing = 0.28f;
        [SerializeField] private float arcRadius = 3.4f;
        [SerializeField] private float arcAngle = 58f;
        [SerializeField] private Vector3 cardLocalScale = new Vector3(1.35f, 1.35f, 1.35f);

        private readonly List<Card3DController> cardControllers = new List<Card3DController>();

        private void Awake()
        {
            if (handContainer == null)
                handContainer = transform;
        }

        public void Configure(Transform container, GameObject fallbackPrefab, GameConfig config)
        {
            if (container != null) handContainer = container;
            if (fallbackPrefab != null) cardPrefab = fallbackPrefab;
            if (config != null) gameConfig = config;
            if (handContainer == null) handContainer = transform;
        }

        public void UpdateHand(List<Card> newHand)
        {
            ClearHand();

            if (newHand == null || newHand.Count == 0)
                return;

            for (int i = 0; i < newHand.Count; i++)
                CreateCard(newHand[i]);

            ArrangeCardsInArc();
        }

        private void CreateCard(Card cardData)
        {
            GameObject cardObj = null;

            if (CardVisualResolver.Instance != null)
                cardObj = CardVisualResolver.Instance.InstantiateCard(cardData, handContainer);

            if (cardObj == null)
            {
                if (cardPrefab == null)
                {
                    Debug.LogError("[HandManager] No card prefab / resolver available");
                    return;
                }

                cardObj = Instantiate(cardPrefab, handContainer);
            }

            cardObj.transform.localScale = cardLocalScale;
            DisablePhysics(cardObj);
            EnsureInteractable(cardObj);

            Card3DController controller = cardObj.GetComponent<Card3DController>();
            if (controller == null)
                controller = cardObj.AddComponent<Card3DController>();

            controller.Initialize(cardData, preservePackMaterials: true);
            cardControllers.Add(controller);
        }

        private static void DisablePhysics(GameObject cardObj)
        {
            foreach (var rb in cardObj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }

        private static void EnsureInteractable(GameObject cardObj)
        {
            if (cardObj.GetComponentInChildren<Collider>() == null)
            {
                var box = cardObj.AddComponent<BoxCollider>();
                box.size = new Vector3(0.7f, 0.02f, 1f);
            }

            foreach (var col in cardObj.GetComponentsInChildren<Collider>())
                col.isTrigger = false;
        }

        private void ArrangeCardsInArc()
        {
            int cardCount = cardControllers.Count;
            if (cardCount == 0) return;

            float radius = gameConfig != null ? gameConfig.handArcRadius : arcRadius;
            if (radius < 2f || radius > 6f) radius = arcRadius;

            // Wider fan when many cards (multi-deck hands)
            float angleSpan = Mathf.Lerp(36f, 72f, Mathf.Clamp01((cardCount - 1) / 24f));
            if (arcAngle > 0f) angleSpan = Mathf.Max(angleSpan, arcAngle * 0.85f);

            float angleStep = cardCount == 1 ? 0f : angleSpan / (cardCount - 1);
            float startAngle = -angleSpan / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                float angleRad = angle * Mathf.Deg2Rad;

                // Pull cards toward camera (south / -Z) so they sit in the bottom of the Game view
                float x = Mathf.Sin(angleRad) * radius;
                float y = 0.012f * i;
                float z = -Mathf.Cos(angleRad) * radius * 0.22f;

                cardControllers[i].transform.localPosition = new Vector3(x, y, z);
                cardControllers[i].transform.localRotation = CardOrientation.FacePlayerHand(angle);
                cardControllers[i].UpdateOriginalPosition(cardControllers[i].transform.position);
            }
        }

        public void RemoveCard(Card card)
        {
            var controller = cardControllers.Find(c => c.CardData != null && c.CardData.id == card.id);
            if (controller == null) return;

            cardControllers.Remove(controller);
            controller.DestroyWithAnimation();
            ArrangeCardsInArc();
        }

        public void ClearHand()
        {
            foreach (var controller in cardControllers)
            {
                if (controller != null)
                    Destroy(controller.gameObject);
            }

            cardControllers.Clear();
        }

        public List<Card> GetSelectedCards()
        {
            var selected = new List<Card>();
            foreach (var controller in cardControllers)
            {
                if (controller.IsSelected)
                    selected.Add(controller.CardData);
            }

            return selected;
        }

        public void DeselectAllCards()
        {
            foreach (var controller in cardControllers)
                controller.SetSelected(false);
        }
    }
}
