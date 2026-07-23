using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PepinoGame.Models;
using PepinoGame.Managers;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Far-side rival: thick face-down card fan + name. Updates in place (no ghost stacking).
    /// </summary>
    public class OpponentSeatManager : MonoBehaviour
    {
        [SerializeField] private float tableRadius = 0.72f;
        [SerializeField] private float seatHeight = 1.7f;
        [SerializeField] private float cardWidth = 0.48f;
        [SerializeField] private float cardHeight = 0.70f;
        [SerializeField] private float cardThickness = 0.04f;
        [SerializeField] private int maxVisibleCards = 5;

        private Transform seatsRoot;
        private readonly Dictionary<string, SeatView> seats = new Dictionary<string, SeatView>();
        private bool bound;

        private static readonly Color BackGreen = new Color(0.2f, 0.85f, 0.4f, 1f);
        private static readonly Color Skin = new Color(0.86f, 0.66f, 0.52f, 1f);

        private class SeatView
        {
            public GameObject root;
            public Transform cardsRoot;
            public readonly List<GameObject> cards = new List<GameObject>();
            public TextMeshPro label;
            public string playerName;
            public int cardCount;
            public Vector3 seatPos;
            public Vector3 tangent;
            public Vector3 towardCenter;
        }

        public void Configure(float tableRadius, float seatHeight, float cardScale = 5.5f)
        {
            this.tableRadius = tableRadius;
            this.seatHeight = seatHeight;
            // Always use readable world sizes (ignore stale serialized inspector values)
            cardWidth = 0.55f;
            cardHeight = 0.80f;
            cardThickness = 0.05f;
            _ = cardScale;
        }

        private void Update()
        {
            if (!bound) TryBind();
        }

        private void LateUpdate()
        {
            FaceCardsAndLabelsToCamera();
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

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            try
            {
                Refresh(state);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OpponentSeatManager] {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void Refresh(GameState state)
        {
            EnsureRoot();

            if (state == null || !state.isGameStarted || state.players == null || state.players.Count == 0)
            {
                ClearAllSeats();
                return;
            }

            string myId = NetworkManager.Instance != null
                ? NetworkManager.Instance.MyConnectionId
                : string.Empty;

            int myIndex = state.players.FindIndex(p =>
                !string.IsNullOrEmpty(myId) && p.connectionId == myId);
            if (myIndex < 0) myIndex = 0;

            var keep = new HashSet<string>();
            int n = state.players.Count;

            for (int i = 0; i < n; i++)
            {
                if (i == myIndex) continue;
                var player = state.players[i];
                if (player == null) continue;

                string key = !string.IsNullOrEmpty(player.connectionId)
                    ? player.connectionId
                    : $"p{i}";
                keep.Add(key);

                int seatOffset = (i - myIndex + n) % n;
                float t = n <= 2 ? 0.5f : (seatOffset - 0.5f) / (n - 1f);
                float angleDeg = n == 2 ? 0f : Mathf.Lerp(40f, 320f, t);

                if (!seats.TryGetValue(key, out var view) || view.root == null)
                {
                    view = CreateSeat(key, player, angleDeg);
                    seats[key] = view;
                }
                else
                {
                    PlaceSeat(view, angleDeg);
                }

                UpdateSeatContents(view, player);
            }

            var toRemove = new List<string>();
            foreach (var kv in seats)
            {
                if (!keep.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }

            foreach (var key in toRemove)
            {
                if (seats[key].root != null)
                    Object.Destroy(seats[key].root);
                seats.Remove(key);
            }
        }

        private SeatView CreateSeat(string key, Player player, float angleDeg)
        {
            var view = new SeatView();
            view.root = new GameObject($"Seat_{player.name}");
            view.root.transform.SetParent(seatsRoot, false);

            view.cardsRoot = new GameObject("Cards").transform;
            view.cardsRoot.SetParent(view.root.transform, false);

            SpawnPalm(view.root.transform, -0.22f);
            SpawnPalm(view.root.transform, 0.22f);

            var badge = new GameObject("Label");
            badge.transform.SetParent(view.root.transform, false);
            view.label = badge.AddComponent<TextMeshPro>();
            view.label.alignment = TextAlignmentOptions.Center;
            view.label.fontSize = 0.55f;
            view.label.fontStyle = FontStyles.Bold;
            view.label.color = Color.white;
            view.label.outlineWidth = 0.25f;
            view.label.outlineColor = Color.black;
            view.label.textWrappingMode = TextWrappingModes.NoWrap;

            PlaceSeat(view, angleDeg);
            return view;
        }

        private void PlaceSeat(SeatView view, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            view.towardCenter = -new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            view.tangent = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));

            // Toward table center so the fan sits mid-upper screen (not off the far rim / outside mesh)
            view.seatPos = new Vector3(
                Mathf.Sin(rad) * tableRadius,
                seatHeight,
                Mathf.Cos(rad) * tableRadius) + view.towardCenter * 0.15f;

            view.root.transform.position = view.seatPos;
            view.root.transform.rotation = Quaternion.LookRotation(view.towardCenter, Vector3.up);
        }

        private void UpdateSeatContents(SeatView view, Player player)
        {
            view.playerName = player.name;
            view.cardCount = player.cardCount;

            int want = player.cardCount <= 0 ? 0 : Mathf.Clamp(player.cardCount, 1, maxVisibleCards);

            while (view.cards.Count < want)
                view.cards.Add(CreateBackCard(view.cardsRoot));

            while (view.cards.Count > want)
            {
                int last = view.cards.Count - 1;
                if (view.cards[last] != null)
                    Object.Destroy(view.cards[last]);
                view.cards.RemoveAt(last);
            }

            for (int c = 0; c < view.cards.Count; c++)
            {
                var card = view.cards[c];
                if (card == null) continue;

                float fan = (c - (view.cards.Count - 1) * 0.5f);
                card.transform.localPosition = new Vector3(fan * 0.32f, 0.22f + c * 0.02f, 0.05f);
                card.transform.localRotation = Quaternion.identity;
                card.transform.localScale = new Vector3(cardWidth, cardHeight, cardThickness);
                card.SetActive(true);
            }

            if (view.label != null)
            {
                string mark = player.isCurrentTurn ? "> " : "";
                string name = string.IsNullOrEmpty(player.name) ? "Rival" : player.name;
                view.label.text = $"{mark}{name}  ·  {player.cardCount}";
                view.label.color = player.isCurrentTurn
                    ? new Color(1f, 0.92f, 0.35f)
                    : Color.white;
                // Under the fan so it stays in frame (above was clipping off the top of the Game view)
                view.label.transform.localPosition = new Vector3(0f, -0.12f, 0.06f);
                view.label.fontSize = 0.5f;
            }
        }

        private void FaceCardsAndLabelsToCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            foreach (var kv in seats)
            {
                var view = kv.Value;
                if (view?.root == null) continue;

                foreach (var card in view.cards)
                {
                    if (card == null) continue;
                    Vector3 toCam = camPos - card.transform.position;
                    if (toCam.sqrMagnitude < 0.0001f) continue;
                    // Cube front is +Z; face the bright green face toward the camera
                    card.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }

                if (view.label != null)
                {
                    view.label.transform.rotation = Quaternion.LookRotation(
                        view.label.transform.position - camPos,
                        Vector3.up);
                }
            }
        }

        private static GameObject CreateBackCard(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "RivalCardBack";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", BackGreen);
            if (mat.HasProperty("_Color"))
                mat.color = BackGreen;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", BackGreen * 0.35f);
            }

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static void SpawnPalm(Transform parent, float x)
        {
            var palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = x < 0 ? "PalmL" : "PalmR";
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = new Vector3(x, 0.08f, 0.02f);
            palm.transform.localRotation = Quaternion.Euler(12f, 0f, x < 0 ? 14f : -14f);
            palm.transform.localScale = new Vector3(0.18f, 0.06f, 0.26f);
            Object.Destroy(palm.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Skin;
            palm.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void EnsureRoot()
        {
            if (seatsRoot != null) return;
            var existing = GameObject.Find("OpponentSeats");
            seatsRoot = existing != null
                ? existing.transform
                : new GameObject("OpponentSeats").transform;
        }

        private void ClearAllSeats()
        {
            foreach (var kv in seats)
            {
                if (kv.Value.root != null)
                    Object.Destroy(kv.Value.root);
            }

            seats.Clear();

            if (seatsRoot != null)
            {
                for (int i = seatsRoot.childCount - 1; i >= 0; i--)
                    Object.Destroy(seatsRoot.GetChild(i).gameObject);
            }
        }
    }
}
