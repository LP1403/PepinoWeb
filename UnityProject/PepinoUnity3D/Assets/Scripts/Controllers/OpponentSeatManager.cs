using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PepinoGame.Models;
using PepinoGame.Managers;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Face-down card fans + name labels for opponents around the table rim.
    /// </summary>
    public class OpponentSeatManager : MonoBehaviour
    {
        [SerializeField] private float tableRadius = 4.0f;
        [SerializeField] private float seatHeight = 0.85f;
        [SerializeField] private float cardScale = 2.4f;
        [SerializeField] private int maxVisibleCards = 10;

        private Transform seatsRoot;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private bool bound;

        public void Configure(float tableRadius, float seatHeight, float cardScale)
        {
            this.tableRadius = tableRadius;
            this.seatHeight = seatHeight;
            this.cardScale = cardScale;
        }

        private void Awake() => EnsureRoot();

        private void Update()
        {
            if (!bound) TryBind();
        }

        private void OnEnable() => TryBind();

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

        private void OnGameStateChanged(GameState state) => Refresh(state);

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
                if (i == myIndex) continue;

                int seatOffset = (i - myIndex + n) % n;
                // Local player = south (angle 180°). Others spaced around the rim.
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
            seat.transform.rotation = Quaternion.identity;
            spawned.Add(seat);

            int cardsToShow = Mathf.Clamp(Mathf.Max(player.cardCount, 1), 1, maxVisibleCards);
            // Always show at least a small fan so the seat is visible
            if (player.cardCount <= 0) cardsToShow = 0;

            for (int c = 0; c < cardsToShow; c++)
            {
                GameObject cardObj = SpawnBackCard(seat.transform);
                if (cardObj == null) break;

                float fan = (c - (cardsToShow - 1) * 0.5f) * 10f;
                // Fan along the rim tangent, backs facing roughly toward camera/up
                float tangentX = Mathf.Cos(rad);
                float tangentZ = -Mathf.Sin(rad);
                cardObj.transform.position = seatPos
                    + new Vector3(tangentX, 0f, tangentZ) * (fan * 0.035f)
                    + Vector3.up * (0.012f * c);
                // Tip toward table center so backs are readable from the south seat
                cardObj.transform.rotation =
                    Quaternion.AngleAxis(angleDeg, Vector3.up)
                    * CardOrientation.FaceDownOnTable(fan * 0.4f)
                    * Quaternion.Euler(-25f, 0f, 0f);
                cardObj.transform.localScale = Vector3.one * cardScale;
            }

            CreateNameLabel(seat.transform, player, angleDeg);
        }

        private static GameObject SpawnBackCard(Transform parent)
        {
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

        private static void CreateNameLabel(Transform seat, Player player, float angleDeg)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(seat, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = BuildLabel(player);
            tmp.fontSize = 5.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = player.isCurrentTurn
                ? new Color(1f, 0.92f, 0.35f)
                : Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;

            labelGo.AddComponent<SeatLabelBillboard>();
        }

        private static string BuildLabel(Player player)
        {
            string mark = player.isCurrentTurn ? "▶ " : "";
            if (player.hasWon) return $"{mark}{player.name} ★";
            return $"{mark}{player.name}\n{player.cardCount}";
        }

        private void EnsureRoot()
        {
            if (seatsRoot != null) return;
            var existing = GameObject.Find("OpponentSeats");
            seatsRoot = existing != null ? existing.transform : new GameObject("OpponentSeats").transform;
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
