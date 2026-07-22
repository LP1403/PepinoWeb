using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PepinoGame.Models;
using PepinoGame.Managers;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Spawns face-down card fans + name labels for opponents around the table (UNO-style seats).
    /// Local player sits at the south edge; others are distributed around.
    /// </summary>
    public class OpponentSeatManager : MonoBehaviour
    {
        [SerializeField] private float tableRadius = 1.85f;
        [SerializeField] private float seatHeight = 0.9f;
        [SerializeField] private float cardScale = 0.85f;
        [SerializeField] private int maxVisibleCards = 8;

        private Transform seatsRoot;
        private readonly List<GameObject> spawned = new List<GameObject>();

        private bool bound;

        private void Awake()
        {
            EnsureRoot();
        }

        private void Update()
        {
            if (!bound)
                TryBind();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void TryBind()
        {
            if (bound || GameManager.Instance == null) return;

            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            bound = true;

            if (GameManager.Instance.CurrentGameState != null)
                OnGameStateChanged(GameManager.Instance.CurrentGameState);
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            bound = false;
        }

        private void OnGameStateChanged(GameState state)
        {
            Refresh(state);
        }

        public void Refresh(GameState state)
        {
            Clear();
            if (state == null || !state.isGameStarted || state.players == null || state.players.Count == 0)
                return;

            EnsureRoot();

            string myId = NetworkManager.Instance != null
                ? NetworkManager.Instance.MyConnectionId
                : string.Empty;

            int myIndex = state.players.FindIndex(p => p.connectionId == myId);
            if (myIndex < 0) myIndex = 0;

            int n = state.players.Count;
            for (int i = 0; i < n; i++)
            {
                if (i == myIndex) continue; // local hand is HandManager

                int seatOffset = (i - myIndex + n) % n;
                // Local at south (180°). Spread others evenly excluding local slot.
                float angleDeg = 180f + (360f * seatOffset / n);
                SpawnSeat(state.players[i], angleDeg);
            }
        }

        private void SpawnSeat(Player player, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 seatPos = new Vector3(
                Mathf.Sin(rad) * tableRadius,
                seatHeight,
                Mathf.Cos(rad) * tableRadius);

            var seat = new GameObject($"Seat_{player.name}");
            seat.transform.SetParent(seatsRoot, false);
            seat.transform.position = seatPos;
            // Face toward table center
            seat.transform.rotation = Quaternion.LookRotation(
                (Vector3.zero - seatPos).normalized + Vector3.up * 0.15f,
                Vector3.up);
            spawned.Add(seat);

            int cardsToShow = Mathf.Clamp(player.cardCount, 0, maxVisibleCards);
            for (int c = 0; c < cardsToShow; c++)
            {
                GameObject cardObj = SpawnBackCard(seat.transform);
                if (cardObj == null) break;

                float fan = (c - (cardsToShow - 1) * 0.5f) * 8f;
                cardObj.transform.localPosition = new Vector3(fan * 0.04f, 0.01f * c, -0.05f * c);
                cardObj.transform.localRotation = CardOrientation.FaceDownOnTable(fan);
                cardObj.transform.localScale = Vector3.one * cardScale;
            }

            CreateNameLabel(seat.transform, player);
        }

        private static GameObject SpawnBackCard(Transform parent)
        {
            // Any pack card works — we show the back via FaceDownOnTable
            if (CardVisualResolver.Instance == null) return null;

            var dummy = new Card("♠", 1);
            var cardObj = CardVisualResolver.Instance.InstantiateCard(dummy, parent);
            if (cardObj == null) return null;

            foreach (var rb in cardObj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            foreach (var col in cardObj.GetComponentsInChildren<Collider>())
                col.enabled = false;

            var controller = cardObj.GetComponent<Card3DController>();
            if (controller != null)
                controller.SetInteractable(false);

            return cardObj;
        }

        private static void CreateNameLabel(Transform seat, Player player)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(seat, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = BuildLabel(player);
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = player.isCurrentTurn
                ? new Color(1f, 0.92f, 0.35f)
                : Color.white;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;

            var rect = labelGo.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(2.5f, 0.6f);

            // Billboard toward main camera each frame via simple look
            labelGo.AddComponent<SeatLabelBillboard>();
        }

        private static string BuildLabel(Player player)
        {
            string mark = player.isCurrentTurn ? "▶ " : "";
            if (player.hasWon) return $"{mark}{player.name} ★";
            return $"{mark}{player.name}\n{player.cardCount} cartas";
        }

        private void EnsureRoot()
        {
            if (seatsRoot != null) return;
            var existing = GameObject.Find("OpponentSeats");
            if (existing != null)
            {
                seatsRoot = existing.transform;
                return;
            }

            var go = new GameObject("OpponentSeats");
            seatsRoot = go.transform;
        }

        private void Clear()
        {
            foreach (var go in spawned)
            {
                if (go != null) Destroy(go);
            }

            spawned.Clear();

            if (seatsRoot != null)
            {
                for (int i = seatsRoot.childCount - 1; i >= 0; i--)
                    Destroy(seatsRoot.GetChild(i).gameObject);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>Keeps seat name labels readable from the player camera.</summary>
    public class SeatLabelBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(
                transform.position - cam.transform.position,
                Vector3.up);
        }
    }
}
