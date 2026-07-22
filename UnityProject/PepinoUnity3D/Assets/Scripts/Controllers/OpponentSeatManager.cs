using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PepinoGame.Models;
using PepinoGame.Managers;
using PepinoGame.Utils;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Opponents: short fan of 3–4 card backs on the table rim + tiny name label (UNO-like).
    /// </summary>
    public class OpponentSeatManager : MonoBehaviour
    {
        [SerializeField] private float tableRadius = 1.35f;
        [SerializeField] private float seatHeight = 0.95f;
        [SerializeField] private float cardScale = 2.6f;
        [SerializeField] private int maxVisibleCards = 4;

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

            // UNO-like: always show at most 3–4 backs (enough to read “has cards”)
            int cardsToShow = 0;
            if (player.cardCount > 0)
                cardsToShow = Mathf.Clamp(player.cardCount, 3, maxVisibleCards);

            float tangentX = Mathf.Cos(rad);
            float tangentZ = -Mathf.Sin(rad);

            for (int c = 0; c < cardsToShow; c++)
            {
                GameObject cardObj = SpawnBackCard(seat.transform);
                if (cardObj == null) break;

                float fan = (c - (cardsToShow - 1) * 0.5f) * 7f;
                cardObj.transform.position = seatPos
                    + new Vector3(tangentX, 0f, tangentZ) * (fan * 0.028f)
                    + Vector3.up * (0.01f * c);
                // Face toward table center so we see the backs from the south seat
                float yaw = angleDeg + 180f;
                cardObj.transform.rotation = CardOrientation.OpponentBack(yaw);
                cardObj.transform.localScale = Vector3.one * cardScale;
            }

            CreateNameLabel(seat.transform, player);
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

        private static void CreateNameLabel(Transform seat, Player player)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(seat, false);
            // Slightly outside the rim, above the backs — keep small so it never covers the HUD
            labelGo.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            var tmp = labelGo.AddComponent<TextMeshPro>();
            string mark = player.isCurrentTurn ? "▶ " : "";
            tmp.text = $"{mark}{player.name} · {player.cardCount}";
            tmp.fontSize = 1.35f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = player.isCurrentTurn
                ? new Color(1f, 0.92f, 0.35f)
                : Color.white;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = Color.black;

            labelGo.AddComponent<SeatLabelBillboard>();
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
