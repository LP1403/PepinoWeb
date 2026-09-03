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
        [SerializeField] private Vector3 cardLocalScale = new Vector3(1f, 1f, 1f);
        [SerializeField] private float tableCardLongEdge = 0.22f;
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
            tableCardLongEdge = 0.22f;

            ClearPlayedCards();
            ClearDecorativeDeck();
        }

        public void ClearDecorativeDeck()
        {
            decorativeReady = false;
            if (decorativeDeckRoot != null)
            {
                Object.Destroy(decorativeDeckRoot);
                decorativeDeckRoot = null;
            }

            // Kill duplicates left from older runs
            if (tableContainer != null)
            {
                for (int i = tableContainer.childCount - 1; i >= 0; i--)
                {
                    var c = tableContainer.GetChild(i);
                    if (c != null && c.name == "DecorativeDrawPile")
                        Object.Destroy(c.gameObject);
                }
            }
        }

        /// <summary>
        /// Small face-down Pepino-back quads (never Kenney meshes — those blew up to wall-size).
        /// </summary>
        public void EnsureDecorativeDeck()
        {
            if (decorativeReady && decorativeDeckRoot != null) return;
            decorativeReady = true;

            ClearDecorativeDeck();
            decorativeReady = true;

            if (tableContainer == null) return;

            decorativeDeckRoot = new GameObject("DecorativeDrawPile");
            decorativeDeckRoot.transform.SetParent(tableContainer, false);
            decorativeDeckRoot.transform.localPosition = tableCenter + drawPileOffset;

            var backMat = PepinoCardSkin.GetBackMaterial();
            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "DeckBack";
                go.transform.SetParent(decorativeDeckRoot.transform, false);
                Object.Destroy(go.GetComponent<Collider>());

                // Flat on felt, readable from above (quad faces +Z → tip up)
                go.transform.localPosition = new Vector3(
                    Random.Range(-0.004f, 0.004f),
                    0.002f + i * 0.003f,
                    Random.Range(-0.004f, 0.004f));
                go.transform.localRotation = Quaternion.Euler(90f, Random.Range(-6f, 6f), 0f);
                // ~mockup draw-pile size
                go.transform.localScale = new Vector3(0.14f, 0.20f, 1f);

                var rend = go.GetComponent<Renderer>();
                if (backMat != null)
                    rend.sharedMaterial = backMat;
                else
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                    var mat = new Material(shader);
                    var forest = new Color(0.08f, 0.28f, 0.18f, 1f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", forest);
                    if (mat.HasProperty("_Color")) mat.color = forest;
                    rend.sharedMaterial = mat;
                }

                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void SyncDiscardPile(List<Card> lastPlayed, bool animate = true)
        {
            SyncDiscardPile(lastPlayed, animate, fromPlayerId: null);
        }

        public void SyncDiscardPile(List<Card> lastPlayed, bool animate, string fromPlayerId)
        {
            EnsureDecorativeDeck();

            if (IdsMatch(lastPlayed))
                return;

            ClearPlayedCards();

            if (lastPlayed == null || lastPlayed.Count == 0)
                return;

            Vector3? fromWorld = animate ? GetFlyFromPosition(fromPlayerId) : null;
            for (int i = 0; i < lastPlayed.Count; i++)
            {
                if (lastPlayed[i] == null) continue;
                CreateTableCard(lastPlayed[i], fromWorld, i * 0.06f);
            }
        }

        /// <summary>
        /// Fly-from: rival seat if we know who played; local hand band if it was me; else far rim.
        /// </summary>
        private Vector3 GetFlyFromPosition(string fromPlayerId)
        {
            string myId = Managers.NetworkManager.Instance != null
                ? Managers.NetworkManager.Instance.MyConnectionId
                : null;

            bool isLocal = !string.IsNullOrEmpty(fromPlayerId)
                           && !string.IsNullOrEmpty(myId)
                           && fromPlayerId == myId;

            if (isLocal)
            {
                var cam = Camera.main;
                if (cam != null)
                    return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.12f, 1.35f));
            }

            if (!string.IsNullOrEmpty(fromPlayerId))
            {
                var seats = Object.FindAnyObjectByType<OpponentSeatManager>();
                if (seats != null && seats.TryGetPlayOrigin(fromPlayerId, out Vector3 rivalOrigin))
                    return rivalOrigin + Vector3.up * 0.05f;
            }

            // Unknown player: far side of table (never the local hand band)
            if (tableContainer != null)
                return tableContainer.TransformPoint(tableCenter + new Vector3(0f, 0.25f, 0.55f));

            return Vector3.up;
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

            float lateral = (tableCards.Count % 3 - 1) * 0.04f;
            Vector3 localTarget = tableCenter
                                  + new Vector3(lateral, tableCards.Count * cardStackSpacing, 0f);
            Quaternion localRot = CardOrientation.FaceUpOnTable(Random.Range(-10f, 10f));

            cardObj.transform.localScale = Vector3.one;
            cardObj.transform.localRotation = localRot;
            FitCardToTableSize(cardObj, tableCardLongEdge);
            Vector3 fittedScale = cardObj.transform.localScale;

            if (flyFromWorld.HasValue)
            {
                cardObj.transform.position = flyFromWorld.Value;
                cardObj.transform.rotation = Quaternion.LookRotation(
                    Camera.main != null ? Camera.main.transform.forward : Vector3.forward);
                cardObj.transform.localScale = fittedScale * 0.7f;

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
                        cardObj.transform.localScale = fittedScale;
                    });
                LeanTween.rotate(cardObj, worldRot.eulerAngles, flyDuration)
                    .setDelay(delay)
                    .setEaseOutCubic();
                LeanTween.scale(cardObj, fittedScale, flyDuration)
                    .setDelay(delay)
                    .setEaseOutBack();
            }
            else
            {
                cardObj.transform.localPosition = localTarget;
                cardObj.transform.localRotation = localRot;
                cardObj.transform.localScale = fittedScale;
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

        /// <summary>Scale so the longest world edge matches mockup table-card size (~22cm).</summary>
        private static void FitCardToTableSize(GameObject cardObj, float targetLongEdge)
        {
            if (cardObj == null) return;
            var rend = cardObj.GetComponentInChildren<Renderer>();
            if (rend == null) return;

            // Force layout refresh
            Physics.SyncTransforms();
            float longest = Mathf.Max(rend.bounds.size.x, rend.bounds.size.y, rend.bounds.size.z);
            if (longest < 0.001f) return;

            float mul = targetLongEdge / longest;
            cardObj.transform.localScale = cardObj.transform.localScale * mul;
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
            ClearDecorativeDeck();
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
