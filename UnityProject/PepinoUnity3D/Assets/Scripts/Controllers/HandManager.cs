using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Models;
using PepinoGame.Config;
using PepinoGame.Managers;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Mano estilo UNO: abajo en pantalla, stacks por valor, scroll horizontal,
    /// selección por click en pantalla (no depende de OnMouse/colliders finos).
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        public const int HandLayer = 31;

        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform handContainer;

        [Header("Settings")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Vector3 cardLocalScale = new Vector3(0.95f, 0.95f, 0.95f);

        [Header("Viewport layout (0-1 screen)")]
        [SerializeField] private float viewportY = 0.05f;
        [SerializeField] private float viewportDepth = 0.68f;
        [SerializeField] private float viewportXMin = 0.2f;
        [SerializeField] private float viewportXMax = 0.8f;
        [SerializeField] private float stackDepthStep = 0.035f;
        [SerializeField] private float stackPeekX = 0.01f;
        [SerializeField] private float stackPeekY = 0.012f;
        [SerializeField] private float scrollSensitivity = 0.08f;

        private readonly List<Card3DController> cardControllers = new List<Card3DController>();
        private Camera handCamera;
        private int mainCamOriginalMask = -1;
        private float scrollOffset; // desplaza el abanico en X (viewport)
        private bool pointerDown;
        private bool draggedThisGesture;
        private Vector2 pointerDownPos;
        private float scrollAtPointerDown;

        private void Awake()
        {
            if (handContainer == null)
                handContainer = transform;

            EnsureHandOverlayCamera();
        }

        private void OnDestroy()
        {
            RestoreMainCameraMask();
            if (handCamera != null)
                Destroy(handCamera.gameObject);
        }

        public void Configure(Transform container, GameObject fallbackPrefab, GameConfig config)
        {
            if (container != null) handContainer = container;
            if (fallbackPrefab != null) cardPrefab = fallbackPrefab;
            if (config != null) gameConfig = config;
            if (handContainer == null) handContainer = transform;

            // Full cards visible in lower band (mockup: hand not clipped by screen edge)
            cardLocalScale = new Vector3(0.88f, 0.88f, 0.88f);
            viewportY = 0.175f;
            viewportDepth = 0.82f;
            viewportXMin = 0.16f;
            viewportXMax = 0.84f;

            EnsureHandOverlayCamera();
        }

        public void UpdateHand(List<Card> newHand)
        {
            ClearHand();
            scrollOffset = 0f;

            if (newHand == null || newHand.Count == 0)
                return;

            foreach (var card in newHand.OrderBy(c => c.GetComparisonValue()).ThenBy(c => c.suit))
                CreateCard(card);

            ArrangeCardsInViewport();
            SyncSelectionVisuals();
        }

        private void Update()
        {
            HandlePointer();
        }

        private void LateUpdate()
        {
            SyncHandCameraToMain();
            if (cardControllers.Count > 0)
            {
                ArrangeCardsInViewport();
                RefreshPlayAssist();
            }
        }

        private void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();

            float wheel = mouse.scroll.ReadValue().y;
            if (cardControllers.Count > 0 && Mathf.Abs(wheel) > 0.01f)
            {
                scrollOffset += Mathf.Sign(wheel) * scrollSensitivity;
                ClampScroll();
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                draggedThisGesture = false;
                pointerDownPos = pos;
                scrollAtPointerDown = scrollOffset;
            }

            if (pointerDown && mouse.leftButton.isPressed)
            {
                float pixelDelta = Vector2.Distance(pos, pointerDownPos);
                if (pixelDelta > 14f)
                {
                    draggedThisGesture = true;
                    // Arrastre horizontal = mover el mazo
                    if (pointerDownPos.y < Screen.height * 0.42f)
                    {
                        float dx = (pos.x - pointerDownPos.x) / Mathf.Max(1f, Screen.width);
                        scrollOffset = scrollAtPointerDown + dx * 0.95f;
                        ClampScroll();
                    }
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                bool wasClick = pointerDown && !draggedThisGesture;
                pointerDown = false;

                if (wasClick && cardControllers.Count > 0)
                    TrySelectAt(pos);
            }
        }

        private void TrySelectAt(Vector2 screenPos)
        {
            if (IsPointerOverBlockingUI(screenPos))
                return;

            var gm = GameManager.Instance;
            var net = NetworkManager.Instance;
            if (gm?.CurrentGameState == null || net == null) return;
            if (!gm.CurrentGameState.IsMyTurn(net.MyConnectionId)) return;

            var cam = handCamera != null ? handCamera : Camera.main;
            if (cam == null) return;

            var hit = PickCardAtScreen(cam, screenPos);
            if (hit == null) return;

            CycleSelectInValueGroup(hit);
            SyncSelectionVisuals();
        }

        /// <summary>
        /// Click en un stack: 1 → 2 → … → N → ninguna (para jugar doble/triple fácil).
        /// </summary>
        private void CycleSelectInValueGroup(Card3DController clicked)
        {
            if (clicked?.CardData == null || GameManager.Instance == null) return;

            int value = clicked.CardData.value;
            var group = cardControllers
                .Where(c => c != null && c.CardData != null && c.CardData.value == value)
                .ToList();

            if (group.Count == 0) return;

            // Si había otra valoración seleccionada, limpiar
            var selected = GameManager.Instance.SelectedCards;
            if (selected.Count > 0 && selected[0].value != value)
            {
                GameManager.Instance.ClearCardSelection();
                foreach (var c in cardControllers)
                    c.SetSelected(false, animate: false);
            }

            int currentlySelected = group.Count(c => GameManager.Instance.IsCardSelected(c.CardData));

            GameManager.Instance.ClearCardSelection();
            foreach (var c in group)
                c.SetSelected(false, animate: false);

            int nextCount = currentlySelected >= group.Count ? 0 : currentlySelected + 1;
            for (int i = 0; i < nextCount; i++)
            {
                GameManager.Instance.ToggleCardSelection(group[i].CardData);
                group[i].SetSelected(true);
            }

            // Soft rule feedback: e.g. double 4 vs double 5 on table
            if (nextCount > 0
                && !GameManager.Instance.TryValidatePlay(GameManager.Instance.SelectedCards, out string reason)
                && !string.IsNullOrEmpty(reason))
            {
                GameManager.Instance.NotifyPlayHint(reason);
            }
        }

        private void SyncSelectionVisuals()
        {
            if (GameManager.Instance == null) return;
            foreach (var c in cardControllers)
            {
                if (c?.CardData == null) continue;
                bool sel = GameManager.Instance.IsCardSelected(c.CardData);
                if (c.IsSelected != sel)
                    c.SetSelected(sel, animate: false);
            }

            RefreshPlayAssist();
        }

        /// <summary>
        /// Mint outline on cards that can legally answer the current table (only on your turn).
        /// </summary>
        private void RefreshPlayAssist()
        {
            var gm = GameManager.Instance;
            var nm = NetworkManager.Instance;
            bool myTurn = gm?.CurrentGameState != null
                          && nm != null
                          && gm.CurrentGameState.IsMyTurn(nm.MyConnectionId);

            var playableValues = myTurn ? ComputePlayableValues() : null;

            foreach (var c in cardControllers)
            {
                if (c?.CardData == null) continue;
                bool assist = playableValues != null && playableValues.Contains(c.CardData.value);
                c.SetPlayAssist(assist);
            }
        }

        private HashSet<int> ComputePlayableValues()
        {
            var result = new HashSet<int>();
            var gm = GameManager.Instance;
            if (gm?.CurrentGameState == null) return result;

            var hand = cardControllers
                .Where(c => c?.CardData != null)
                .Select(c => c.CardData)
                .ToList();
            if (hand.Count == 0) return result;

            var byValue = hand.GroupBy(c => c.value)
                .ToDictionary(g => g.Key, g => g.Count());

            bool free = gm.CurrentGameState.IsFirstPlay()
                        || gm.CurrentGameState.isNewRound
                        || gm.CurrentGameState.lastPlayedCards == null
                        || gm.CurrentGameState.lastPlayedCards.Count == 0;

            if (free)
            {
                foreach (var kv in byValue)
                    result.Add(kv.Key);
                return result;
            }

            int need = gm.CurrentGameState.lastPlayedCards.Count;
            int lastCmp = gm.CurrentGameState.lastPlayedCards[0].GetComparisonValue();

            foreach (var kv in byValue)
            {
                int value = kv.Key;
                int count = kv.Value;

                // Comodín (2): any count is legal
                if (value == 2)
                {
                    result.Add(value);
                    continue;
                }

                if (count < need) continue;

                int cmp = value == 1 ? 13 : value;
                if (cmp >= lastCmp)
                    result.Add(value);
            }

            return result;
        }

        private Card3DController PickCardAtScreen(Camera cam, Vector2 screenPos)
        {
            Card3DController best = null;
            float bestDepth = float.MaxValue;

            foreach (var controller in cardControllers)
            {
                if (controller == null) continue;

                var rend = controller.GetComponentInChildren<Renderer>();
                if (rend == null) continue;

                Bounds b = rend.bounds;
                // Proyectar centro + radio aprox a pantalla
                Vector3 center = cam.WorldToScreenPoint(b.center);
                if (center.z < 0.05f) continue;

                Vector3 corner = cam.WorldToScreenPoint(b.center + cam.transform.right * b.extents.magnitude * 0.55f
                                                         + cam.transform.up * b.extents.magnitude * 0.7f);
                float radiusX = Mathf.Abs(corner.x - center.x);
                float radiusY = Mathf.Abs(corner.y - center.y);
                radiusX = Mathf.Max(radiusX, 28f);
                radiusY = Mathf.Max(radiusY, 40f);

                if (Mathf.Abs(screenPos.x - center.x) <= radiusX &&
                    Mathf.Abs(screenPos.y - center.y) <= radiusY)
                {
                    if (center.z < bestDepth)
                    {
                        bestDepth = center.z;
                        best = controller;
                    }
                }
            }

            return best;
        }

        private static bool IsPointerOverBlockingUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject == null) continue;
                // Solo bloquear controles reales, no paneles full-screen transparentes
                if (r.gameObject.GetComponent<Button>() != null) return true;
                if (r.gameObject.GetComponentInParent<Button>() != null) return true;
                if (r.gameObject.GetComponent<TMP_InputField>() != null) return true;
                if (r.gameObject.GetComponentInParent<TMP_InputField>() != null) return true;
            }

            return false;
        }

        private void ClampScroll()
        {
            int groups = cardControllers
                .Where(c => c != null && c.CardData != null)
                .Select(c => c.CardData.value)
                .Distinct()
                .Count();

            float span = Mathf.Max(0f, (groups - 1) * 0.06f);
            scrollOffset = Mathf.Clamp(scrollOffset, -span - 0.15f, span + 0.15f);
        }

        private void CreateCard(Card cardData)
        {
            GameObject cardObj = null;

            if (Utils.CardVisualResolver.Instance != null)
                cardObj = Utils.CardVisualResolver.Instance.InstantiateCard(cardData, handContainer);

            if (cardObj == null)
            {
                if (cardPrefab == null)
                {
                    Debug.LogError("[HandManager] No card prefab / resolver available");
                    return;
                }

                cardObj = Instantiate(cardPrefab, handContainer);
            }

            SetLayerRecursive(cardObj, HandLayer);
            DisablePhysics(cardObj);
            EnsureInteractable(cardObj);
            ForceDrawOnTop(cardObj);

            Card3DController controller = cardObj.GetComponent<Card3DController>();
            if (controller == null)
                controller = cardObj.AddComponent<Card3DController>();

            controller.Initialize(cardData, preservePackMaterials: true);
            PepinoCardSkin.ApplyFaceUp(cardObj, cardData);
            cardControllers.Add(controller);
        }

        private void EnsureHandOverlayCamera()
        {
            var main = Camera.main;
            if (main == null) return;

            if (handCamera == null)
            {
                var existing = GameObject.Find("HandOverlayCamera");
                if (existing != null)
                    handCamera = existing.GetComponent<Camera>();
            }

            if (handCamera == null)
            {
                var go = new GameObject("HandOverlayCamera");
                handCamera = go.AddComponent<Camera>();
            }

            if (mainCamOriginalMask < 0)
                mainCamOriginalMask = main.cullingMask;

            main.cullingMask = mainCamOriginalMask & ~(1 << HandLayer);
            handCamera.cullingMask = 1 << HandLayer;
            handCamera.clearFlags = CameraClearFlags.Depth;
            handCamera.depth = main.depth + 10f;
            SyncHandCameraToMain();
        }

        private void SyncHandCameraToMain()
        {
            var main = Camera.main;
            if (main == null || handCamera == null) return;

            var tr = handCamera.transform;
            tr.SetParent(null, true);
            tr.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            handCamera.fieldOfView = main.fieldOfView;
            handCamera.orthographic = main.orthographic;
            handCamera.orthographicSize = main.orthographicSize;
            handCamera.nearClipPlane = main.nearClipPlane;
            handCamera.farClipPlane = main.farClipPlane;
            handCamera.depth = main.depth + 10f;
            handCamera.clearFlags = CameraClearFlags.Depth;
            handCamera.cullingMask = 1 << HandLayer;

            main.cullingMask = (mainCamOriginalMask >= 0 ? mainCamOriginalMask : main.cullingMask) & ~(1 << HandLayer);
        }

        private void RestoreMainCameraMask()
        {
            var main = Camera.main;
            if (main != null && mainCamOriginalMask >= 0)
                main.cullingMask = mainCamOriginalMask;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
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
            // Collider opcional (fallback); la selección real es por proyección a pantalla
            if (cardObj.GetComponentInChildren<Collider>() == null)
            {
                var box = cardObj.AddComponent<BoxCollider>();
                box.size = new Vector3(0.65f, 0.95f, 0.04f);
            }
        }

        private static void ForceDrawOnTop(GameObject cardObj)
        {
            foreach (var r in cardObj.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private void ArrangeCardsInViewport()
        {
            var cam = Camera.main;
            if (cam == null || cardControllers.Count == 0) return;

            var groups = cardControllers
                .Where(c => c != null && c.CardData != null)
                .GroupBy(c => c.CardData.value)
                .OrderBy(g => g.First().CardData.GetComparisonValue())
                .Select(g => g.ToList())
                .ToList();

            int groupCount = groups.Count;
            int totalCards = cardControllers.Count;
            float scaleMul = totalCards > 22 ? 0.82f : (totalCards > 14 ? 0.9f : 1f);
            Vector3 scale = cardLocalScale * scaleMul;

            float usableMin = viewportXMin + scrollOffset;
            float usableMax = viewportXMax + scrollOffset;

            for (int gi = 0; gi < groupCount; gi++)
            {
                float t = groupCount == 1 ? 0.5f : (float)gi / (groupCount - 1);
                float vx = Mathf.Lerp(usableMin, usableMax, t);
                float arc = 1f - Mathf.Abs(t - 0.5f) * 2f;
                float vyBase = viewportY + arc * 0.035f;
                float twist = Mathf.Lerp(14f, -14f, t);

                var group = groups[gi];
                for (int i = 0; i < group.Count; i++)
                {
                    float depth = viewportDepth + i * stackDepthStep;
                    float vy = vyBase + i * stackPeekY;
                    float vxStack = vx + i * stackPeekX;

                    Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vxStack, vy, depth));

                    Quaternion worldRot = Quaternion.LookRotation(
                        (cam.transform.position - worldPos).normalized,
                        cam.transform.up);
                    worldRot *= Quaternion.Euler(0f, 0f, twist);

                    var controller = group[i];
                    if (controller.IsSelected)
                        worldPos += -cam.transform.forward * 0.1f + cam.transform.up * 0.08f;

                    var tr = controller.transform;
                    tr.SetParent(null, true);
                    SetLayerRecursive(tr.gameObject, HandLayer);
                    tr.position = worldPos;
                    tr.rotation = worldRot;
                    tr.localScale = scale;
                    controller.UpdateOriginalPosition(worldPos);
                }
            }
        }

        public void RemoveCard(Card card)
        {
            var controller = cardControllers.Find(c => c.CardData != null && c.CardData.id == card.id);
            if (controller == null) return;

            cardControllers.Remove(controller);
            controller.DestroyWithAnimation();
            ArrangeCardsInViewport();
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
