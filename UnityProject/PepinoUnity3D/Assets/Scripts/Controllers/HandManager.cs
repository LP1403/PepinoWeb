using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Config;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Local hand — UNO style: large fan in camera space at the bottom of the view.
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform handContainer;

        [Header("Settings")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float arcRadius = 0.72f;
        [SerializeField] private float arcAngle = 54f;
        [SerializeField] private Vector3 cardLocalScale = new Vector3(2.9f, 2.9f, 2.9f);

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

            // Slightly shrink when the hand is huge so cards stay on screen
            float scaleMul = cardControllers.Count > 18 ? 0.82f : 1f;
            cardObj.transform.localScale = cardLocalScale * scaleMul;
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

            // Re-apply scale now that we know final count
            float scaleMul = cardCount > 18 ? 0.82f : (cardCount > 12 ? 0.9f : 1f);
            Vector3 scale = cardLocalScale * scaleMul;

            float angleSpan = Mathf.Lerp(36f, 68f, Mathf.Clamp01((cardCount - 1) / 28f));
            float radius = arcRadius * (cardCount > 16 ? 1.12f : 1f);

            float angleStep = cardCount == 1 ? 0f : angleSpan / (cardCount - 1);
            float startAngle = -angleSpan / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                float angleRad = angle * Mathf.Deg2Rad;

                // Camera-local fan across the bottom of the lens
                float x = Mathf.Sin(angleRad) * radius;
                float y = (1f - Mathf.Cos(angleRad)) * -0.06f + i * 0.003f;
                float z = -Mathf.Abs(Mathf.Sin(angleRad)) * 0.04f;

                var t = cardControllers[i].transform;
                t.localScale = scale;
                t.localPosition = new Vector3(x, y, z);
                t.localRotation = CardOrientation.FacePlayerHand(angle);
                cardControllers[i].UpdateOriginalPosition(t.position);
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
